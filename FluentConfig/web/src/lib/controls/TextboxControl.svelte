<script lang="ts">
  import type { TextboxNode } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: TextboxNode;
  }

  let { node }: Props = $props();
  const fieldId = $derived(node.id ?? node.saveKey);

  let value = $derived(
    String(appStore.getValue(node.saveKey) ?? node.defaultValue ?? ''),
  );

  function onInput(e: Event): void {
    const t = e.currentTarget as HTMLInputElement | HTMLTextAreaElement;
    appStore.setValue(node.saveKey, t.value);
  }
</script>

<FieldShell label={node.label} hint={node.hint} forId={fieldId}>
  {#if node.multiline}
    <textarea
      id={fieldId}
      class="fc-input min-h-24"
      rows="4"
      value={value}
      oninput={onInput}
    ></textarea>
  {:else}
    <input
      id={fieldId}
      type={node.password ? 'password' : 'text'}
      class="fc-input"
      value={value}
      oninput={onInput}
      autocomplete="off"
    />
  {/if}
</FieldShell>
