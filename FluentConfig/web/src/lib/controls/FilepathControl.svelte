<script lang="ts">
  import type { FilepathNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import type { FilepathBrowseResult } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: FilepathNode;
  }

  let { node }: Props = $props();
  const fieldId = $derived(node.id ?? node.saveKey);
  let browsing = $state(false);

  let value = $derived(
    String(appStore.getValue(node.saveKey) ?? node.defaultValue ?? ''),
  );

  function onInput(e: Event): void {
    appStore.setValue(node.saveKey, (e.currentTarget as HTMLInputElement).value);
  }

  async function browse(): Promise<void> {
    browsing = true;
    try {
      const result = await appStore.client().request<FilepathBrowseResult>(
        RpcMethods.FilepathBrowse,
        { saveKey: node.saveKey },
      );
      if (result.path) appStore.setValue(node.saveKey, result.path);
    } catch (err) {
      appStore.pushToast(err instanceof Error ? err.message : 'Browse failed');
    } finally {
      browsing = false;
    }
  }
</script>

<FieldShell label={node.label} hint={node.hint} forId={fieldId}>
  <div class="flex gap-2">
    <input
      id={fieldId}
      type="text"
      class="fc-input font-mono"
      value={value}
      oninput={onInput}
      spellcheck="false"
    />
    <button
      type="button"
      class="fc-btn"
      disabled={browsing}
      onclick={browse}
    >
      Browse…
    </button>
  </div>
</FieldShell>
