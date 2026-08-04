<script lang="ts">
  import type { PillInputNode, PillItemSchema, SchemaNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import type { PillChangedResult } from '../../protocol';
  import { cloneJson } from '../clone';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';
  import SchemaNodeView from './SchemaNodeView.svelte';

  interface Props {
    node: PillInputNode;
  }

  let { node }: Props = $props();
  let draft = $state('');
  let selected = $state<string | null>(null);
  let busy = $state(false);

  let names = $derived.by(() => {
    const raw = appStore.getValue(node.saveKey);
    if (Array.isArray(raw)) return raw.map(String);
    return (node.items ?? []).map((i) => i.name);
  });

  let activeItem = $derived.by((): PillItemSchema | null => {
    const name = selected && names.includes(selected) ? selected : names[0] ?? null;
    if (!name) return null;
    const fromHost = node.items?.find((i) => i.name === name);
    if (fromHost) return fromHost;
    if (node.itemTemplate) {
      return {
        name,
        children: expandTemplate(node.itemTemplate, name),
      };
    }
    return { name, children: [] };
  });

  $effect(() => {
    if (selected && !names.includes(selected)) {
      selected = names[0] ?? null;
    } else if (!selected && names.length > 0) {
      selected = names[0]!;
    }
  });

  function expandTemplate(template: SchemaNode[], name: string): SchemaNode[] {
    return JSON.parse(JSON.stringify(template).replaceAll('{name}', name)) as SchemaNode[];
  }

  async function sync(
    action: 'add' | 'remove' | 'rename',
    name: string,
    previousName?: string,
  ): Promise<void> {
    busy = true;
    try {
      const items =
        action === 'add'
          ? [...names, name]
          : action === 'remove'
            ? names.filter((n) => n !== name)
            : names.map((n) => (n === previousName ? name : n));

      appStore.setValue(node.saveKey, items);

      const result = await appStore.client().request<PillChangedResult>(
        RpcMethods.PillChanged,
        {
          saveKey: node.saveKey,
          action,
          name,
          previousName,
          items,
        },
      );

      if (result.items) {
        appStore.patchPillItems(node.saveKey, result.items);
      }
      if (result.removedKeys) {
        for (const key of result.removedKeys) {
          const next = cloneJson(appStore.values);
          delete next[key];
          appStore.values = next;
        }
      }

      if (action === 'add') selected = name;
      if (action === 'remove') selected = items[0] ?? null;
    } catch (err) {
      appStore.pushToast(err instanceof Error ? err.message : 'Pill update failed');
    } finally {
      busy = false;
    }
  }

  function add(): void {
    const name = draft.trim();
    if (!name || names.includes(name)) return;
    draft = '';
    void sync('add', name);
  }

  function remove(name: string): void {
    void sync('remove', name);
  }
</script>

<FieldShell label={node.label} hint={node.hint}>
  <div class="flex flex-wrap gap-2">
    {#each names as name (name)}
      <div
        class="inline-flex items-center gap-1 rounded-full border pl-3 text-sm transition-colors
          {selected === name
          ? 'border-sky-500 bg-sky-500/20 text-sky-800 dark:text-sky-100'
          : 'border-zinc-300 bg-white text-zinc-700 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-300'}"
      >
        <button
          type="button"
          class="py-1 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-500"
          onclick={() => (selected = name)}
        >
          {name}
        </button>
        <button
          type="button"
          class="rounded-full px-2 py-1 text-zinc-500 hover:text-red-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-500"
          aria-label={`Remove ${name}`}
          disabled={busy}
          onclick={() => remove(name)}
        >
          ×
        </button>
      </div>
    {/each}
  </div>

  <div class="mt-2 flex gap-2">
    <input
      type="text"
      class="fc-input"
      placeholder="New pill name…"
      bind:value={draft}
      disabled={busy}
      onkeydown={(e) => {
        if (e.key === 'Enter') {
          e.preventDefault();
          add();
        }
      }}
    />
    <button type="button" class="fc-btn" disabled={busy} onclick={add}>
      Add
    </button>
  </div>

  {#if activeItem && activeItem.children.length > 0}
    <div class="mt-3 rounded-lg border border-zinc-800 bg-zinc-950/50 px-3 py-2">
      {#each activeItem.children as child, i (child.type + String('id' in child ? child.id : i) + activeItem.name)}
        <SchemaNodeView node={child} />
      {/each}
    </div>
  {/if}
</FieldShell>
