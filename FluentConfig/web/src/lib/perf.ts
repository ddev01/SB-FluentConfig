import { RpcMethods } from '../protocol';
import type { RpcClient } from '../rpc/client';

/**
 * Fire-and-forget perf.mark helper. Queues marks until an RpcClient is bound
 * so early events (script-start) are not lost before the bridge is ready.
 */

const pending: string[] = [];
let rpc: RpcClient | null = null;
const sent = new Set<string>();

function send(name: string): void {
  if (!rpc || !name) return;
  void rpc.request(RpcMethods.PerfMark, { name }).catch(() => {
    /* fire-and-forget */
  });
}

/** Bind the RPC client and flush any queued marks (in order). */
export function bindPerf(client: RpcClient): void {
  rpc = client;
  for (const name of pending) send(name);
  pending.length = 0;
}

/** Unbind on dispose so marks after teardown are dropped. */
export function unbindPerf(): void {
  rpc = null;
  pending.length = 0;
  sent.clear();
}

/**
 * Emit a perf.mark. When `once` is true (default for known startup marks),
 * duplicate names in the same page lifetime are ignored.
 */
export function mark(name: string, once = true): void {
  if (!name) return;
  if (once) {
    if (sent.has(name)) return;
    sent.add(name);
  }
  if (rpc) send(name);
  else pending.push(name);
}
