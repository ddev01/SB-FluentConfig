<script lang="ts">
  import { onMount, tick } from 'svelte';
  import { fadeIn, scaleIn } from '../motion';

  interface Props {
    title: string;
    message: string;
    onclose: () => void;
  }

  let { title, message, onclose }: Props = $props();

  let panelEl = $state<HTMLDivElement | null>(null);
  let okBtn = $state<HTMLButtonElement | null>(null);

  onMount(() => {
    void tick().then(() => okBtn?.focus());
  });

  function onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Escape') {
      e.preventDefault();
      onclose();
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
    if (e.target === e.currentTarget) onclose();
  }}
>
  <div
    bind:this={panelEl}
    class="fc-card w-full max-w-sm bg-fc-elevated/90 p-5 shadow-fc-glow"
    transition:scaleIn={{ duration: 0.18 }}
    role="alertdialog"
    aria-modal="true"
    aria-labelledby="fc-popup-title"
    aria-describedby="fc-popup-message"
  >
    <h2 id="fc-popup-title" class="text-base font-semibold text-fc-text">
      {title}
    </h2>
    <p id="fc-popup-message" class="mt-2 text-sm whitespace-pre-wrap text-fc-text-muted">
      {message}
    </p>
    <div class="mt-5 flex justify-end">
      <button type="button" class="fc-btn-accent" bind:this={okBtn} onclick={onclose}>
        OK
      </button>
    </div>
  </div>
</div>
