<script lang="ts">
  import type { PillInputNode, PillItemSchema, SchemaNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import type { PillChangedResult } from '../../protocol';
  import { cloneJson } from '../clone';
  import { scaleIn } from '../motion';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';
  import SchemaNodeView from './SchemaNodeView.svelte';
  import TrashIcon from '../icons/TrashIcon.svelte';

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

  async function removeSelected(): Promise<void> {
    if (!selected || busy) return;
    const name = selected;
    const ok = await appStore.openConfirm({
      title: 'Remove item',
      message: `Remove “${name}” and its settings? This can’t be undone from here.`,
      confirmText: 'Remove',
      cancelText: 'Cancel',
    });
    if (!ok) return;
    await sync('remove', name);
  }
</script>

<FieldShell label={node.label} hint={node.hint}>
  <div class="flex flex-wrap gap-2" role="tablist" aria-label={node.label}>
    {#each names as name (name)}
      <button
        type="button"
        role="tab"
        aria-selected={selected === name}
        class="rounded-full border px-3.5 py-1.5 text-sm font-medium transition-colors duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50
          {selected === name
          ? 'border-fc-accent bg-fc-accent/15 text-fc-accent'
          : 'border-fc-border bg-fc-surface text-fc-text-muted hover:border-fc-border-strong hover:text-fc-text'}"
        transition:scaleIn={{ duration: 0.16 }}
        onclick={() => (selected = name)}
      >
        {name}
      </button>
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
    <button
      type="button"
      class="fc-btn flex items-center gap-2 text-fc-text-subtle hover:border-fc-danger/50 hover:text-fc-danger"
      disabled={busy || !selected}
      aria-label={selected ? `Remove ${selected}` : 'Remove selected item'}
      title={selected ? `Remove “${selected}”` : 'Select an item to remove'}
      onclick={() => void removeSelected()}
    >
      <TrashIcon class="h-4 w-4" />
      <span class="hidden sm:inline">Remove</span>
    </button>
  </div>

  {#if activeItem && activeItem.children.length > 0}
    <div class="mt-3 rounded-fc-lg border border-fc-border bg-fc-elevated/50 px-3 py-2">
      {#each activeItem.children as child, i (child.type + String('id' in child ? child.id : i) + activeItem.name)}
        <SchemaNodeView node={child} />
      {/each}
    </div>
  {/if}
</FieldShell>
