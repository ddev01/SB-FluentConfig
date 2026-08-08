<script lang="ts">
  import { onMount, tick } from 'svelte';
  import type { UpdateNoticeNode } from '../../protocol';
  import { RpcMethods } from '../../protocol';
  import { appStore } from '../../store/app.svelte';
  import { fadeIn, scaleIn } from '../motion';

  interface Props {
    node: UpdateNoticeNode;
  }

  const NOTES_MAX = 800;

  let { node }: Props = $props();

  let panelEl = $state<HTMLDivElement | null>(null);
  let laterBtn = $state<HTMLButtonElement | null>(null);
  let dismissing = $state(false);

  const title = $derived(
    `Update available: ${node.currentVersion} → ${node.latestVersion}`,
  );

  const displayNotes = $derived.by(() => {
    const notes = node.releaseNotes?.trim();
    if (!notes) return '';
    if (notes.length <= NOTES_MAX) return notes;
    return `${notes.slice(0, NOTES_MAX).trimEnd()}…`;
  });

  onMount(() => {
    void tick().then(() => laterBtn?.focus());
  });

  async function dismiss(reason: 'later' | 'ignoreVersion'): Promise<void> {
    if (dismissing) return;
    dismissing = true;
    try {
      await appStore.client().request(RpcMethods.UpdateDismiss, {
        noticeId: node.id,
        reason,
        ...(reason === 'ignoreVersion' ? { version: node.latestVersion } : {}),
      });
    } catch {
      /* still hide locally */
    }
    appStore.removeUpdateNotice(node.id);
  }

  function openUpdateGuide(): void {
    const url = node.updateGuideUrl ?? node.releasePageUrl;
    if (!url) {
      appStore.pushToast('No update guide URL');
      return;
    }
    appStore.openUrl(url);
  }

  function onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Escape') {
      e.preventDefault();
      void dismiss('later');
      return;
    }
    if (e.key !== 'Tab' || !panelEl) return;
    const focusable = panelEl.querySelectorAll<HTMLElement>(
      'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
    );
    if (focusable.length === 0) return;
    const first = focusable[0]!;
    const last = focusable[focusable.length - 1]!;
    if (e.shiftKey && document.activeElement === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && document.activeElement === last) {
      e.preventDefault();
      first.focus();
    }
  }
</script>

<svelte:window onkeydown={onKeydown} />

<div
  class="fixed inset-0 z-[60] flex items-center justify-center bg-fc-bg/70 p-4 backdrop-blur-sm"
  transition:fadeIn={{ duration: 0.15 }}
  role="presentation"
  onclick={(e) => {
    if (e.target === e.currentTarget) void dismiss('later');
  }}
>
  <div
    bind:this={panelEl}
    class="fc-card w-full max-w-md bg-fc-elevated/90 p-5 shadow-fc-glow"
    transition:scaleIn={{ duration: 0.18 }}
    role="alertdialog"
    aria-modal="true"
    aria-labelledby="fc-update-title"
    aria-describedby="fc-update-body"
  >
    <h2 id="fc-update-title" class="text-base font-semibold text-fc-text">
      {title}
    </h2>

    <div id="fc-update-body" class="mt-2 space-y-2">
      {#if node.repo}
        <p class="text-xs text-fc-text-subtle">{node.repo}</p>
      {/if}
      {#if displayNotes}
        <p class="max-h-64 overflow-y-auto text-sm whitespace-pre-wrap text-fc-text-muted">
          {displayNotes}
        </p>
      {/if}
    </div>

    <div class="mt-5 flex flex-col gap-3">
      <div class="flex flex-wrap justify-end gap-2">
        <button
          type="button"
          class="fc-btn"
          bind:this={laterBtn}
          disabled={dismissing}
          onclick={() => void dismiss('later')}
        >
          Later
        </button>
        <button type="button" class="fc-btn-accent" onclick={openUpdateGuide}>
          How to update
        </button>
      </div>
      {#if node.dismissible !== false}
        <button
          type="button"
          class="self-end text-sm text-fc-text-subtle transition-colors duration-150 hover:text-fc-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fc-ring/50"
          disabled={dismissing}
          onclick={() => void dismiss('ignoreVersion')}
        >
          Don't ask for this version
        </button>
      {/if}
    </div>
  </div>
</div>
