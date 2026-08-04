<script lang="ts">
  import type { SliderNode } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: SliderNode;
  }

  let { node }: Props = $props();
  const fieldId = $derived(node.id ?? node.saveKey);

  let value = $derived(
    Number(appStore.getValue(node.saveKey) ?? node.defaultValue ?? node.min),
  );

  function onInput(e: Event): void {
    const t = e.currentTarget as HTMLInputElement;
    appStore.setValue(node.saveKey, Number(t.value));
  }
</script>

<FieldShell label={node.label} hint={node.hint} forId={fieldId}>
  <div class="flex items-center gap-3">
    <input
      id={fieldId}
      type="range"
      class="h-2 w-full cursor-pointer appearance-none rounded-full bg-zinc-700 accent-sky-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-500"
      min={node.min}
      max={node.max}
      value={value}
      oninput={onInput}
    />
    <span class="w-14 shrink-0 text-right font-mono text-sm text-zinc-300 tabular-nums"
      >{value}</span
    >
  </div>
</FieldShell>
