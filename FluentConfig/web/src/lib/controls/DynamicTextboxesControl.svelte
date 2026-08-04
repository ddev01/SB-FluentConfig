<script lang="ts">
  import type { DynamicTextboxesNode } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: DynamicTextboxesNode;
  }

  let { node }: Props = $props();
  let draft = $state('');

  let items = $derived.by(() => {
    const raw = appStore.getValue(node.saveKey);
    if (Array.isArray(raw)) return raw.map(String);
    if (node.preset) return [...node.preset];
    return [] as string[];
  });

  function setItems(next: string[]): void {
    appStore.setValue(node.saveKey, next);
  }

  function add(): void {
    const v = draft.trim();
    if (!v) return;
    if (!node.allowDuplicates && items.includes(v)) {
      draft = '';
      return;
    }
    setItems([...items, v]);
    draft = '';
  }

  function remove(index: number): void {
    setItems(items.filter((_, i) => i !== index));
  }

  function updateAt(index: number, value: string): void {
    const next = [...items];
    next[index] = value;
    setItems(next);
  }
</script>

<FieldShell label={node.label} hint={node.hint}>
  <ul class="space-y-2">
    {#each items as item, index (index)}
      <li class="flex gap-2">
        <input
          type="text"
          class="fc-input"
          value={item}
          oninput={(e) => updateAt(index, (e.currentTarget as HTMLInputElement).value)}
        />
        <button
          type="button"
          class="fc-btn text-zinc-500 hover:border-red-500/50 hover:text-red-500"
          aria-label="Remove"
          onclick={() => remove(index)}
        >
          ✕
        </button>
      </li>
    {/each}
  </ul>
  <div class="mt-2 flex gap-2">
    <input
      type="text"
      class="fc-input"
      placeholder="Add entry…"
      bind:value={draft}
      onkeydown={(e) => {
        if (e.key === 'Enter') {
          e.preventDefault();
          add();
        }
      }}
    />
    <button type="button" class="fc-btn" onclick={add}>
      Add
    </button>
  </div>
</FieldShell>
