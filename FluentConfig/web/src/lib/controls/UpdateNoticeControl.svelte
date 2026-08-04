<script lang="ts">
  import type { UpdateNoticeNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import { appStore } from '../../store/app.svelte';

  interface Props {
    node: UpdateNoticeNode;
  }

  let { node }: Props = $props();
  let busy = $state(false);

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
  class="my-2 rounded-lg border border-amber-500/40 bg-amber-500/10 px-4 py-3 text-sm text-amber-50"
  role="status"
>
  <div class="flex flex-wrap items-start justify-between gap-3">
    <div class="min-w-0 flex-1 space-y-1">
      <p class="font-medium text-amber-100">
        Update available: {node.currentVersion} → {node.latestVersion}
      </p>
      {#if node.repo}
        <p class="text-xs text-amber-200/70">{node.repo}</p>
      {/if}
      {#if node.releaseNotes}
        <p class="text-amber-100/80">{node.releaseNotes}</p>
      {/if}
    </div>
    <div class="flex shrink-0 gap-2">
      <button
        type="button"
        class="rounded-md bg-amber-500 px-3 py-1.5 text-sm font-medium text-zinc-950 hover:bg-amber-400 disabled:opacity-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-300"
        disabled={busy}
        onclick={stage}
      >
        {busy ? 'Staging…' : 'Update'}
      </button>
      {#if node.dismissible !== false}
        <button
          type="button"
          class="rounded-md border border-amber-500/40 px-3 py-1.5 text-sm text-amber-100 hover:bg-amber-500/20 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-amber-300"
          onclick={dismiss}
        >
          Dismiss
        </button>
      {/if}
    </div>
  </div>
</div>
