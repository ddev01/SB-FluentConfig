<script lang="ts">
  import { onMount } from 'svelte';
  import type { FilepathNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import type { FilepathBrowseResult, FilepathValidateResult } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: FilepathNode;
  }

  let { node }: Props = $props();
  const fieldId = $derived(node.id ?? node.saveKey);
  let browsing = $state(false);
  let error = $state<string | null>(null);
  let seq = 0;
  let timer: ReturnType<typeof setTimeout> | undefined;

  let value = $derived(
    String(appStore.getValue(node.saveKey) ?? node.defaultValue ?? ''),
  );

  async function validate(path: string): Promise<void> {
    const trimmed = path.trim();
    if (!trimmed) {
      error = null;
      return;
    }
    const token = ++seq;
    try {
      const result = await appStore.client().request<FilepathValidateResult>(
        RpcMethods.FilepathValidate,
        { saveKey: node.saveKey, path: trimmed },
      );
      if (token !== seq) return;
      error = result.ok ? null : (result.error ?? 'Invalid file path.');
    } catch {
      if (token !== seq) return;
      error = null;
    }
  }

  function scheduleValidate(path: string): void {
    if (timer) clearTimeout(timer);
    timer = setTimeout(() => {
      void validate(path);
    }, 400);
  }

  function onInput(e: Event): void {
    const next = (e.currentTarget as HTMLInputElement).value;
    appStore.setValue(node.saveKey, next);
    scheduleValidate(next);
  }

  onMount(() => {
    scheduleValidate(value);
    return () => {
      if (timer) clearTimeout(timer);
    };
  });

  async function browse(): Promise<void> {
    browsing = true;
    try {
      const result = await appStore.client().request<FilepathBrowseResult>(
        RpcMethods.FilepathBrowse,
        { saveKey: node.saveKey },
      );
      if (result.path) {
        appStore.setValue(node.saveKey, result.path);
        await validate(result.path);
      }
    } catch (err) {
      appStore.pushToast(err instanceof Error ? err.message : 'Browse failed');
    } finally {
      browsing = false;
    }
  }
</script>

<FieldShell label={node.label} hint={node.hint} error={error} forId={fieldId}>
  <div class="flex gap-2">
    <input
      id={fieldId}
      type="text"
      class="fc-input font-mono {error ? 'border-fc-danger focus-visible:border-fc-danger' : ''}"
      value={value}
      oninput={onInput}
      onblur={() => void validate(value)}
      spellcheck="false"
      aria-invalid={error ? true : undefined}
    />
    {#if !node.hideBrowse}
      <button
        type="button"
        class="fc-btn"
        disabled={browsing}
        onclick={browse}
      >
        Browse…
      </button>
    {/if}
  </div>
</FieldShell>
