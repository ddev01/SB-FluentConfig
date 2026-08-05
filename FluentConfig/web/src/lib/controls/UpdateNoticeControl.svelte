<script lang="ts">
  import type { UpdateNoticeNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import { appStore } from '../../store/app.svelte';

  interface Props {
    node: UpdateNoticeNode;
  }

  let { node }: Props = $props();
  let busy = $state(false);

  const isNotify = $derived(node.mode === 'notify');

  async function stage(): Promise<void> {
    busy = true;
    try {
      await appStore.client().request(RpcMethods.UpdateStage, {
        downloadUrl: node.downloadUrl,
        noticeId: node.id,
      });
      appStore.pushToast('Update staging started');
    } catch (err) {
      appStore.pushToast(err instanceof Error ? err.message : 'Update failed');
    } finally {
      busy = false;
    }
  }

  function openRelease(): void {
    const url = node.releasePageUrl;
    if (!url) {
      appStore.pushToast('No release page URL');
      return;
    }
    appStore.openUrl(url);
  }

  async function dismiss(): Promise<void> {
    try {
      await appStore.client().request(RpcMethods.UpdateDismiss, {
        noticeId: node.id,
      });
    } catch {
      /* still hide locally */
    }
    appStore.removeUpdateNotice(node.id);
  }
</script>

<div
  class="my-2 rounded-fc-lg border border-fc-warning/40 bg-fc-warning/10 px-4 py-3 text-sm text-fc-text"
  role="status"
>
  <div class="flex flex-wrap items-start justify-between gap-3">
    <div class="min-w-0 flex-1 space-y-1">
      <p class="font-medium text-fc-warning">
        Update available: {node.currentVersion} → {node.latestVersion}
      </p>
      {#if node.repo}
        <p class="text-xs text-fc-text-subtle">{node.repo}</p>
      {/if}
      {#if node.releaseNotes}
        <p class="text-fc-text-muted">{node.releaseNotes}</p>
      {/if}
    </div>
    <div class="flex shrink-0 gap-2">
      {#if isNotify}
        <button
          type="button"
          class="rounded-fc bg-fc-warning px-3 py-1.5 text-sm font-semibold text-fc-bg transition-[filter] duration-150 hover:brightness-110 active:brightness-95 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50 focus-visible:ring-offset-2 focus-visible:ring-offset-fc-bg"
          onclick={openRelease}
        >
          View release
        </button>
      {:else}
        <button
          type="button"
          class="rounded-fc bg-fc-warning px-3 py-1.5 text-sm font-semibold text-fc-bg transition-[filter] duration-150 hover:brightness-110 active:brightness-95 disabled:pointer-events-none disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50 focus-visible:ring-offset-2 focus-visible:ring-offset-fc-bg"
          disabled={busy}
          onclick={stage}
        >
          {busy ? 'Staging…' : 'Update'}
        </button>
      {/if}
      {#if node.dismissible !== false}
        <button
          type="button"
          class="rounded-fc border border-fc-warning/40 px-3 py-1.5 text-sm font-medium text-fc-text transition-colors duration-150 hover:bg-fc-warning/20 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50"
          onclick={dismiss}
        >
          Dismiss
        </button>
      {/if}
    </div>
  </div>
</div>
