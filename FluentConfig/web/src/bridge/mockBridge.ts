import type { ChromeWebView, WebViewMessageListener } from './types';
import type {
  PillItemSchema,
  SchemaNode,
  WireMessage,
} from '../protocol';
import { PushEventNames, RpcMethods, WireKinds } from '../protocol';
import { cloneJson } from '../lib/clone';
import { createMockDocument } from './mockDocument';

function parseIncoming(data: unknown): WireMessage | null {
  if (data == null) return null;
  if (typeof data === 'string') {
    try {
      return JSON.parse(data) as WireMessage;
    } catch {
      return null;
    }
  }
  if (typeof data === 'object' && data !== null && 'kind' in data) {
    return data as WireMessage;
  }
  return null;
}

function expandTemplate(template: SchemaNode[], name: string): SchemaNode[] {
  const json = JSON.stringify(template).replaceAll('{name}', name);
  return JSON.parse(json) as SchemaNode[];
}

/**
 * In-browser stand-in for `window.chrome.webview`.
 * Same postMessage / message-event surface; serves fake bootstrap + RPC.
 */
export class MockWebView implements ChromeWebView {
  private readonly listeners = new Set<WebViewMessageListener>();
  private readonly doc = createMockDocument();
  private refreshRound = 0;
  private dismissedUpdate = false;
  private bootstrapped = false;

  constructor() {
    // Defer so RpcClient can subscribe before bootstrap arrives.
    queueMicrotask(() => this.pushBootstrap());
  }

  postMessage(message: unknown): void {
    const msg = parseIncoming(message);
    if (!msg) return;
    // Web→Host reply to a host-initiated request (e.g. window.closeRequested).
    if (msg.kind === WireKinds.Response) {
      this.emit(msg);
      return;
    }
    if (msg.kind !== WireKinds.Request) return;
    void this.handleRequest(msg.id, msg.method, msg.params);
  }

  addEventListener(type: 'message', listener: WebViewMessageListener): void {
    if (type !== 'message') return;
    this.listeners.add(listener);
    // HMR / remount: singleton may already have pushed bootstrap before this listener existed.
    if (this.bootstrapped) {
      queueMicrotask(() => {
        if (!this.listeners.has(listener)) return;
        const payload = this.bootstrapPayload();
        listener({
          data: {
            kind: WireKinds.Event,
            event: PushEventNames.Bootstrap,
            payload,
          },
        });
      });
    }
  }

  removeEventListener(type: 'message', listener: WebViewMessageListener): void {
    if (type !== 'message') return;
    this.listeners.delete(listener);
  }

  private emit(message: WireMessage): void {
    const event = { data: message };
    for (const listener of [...this.listeners]) {
      listener(event);
    }
  }

  private bootstrapPayload() {
    const payload = cloneJson(this.doc);
    if (this.dismissedUpdate) {
      payload.sections = payload.sections.map((section) => ({
        ...section,
        children: section.children.filter((n) => n.type !== 'update-notice'),
      }));
    }
    return payload;
  }

  private pushBootstrap(): void {
    this.bootstrapped = true;
    this.emit({
      kind: WireKinds.Event,
      event: PushEventNames.Bootstrap,
      payload: this.bootstrapPayload(),
    });
  }

