<script lang="ts">
  import type { RepeatableRowsNode, SchemaNode } from '../../protocol';
  import { cloneJson } from '../clone';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';
  import SchemaNodeView from './SchemaNodeView.svelte';

  interface Props {
    node: RepeatableRowsNode;
  }

  let { node }: Props = $props();

  let rows = $derived.by(() => {
    const raw = appStore.getValue(node.saveKey);
    return Array.isArray(raw) ? (raw as Record<string, unknown>[]) : [];
  });

  function defaultsFromSchema(): Record<string, unknown> {
    const row: Record<string, unknown> = {};
    for (const child of node.rowSchema) {
      applyDefault(child, row);
    }
    return row;
  }

  function applyDefault(child: SchemaNode, row: Record<string, unknown>): void {
    if (!('saveKey' in child) || !child.saveKey) return;
    switch (child.type) {
      case 'toggle':
        row[child.saveKey] = child.defaultValue ?? false;
        break;
      case 'textbox':
      case 'filepath':
      case 'color-picker':
      case 'duration-input':
        row[child.saveKey] = child.defaultValue ?? '';
        break;
      case 'slider':
      case 'number-input':
        row[child.saveKey] = child.defaultValue ?? 0;
        break;
      case 'dropdown':
        row[child.saveKey] =
          child.defaultByValue ??
          child.options?.[child.defaultIndex ?? 0]?.value ??
          '';
        break;
      default:
        break;
    }
  }

  function setRows(next: Record<string, unknown>[]): void {
    appStore.setValue(node.saveKey, next);
  }

  function addRow(): void {
    setRows([...rows, defaultsFromSchema()]);
  }

  function removeRow(index: number): void {
    setRows(rows.filter((_, i) => i !== index));
  }

  /** Rewrite relative saveKeys to `saveKey[i].relative`. */
  function scopedNode(child: SchemaNode, index: number): SchemaNode {
    const clone = cloneJson(child) as SchemaNode;
    scopeKeys(clone, index);
    return clone;
  }

  function scopeKeys(n: SchemaNode, index: number): void {
    if ('saveKey' in n && typeof n.saveKey === 'string' && !n.saveKey.includes('[')) {
      n.saveKey = `${node.saveKey}[${index}].${n.saveKey}`;
    }
    if ('valueSaveKey' in n && typeof n.valueSaveKey === 'string' && n.valueSaveKey) {
      if (!n.valueSaveKey.includes('[')) {
        n.valueSaveKey = `${node.saveKey}[${index}].${n.valueSaveKey}`;
      }
    }
    if (n.type === 'group') {
      for (const c of n.children) scopeKeys(c, index);
    }
    if ('visibility' in n && n.visibility && !n.visibility.saveKey.includes('[')) {
      n.visibility = {
        ...n.visibility,
        saveKey: `${node.saveKey}[${index}].${n.visibility.saveKey}`,
      };
    }
  }
</script>

<FieldShell>
  <div class="space-y-3">
    {#each rows as _row, index (index)}
      <div class="rounded-lg border border-zinc-800 bg-zinc-950/40 px-3 py-2">
        <div class="mb-1 flex items-center justify-between">
          <span class="text-xs font-medium uppercase tracking-wide text-zinc-500"
            >Row {index + 1}</span
          >
          <button
            type="button"
            class="text-xs text-zinc-500 hover:text-red-300 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-500"
            onclick={() => removeRow(index)}
          >
            Remove
          </button>
        </div>
        {#each node.rowSchema as child, ci (ci)}
          <SchemaNodeView node={scopedNode(child, index)} />
        {/each}
      </div>
    {/each}
  </div>
  <button type="button" class="fc-btn mt-2 w-full border-dashed" onclick={addRow}>
    Add row
  </button>
</FieldShell>
