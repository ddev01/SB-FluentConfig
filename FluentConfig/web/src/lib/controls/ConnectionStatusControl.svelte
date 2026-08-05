<script lang="ts">
  import type { ConnectionStatusNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import FieldShell from './FieldShell.svelte';

  interface Props {
    node: ConnectionStatusNode;
  }

  let { node }: Props = $props();
  let busy = $state(false);

  const statusLabel = $derived(
    node.statusText ??
      (node.status === 'connected'
        ? 'Connected'
        : node.status === 'disconnected'
          ? 'Disconnected'
          : node.status === 'connecting'
            ? 'Connecting…'
            : 'Error'),
  );
  const dotClass = $derived.by(() => {
    switch (node.status) {
      case 'connected':
        return 'bg-fc-success';
      case 'connecting':
        return 'bg-fc-warning animate-pulse';
      case 'error':
        return 'bg-fc-danger';
      case 'disconnected':
      default:
        return 'bg-fc-text-subtle';
    }
  });

  async function onAction(): Promise<void> {
    if (!node.buttonId) return;
    busy = true;
    try {
      const result = await appStore.client().request<{ toast?: string }>(
        RpcMethods.ButtonClick,
        { buttonId: node.buttonId, values: appStore.values },
      );
      if (result?.toast) appStore.pushToast(result.toast);
    } catch (err) {
      appStore.pushToast(err instanceof Error ? err.message : 'Action failed');
    } finally {
      busy = false;
    }
  }
</script>

<FieldShell label={node.label} hint={node.hint}>
  <div class="flex flex-wrap items-center gap-3">
    <div class="flex items-center gap-2 text-sm text-fc-text">
      <span
        class="inline-block size-2.5 shrink-0 rounded-full {dotClass}"
        aria-hidden="true"
      ></span>
      <span>{statusLabel}</span>
    </div>
    {#if node.buttonText && node.buttonId}
      <button
        type="button"
        class="fc-btn"
        disabled={busy}
        onclick={() => void onAction()}
      >
        {busy ? 'Working…' : node.buttonText}
      </button>
    {/if}
  </div>
</FieldShell>