  private async handleRequest(
    id: number,
    method: string,
    params: unknown,
  ): Promise<void> {
    try {
      const result = await this.dispatch(method, params);
      this.emit({ kind: WireKinds.Response, id, result });
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);
      this.emit({
        kind: WireKinds.Response,
        id,
        error: { code: 'mock_error', message },
      });
    }
  }

  private async dispatch(method: string, params: unknown): Promise<unknown> {
    switch (method) {
      case RpcMethods.Save:
        return { ok: true };

      case RpcMethods.DropdownRefresh: {
        this.refreshRound += 1;
        const p = params as { saveKey?: string };
        if (p.saveKey === 'reward_display' || p.saveKey === 'channel_reward') {
          return {
            options: [
              { value: 'reward-1', display: 'Highlight Message' },
              { value: 'reward-2', display: 'Hydrate Reminder' },
              { value: 'reward-3', display: 'Song Request' },
              {
                value: `reward-new-${this.refreshRound}`,
                display: `New Reward (${this.refreshRound})`,
              },
            ],
          };
        }
        return {
          options: [
            { value: 'Option A', display: 'Option A' },
            { value: 'Option B', display: 'Option B' },
            { value: 'Option C', display: 'Option C' },
            {
              value: `Option D (refreshed ${this.refreshRound})`,
              display: `Option D (refreshed ${this.refreshRound})`,
            },
          ],
        };
      }

      case RpcMethods.FilepathBrowse:
        return { path: 'C:\\Example\\export\\settings.json' };

      case RpcMethods.ButtonClick: {
        const p = params as { buttonId?: string };
        if (p.buttonId === 'btn-progress') {
          void this.runProgressDemo();
          return { ok: true };
        }
        if (p.buttonId === 'btn-toast') {
          // Host would show toast; mock replies ok — UI may also surface locally.
          return { ok: true, toast: 'Mock toast from host' };
        }
        if (p.buttonId === 'btn-reconnect') {
          return { ok: true, toast: 'Reconnect requested (mock)' };
        }
        return { ok: true };
      }

      case RpcMethods.PillChanged: {
        const p = params as {
          saveKey: string;
          action: 'add' | 'remove' | 'rename';
          name: string;
          previousName?: string;
          items: string[];
        };
        const node = this.findPillNode(p.saveKey);
        const template = node?.itemTemplate ?? [];
        const items: PillItemSchema[] = p.items.map((name) => ({
          name,
          children: expandTemplate(template, name),
        }));
        if (node) node.items = items;
        return {
          items,
          removedKeys:
            p.action === 'remove'
              ? [`${p.name}_enabled`, `${p.name}_value`]
              : undefined,
        };
      }

      case RpcMethods.UpdateStage:
        void this.runProgressDemo('update-stage', 'Staging update…');
        return { ok: true };

      case RpcMethods.UpdateDismiss:
        this.dismissedUpdate = true;
        return { ok: true };

      case RpcMethods.Log:
        console.info('[mock host log]', (params as { message?: string })?.message);
        return { ok: true };

      case RpcMethods.Toast:
        console.info('[mock toast]', (params as { message?: string })?.message);
        return { ok: true };

      case RpcMethods.WindowClose: {
        const p = params as { alreadyConfirmed?: boolean };
        if (p.alreadyConfirmed) {
          // Web already ran confirmDiscardIfNeeded — mirror host Close().
          console.info('[mock] window.close (alreadyConfirmed)');
          this.doc.dontRemindDiscard = this.doc.dontRemindDiscard ?? false;
          return { ok: true };
        }
        // Host would send window.closeRequested; simulate that round-trip.
        const result = await this.requestFromWeb(RpcMethods.WindowCloseRequested, {});
        const closeResult = result as {
          allowClose?: boolean;
          dontRemindAgain?: boolean;
        };
        if (closeResult?.dontRemindAgain) {
          this.doc.dontRemindDiscard = true;
        }
        if (!closeResult?.allowClose) {
          return { ok: false, cancelled: true };
        }
        console.info('[mock] window.close after closeRequested allow');
        return { ok: true };
      }

      case RpcMethods.ShellOpenUrl: {
        const url = (params as { url?: string })?.url;
        if (!url || !/^https?:\/\//i.test(url)) {
          throw new Error('shell.openUrl: only http(s) URLs allowed');
        }
        console.info('[mock] shell.openUrl', url);
        // Dev convenience — open in a new tab when possible.
        try {
          window.open(url, '_blank', 'noopener,noreferrer');
        } catch {
          /* headless / restricted */
        }
        return { ok: true };
      }

      case RpcMethods.PerfMark: {
        const name = (params as { name?: string })?.name;
        console.info('[mock] perf.mark', name);
        return { ok: true };
      }

      default:
        throw new Error(`Unknown mock RPC method: ${method}`);
    }
  }

  /**
   * Host→Web request (same surface as real HostBridge.SendRequestAndWait).
   * Used so window.close without alreadyConfirmed exercises the real dirty dialog.
   */
  private requestFromWeb(method: string, params: unknown): Promise<unknown> {
    const id = -Math.floor(Math.random() * 1_000_000) - 1;
    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.listeners.delete(waiter);
        reject(new Error(`mock requestFromWeb timeout: ${method}`));
      }, 60_000);

      const waiter: WebViewMessageListener = (event) => {
        const msg = parseIncoming(event.data);
        if (!msg || msg.kind !== WireKinds.Response || msg.id !== id) return;
        clearTimeout(timeout);
        this.listeners.delete(waiter);
        if (msg.error) {
          reject(new Error(`${msg.error.code}: ${msg.error.message}`));
        } else {
          resolve(msg.result);
        }
      };
      this.listeners.add(waiter);
      this.emit({
        kind: WireKinds.Request,
        id,
        method,
        params,
      });
    });
  }

  private findPillNode(saveKey: string) {
    for (const section of this.doc.sections) {
      for (const child of section.children) {
        if (child.type === 'pill-input' && child.saveKey === saveKey) {
          return child;
        }
      }
    }
    return undefined;
  }

  private async runProgressDemo(
    id = 'progress-1',
    title = 'Progress Test',
  ): Promise<void> {
    const total = 10;
    for (let current = 1; current <= total; current++) {
      await delay(120);
      this.emit({
        kind: WireKinds.Event,
        event: PushEventNames.Progress,
        payload: {
          id,
          title,
          message: current < total ? 'Simulating work…' : 'Done.',
          current,
          total,
          percent: Math.round((current / total) * 100),
          done: current === total,
        },
      });
    }
  }
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

let singleton: MockWebView | null = null;

export function getMockWebView(): MockWebView {
  if (!singleton) singleton = new MockWebView();
  return singleton;
}
