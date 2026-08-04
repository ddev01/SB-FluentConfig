<script lang="ts">
  import type { ToggleNode } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: ToggleNode;
  }

  let { node }: Props = $props();

  const fieldId = $derived(node.id ?? node.saveKey);

  let plainOn = $derived(
    node.exclusive
      ? false
      : Boolean(appStore.getValue(node.saveKey) ?? node.defaultValue ?? false),
  );

  let exclusiveSelected = $derived.by(() => {
    if (!node.exclusive) return [] as number[];
    const raw = appStore.getValue(node.saveKey);
    if (Array.isArray(raw)) return raw as number[];
    if (node.exclusive.defaultIndices) return [...node.exclusive.defaultIndices];
    if (node.exclusive.defaultIndex !== undefined) return [node.exclusive.defaultIndex];
    return [];
  });

  function togglePlain(): void {
    appStore.setValue(node.saveKey, !plainOn);
  }

  function toggleExclusive(index: number): void {
    if (!node.exclusive) return;
    const max = node.exclusive.maxSelected ?? 1;
    let next = [...exclusiveSelected];
    const at = next.indexOf(index);
    if (at >= 0) {
      next.splice(at, 1);
    } else {
      if (max === 1) next = [index];
      else if (next.length >= max) {
        next.shift();
        next.push(index);
      } else {
        next.push(index);
      }
    }
    appStore.setValue(node.saveKey, next);
  }
</script>

{#if node.exclusive}
  <FieldShell label={node.label} hint={node.hint}>
    <div class="flex flex-wrap gap-2" role="group" aria-label={node.label}>
      {#each node.exclusive.options as option, index (option)}
        {@const on = exclusiveSelected.includes(index)}
        <button
          type="button"
          class="rounded-fc border px-3 py-1.5 text-sm font-medium transition-colors duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50
            {on
            ? 'border-fc-accent bg-fc-accent/15 text-fc-accent'
            : 'border-fc-border bg-fc-surface text-fc-text-muted hover:border-fc-border-strong hover:text-fc-text'}"
          aria-pressed={on}
          onclick={() => toggleExclusive(index)}
        >
          {option}
        </button>
      {/each}
    </div>
  </FieldShell>
{:else}
  <FieldShell label={node.label} hint={node.hint} forId={fieldId}>
    <button
      id={fieldId}
      type="button"
      role="switch"
      aria-checked={plainOn}
      aria-label={node.label}
      class="relative inline-flex h-6 w-11 shrink-0 rounded-full transition-colors duration-200 ease-out focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50 focus-visible:ring-offset-2 focus-visible:ring-offset-fc-bg
        {plainOn
        ? 'bg-fc-accent'
        : 'bg-fc-elevated ring-1 ring-inset ring-fc-border-strong hover:ring-fc-text-subtle'}"
      onclick={togglePlain}
    >
      <span
        class="pointer-events-none absolute inset-y-0 left-0.5 my-auto h-5 w-5 rounded-full bg-white shadow-sm transition-transform duration-200 ease-out dark:bg-fc-bg
          {plainOn ? 'translate-x-5' : 'translate-x-0'}"
      ></span>
    </button>
  </FieldShell>
{/if}
