<script lang="ts">
  import type { ColorPickerNode } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: ColorPickerNode;
  }

  let { node }: Props = $props();
  const fieldId = $derived(node.id ?? node.saveKey);

  let value = $derived(
    String(appStore.getValue(node.saveKey) ?? node.defaultValue ?? '#000000'),
  );

  function normalize(raw: string): string {
    const t = raw.trim();
    if (/^#[0-9a-fA-F]{6}$/.test(t)) return t.toLowerCase();
    if (/^[0-9a-fA-F]{6}$/.test(t)) return `#${t.toLowerCase()}`;
    return t;
  }

  function onColor(e: Event): void {
    const t = e.currentTarget as HTMLInputElement;
    appStore.setValue(node.saveKey, t.value);
  }

  function onText(e: Event): void {
    const t = e.currentTarget as HTMLInputElement;
    appStore.setValue(node.saveKey, normalize(t.value));
  }
</script>

<FieldShell label={node.label} hint={node.hint} forId={fieldId}>
  <div class="flex items-center gap-2">
    <input
      id={fieldId}
      type="color"
      class="h-9 w-12 cursor-pointer rounded border border-zinc-700 bg-zinc-900 p-0.5 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-sky-500"
      value={/^#[0-9a-fA-F]{6}$/.test(value) ? value : '#000000'}
      oninput={onColor}
    />
    <input
      type="text"
      class="fc-input font-mono"
      value={value}
      oninput={onText}
      spellcheck="false"
      aria-label={`${node.label} hex`}
    />
  </div>
</FieldShell>
