<script lang="ts">
  import { onMount, tick } from 'svelte';
  import { fadeIn, scaleIn } from '../motion';

  interface Props {
    title: string;
    message: string;
    confirmText?: string;
    cancelText?: string;
    onconfirm: () => void;
    oncancel: () => void;
  }

  let {
    title,
    message,
    confirmText = 'Confirm',
    cancelText = 'Cancel',
    onconfirm,
    oncancel,
  }: Props = $props();

  let panelEl = $state<HTMLDivElement | null>(null);
  let cancelBtn = $state<HTMLButtonElement | null>(null);

  // Default focus on the non-destructive cancel action (a11y).
  onMount(() => {
    void tick().then(() => cancelBtn?.focus());
  });

  function onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Escape') {
      e.preventDefault();
      oncancel();
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

<!-- svelte-ignore a11y_no_static_element_interactions a11y_click_events_have_key_events -->
<div
  class="fixed inset-0 z-[60] flex items-center justify-center bg-fc-bg/70 p-4 backdrop-blur-sm"
  transition:fadeIn={{ duration: 0.15 }}
  role="presentation"
  onclick={(e) => {
    if (e.target === e.currentTarget) oncancel();
  }}
>
  <div
    bind:this={panelEl}
    class="fc-card w-full max-w-sm bg-fc-elevated/90 p-5 shadow-fc-glow"
    transition:scaleIn={{ duration: 0.18 }}
    role="alertdialog"
    aria-modal="true"
    aria-labelledby="fc-confirm-title"
    aria-describedby="fc-confirm-message"
  >
    <h2 id="fc-confirm-title" class="text-base font-semibold text-fc-text">
      {title}
    </h2>
    <p id="fc-confirm-message" class="mt-2 text-sm whitespace-pre-wrap text-fc-text-muted">
      {message}
    </p>
    <div class="mt-5 flex justify-end gap-2">
      <button type="button" class="fc-btn" bind:this={cancelBtn} onclick={oncancel}>
        {cancelText}
      </button>
      <button type="button" class="fc-btn-accent" onclick={onconfirm}>
        {confirmText}
      </button>
    </div>
  </div>
</div>
