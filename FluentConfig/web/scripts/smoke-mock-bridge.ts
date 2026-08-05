/**
 * Protocol-level smoke test: MockWebView + RpcClient bootstrap, refresh, progress, save.
 * Run with: bun run scripts/smoke-mock-bridge.ts
 */
import { MockWebView } from '../src/bridge/mockBridge.ts';
import { RpcClient } from '../src/rpc/client.ts';
import { PushEventNames, RpcMethods } from '../src/protocol/index.ts';
import type { ProgressPayload, UiDocument } from '../src/protocol/index.ts';

function waitForBootstrap(rpc: RpcClient, timeoutMs = 2000): Promise<UiDocument> {
  return new Promise((resolve, reject) => {
    const t = setTimeout(() => reject(new Error('bootstrap timeout')), timeoutMs);
    rpc.on(PushEventNames.Bootstrap, (payload) => {
      clearTimeout(t);
      resolve(payload as UiDocument);
    });
  });
}

const bridge = new MockWebView();
const rpc = new RpcClient(bridge);

const doc = await waitForBootstrap(rpc);
if (!doc.title || !doc.sections?.length) throw new Error('bad bootstrap');
if (doc.colorScheme !== 'dark') throw new Error('expected dark colorScheme');
if (!doc.frameworkVersion) throw new Error('expected frameworkVersion');
if (!doc.repoUrl) throw new Error('expected repoUrl');
if (typeof doc.dontRemindDiscard !== 'boolean') {
  throw new Error('expected dontRemindDiscard boolean');
}

const types = new Set<string>();
const walk = (nodes: { type: string; children?: unknown[]; items?: { children: unknown[] }[]; itemTemplate?: unknown[]; rowSchema?: unknown[] }[]) => {
  for (const n of nodes) {
    types.add(n.type);
    if (n.type === 'group' && Array.isArray(n.children)) walk(n.children as typeof nodes);
    if (n.type === 'pill-input') {
      if (n.itemTemplate) walk(n.itemTemplate as typeof nodes);
      if (n.items) for (const i of n.items) walk(i.children as typeof nodes);
    }
    if (n.type === 'repeatable-rows' && n.rowSchema) walk(n.rowSchema as typeof nodes);
  }
};
for (const s of doc.sections) walk(s.children as Parameters<typeof walk>[0]);
if (!types.has('connection-status')) {
  throw new Error('mock document missing connection-status');
}

const refresh = await rpc.request<{ options: { value: string; display: string }[] }>(
  RpcMethods.DropdownRefresh,
  { saveKey: 'reward_display' },
);
if (!refresh.options.some((o) => o.display.includes('New Reward'))) {
  throw new Error('dropdown.refresh did not return refreshed options');
}

const browse = await rpc.request<{ path?: string }>(RpcMethods.FilepathBrowse, {
  saveKey: 'export_filepath',
});
if (!browse.path) throw new Error('filepath.browse returned no path');

const progressEvents: ProgressPayload[] = [];
const unsub = rpc.on(PushEventNames.Progress, (p) => {
  progressEvents.push(p as ProgressPayload);
});
await rpc.request(RpcMethods.ButtonClick, {
  buttonId: 'btn-progress',
  values: doc.values,
});
// Wait for progress sequence
await new Promise((r) => setTimeout(r, 1500));
unsub();
if (progressEvents.length < 5) {
  throw new Error(`expected progress sequence, got ${progressEvents.length}`);
}
if (!progressEvents.some((p) => p.done)) {
  throw new Error('progress never completed');
}

await rpc.request(RpcMethods.Save, { values: doc.values });

const pill = await rpc.request<{ items?: { name: string }[] }>(RpcMethods.PillChanged, {
  saveKey: 'test_items',
  action: 'add',
  name: 'Gamma',
  items: ['Alpha', 'Beta', 'Gamma'],
});
if (!pill.items?.some((i) => i.name === 'Gamma')) {
  throw new Error('pill.changed add failed');
}

await rpc.request(RpcMethods.ShellOpenUrl, { url: 'https://example.test/docs' });
await rpc.request(RpcMethods.PerfMark, { name: 'web-ready' });
await rpc.request(RpcMethods.WindowClose, { alreadyConfirmed: true });

console.log('OK — mock bridge smoke passed');
console.log('  bootstrap title:', doc.title);
console.log('  frameworkVersion:', doc.frameworkVersion);
console.log('  schema types:', [...types].sort().join(', '));
console.log('  refresh options:', refresh.options.length);
console.log('  browse path:', browse.path);
console.log('  progress events:', progressEvents.length);
rpc.dispose();
