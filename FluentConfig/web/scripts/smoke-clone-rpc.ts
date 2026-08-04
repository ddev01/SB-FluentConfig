/**
 * Regression: Svelte $state Proxies cannot structuredClone / object-postMessage.
 * Run with: bun run scripts/smoke-clone-rpc.ts
 */
import { cloneJson } from '../src/lib/clone.ts';
import { RpcClient } from '../src/rpc/client.ts';
import { WireKinds } from '../src/protocol/index.ts';
import type { ChromeWebView, WebViewMessageListener } from '../src/bridge/types.ts';

// Minimal reactive-like Proxy (same failure mode as Svelte $state).
function asStateProxy<T extends object>(value: T): T {
  return new Proxy(value, {
    get(target, prop, receiver) {
      const v = Reflect.get(target, prop, receiver);
      if (v !== null && typeof v === 'object') return asStateProxy(v as object);
      return v;
    },
    set(target, prop, value, receiver) {
      return Reflect.set(target, prop, value, receiver);
    },
  }) as T;
}

const proxied = asStateProxy({
  smoke_enabled: true,
  smoke_name: 'smoke-user',
  smoke_pills: ['Alpha'],
});

let structuredCloneFailed = false;
try {
  structuredClone(proxied);
} catch {
  structuredCloneFailed = true;
}
if (!structuredCloneFailed) {
  throw new Error('expected structuredClone to reject Proxy (test setup invalid)');
}

const cloned = cloneJson(proxied);
if (cloned.smoke_enabled !== true || cloned.smoke_name !== 'smoke-user') {
  throw new Error('cloneJson lost values');
}
cloned.smoke_enabled = false;
if (proxied.smoke_enabled !== true) {
  throw new Error('cloneJson did not deep-copy');
}

// RpcClient must post JSON strings so WebView2 TryGetWebMessageAsString works.
class CaptureBridge implements ChromeWebView {
  last: unknown = null;
  private readonly listeners = new Set<WebViewMessageListener>();

  postMessage(message: unknown): void {
    this.last = message;
    if (typeof message !== 'string') {
      throw new Error('RpcClient must postMessage a JSON string, got ' + typeof message);
    }
    const parsed = JSON.parse(message) as { kind: string; id: number; method: string; params?: { values?: unknown } };
    // Reply so request() resolves
    queueMicrotask(() => {
      for (const l of this.listeners) {
        l({
          data: {
            kind: WireKinds.Response,
            id: parsed.id,
            result: { ok: true, echoEnabled: (parsed.params?.values as { smoke_enabled?: boolean })?.smoke_enabled },
          },
        });
      }
    });
  }

  addEventListener(_type: 'message', listener: WebViewMessageListener): void {
    this.listeners.add(listener);
  }

  removeEventListener(_type: 'message', listener: WebViewMessageListener): void {
    this.listeners.delete(listener);
  }
}

const bridge = new CaptureBridge();
const rpc = new RpcClient(bridge);
const result = await rpc.request<{ ok: boolean; echoEnabled: boolean }>('save', {
  values: proxied,
});
if (!result.ok || result.echoEnabled !== true) {
  throw new Error('save RPC with proxied values failed: ' + JSON.stringify(result));
}
if (typeof bridge.last !== 'string') {
  throw new Error('bridge did not receive a string');
}

console.log('OK — cloneJson + string postMessage RPC smoke passed');
rpc.dispose();
