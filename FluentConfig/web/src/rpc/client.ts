import type { ChromeWebView } from '../bridge/types';
import type {
  PushEventMessage,
  RpcRequest,
  RpcResponse,
  WireMessage,
} from '../protocol';
import { WireKinds } from '../protocol';

export type EventHandler = (payload: unknown) => void;
export type IncomingRequestHandler = (
  method: string,
  params: unknown,
) => Promise<unknown> | unknown;

function parseMessage(data: unknown): WireMessage | null {
  if (data == null) return null;
  if (typeof data === 'string') {
    try {
      return JSON.parse(data) as WireMessage;
    } catch {
      return null;
    }
  }
  if (typeof data === 'object' && 'kind' in (data as object)) {
    return data as WireMessage;
  }
  return null;
}

export class RpcTimeoutError extends Error {
  readonly method: string;
  readonly timeoutMs: number;

  constructor(method: string, timeoutMs: number) {
    super(`RPC timeout after ${timeoutMs}ms: ${method}`);
    this.name = 'RpcTimeoutError';
    this.method = method;
    this.timeoutMs = timeoutMs;
  }
}

/**
 * Correlation-ID RPC + push-event subscriptions over `window.chrome.webview`
 * (or a mock with the same interface).
 */
export class RpcClient {
  private nextId = 1;
  private readonly pending = new Map<
    number,
    {
      resolve: (value: unknown) => void;
      reject: (reason: Error) => void;
      timer: ReturnType<typeof setTimeout>;
    }
  >();
  private readonly eventHandlers = new Map<string, Set<EventHandler>>();
  private incomingRequestHandler: IncomingRequestHandler | null = null;
  private readonly onMessage: (event: { data: unknown }) => void;
  /** Default request timeout (ms). Override per-call via <c>request(..., { timeoutMs })</c>. */
  defaultTimeoutMs = 60_000;

  constructor(private readonly bridge: ChromeWebView) {
    this.onMessage = (event) => this.handleRaw(event.data);
    this.bridge.addEventListener('message', this.onMessage);
  }

  dispose(): void {
    this.bridge.removeEventListener('message', this.onMessage);
    for (const { reject, timer } of this.pending.values()) {
      clearTimeout(timer);
      reject(new Error('RpcClient disposed'));
    }
    this.pending.clear();
    this.eventHandlers.clear();
  }

  /** Register handler for host→web requests (e.g. dialog.confirm). */
  setIncomingRequestHandler(handler: IncomingRequestHandler | null): void {
    this.incomingRequestHandler = handler;
  }

  request<T = unknown>(
    method: string,
    params?: unknown,
    options?: { timeoutMs?: number },
  ): Promise<T> {
    const id = this.nextId++;
    const timeoutMs = options?.timeoutMs ?? this.defaultTimeoutMs;
    const msg: RpcRequest = {
      kind: WireKinds.Request,
      id,
      method,
      ...(params !== undefined ? { params } : {}),
    };

    return new Promise<T>((resolve, reject) => {
      const timer = setTimeout(() => {
        const entry = this.pending.get(id);
        if (!entry) return;
        this.pending.delete(id);
        entry.reject(new RpcTimeoutError(method, timeoutMs));
      }, timeoutMs);

      this.pending.set(id, {
        resolve: (v) => resolve(v as T),
        reject,
        timer,
      });
      this.post(msg);
    });
  }

  /** Subscribe to a push event name (`bootstrap`, `progress`, …). Returns unsubscribe. */
  on(event: string, handler: EventHandler): () => void {
    let set = this.eventHandlers.get(event);
    if (!set) {
      set = new Set();
      this.eventHandlers.set(event, set);
    }
    set.add(handler);
    return () => {
      set!.delete(handler);
      if (set!.size === 0) this.eventHandlers.delete(event);
    };
  }

  private post(message: WireMessage): void {
    // Always send a JSON string. WebView2's TryGetWebMessageAsString only returns
    // string posts; object posts need WebMessageAsJson. Stringifying also strips
    // Svelte $state Proxies that structured-clone / object postMessage reject.
    this.bridge.postMessage(JSON.stringify(message));
  }

  private handleRaw(data: unknown): void {
    const msg = parseMessage(data);
    if (!msg) return;

    if (msg.kind === WireKinds.Response) {
      this.handleResponse(msg);
      return;
    }
    if (msg.kind === WireKinds.Event) {
      this.handleEvent(msg);
      return;
    }
    if (msg.kind === WireKinds.Request) {
      void this.handleIncomingRequest(msg);
    }
  }

  private handleResponse(msg: RpcResponse): void {
    const entry = this.pending.get(msg.id);
    if (!entry) return;
    this.pending.delete(msg.id);
    clearTimeout(entry.timer);
    if (msg.error) {
      entry.reject(new Error(`${msg.error.code}: ${msg.error.message}`));
    } else {
      entry.resolve(msg.result);
    }
  }

  private handleEvent(msg: PushEventMessage): void {
    const handlers = this.eventHandlers.get(msg.event);
    if (!handlers) return;
    for (const h of [...handlers]) {
      h(msg.payload);
    }
  }

  private async handleIncomingRequest(msg: RpcRequest): Promise<void> {
    const reply = (response: RpcResponse) => this.post(response);
    if (!this.incomingRequestHandler) {
      reply({
        kind: WireKinds.Response,
        id: msg.id,
        error: { code: 'not_supported', message: `No handler for ${msg.method}` },
      });
      return;
    }
    try {
      const result = await this.incomingRequestHandler(msg.method, msg.params);
      reply({ kind: WireKinds.Response, id: msg.id, result });
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);
      reply({
        kind: WireKinds.Response,
        id: msg.id,
        error: { code: 'handler_error', message },
      });
    }
  }
}
