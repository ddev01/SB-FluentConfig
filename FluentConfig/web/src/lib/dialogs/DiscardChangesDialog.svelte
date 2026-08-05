<script lang="ts">
  import { onMount, tick } from 'svelte';
  import { fadeIn, scaleIn } from '../motion';
  import { formatDiffValue, type DiffEntry } from '../diff';

  interface Props {
    entries: DiffEntry[];
    dontRemindInitial?: boolean;
    saving?: boolean;
    /** Keep editing — still pass checkbox so "don't remind" can persist without closing. */
    onkeep: (dontRemindAgain: boolean) => void;
    onsaveexit: (dontRemindAgain: boolean) => void;
    ondiscard: (dontRemindAgain: boolean) => void;
  }

  let {
    entries,
    dontRemindInitial = false,
    saving = false,
    onkeep,
    onsaveexit,
    ondiscard,
  }: Props = $props();

  // Writable local copy — initialized once when the dialog mounts.
  let dontRemind = $state(false);
  let panelEl = $state<HTMLDivElement | null>(null);
  let keepBtn = $state<HTMLButtonElement | null>(null);

  onMount(() => {
    dontRemind = dontRemindInitial;
    void tick().then(() => keepBtn?.focus());
  });

  function onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Escape') {
      e.preventDefault();
      onkeep(dontRemind);
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
    if (e.target === e.currentTarget) onkeep(dontRemind);
  }}
>
  <div
    bind:this={panelEl}
    class="fc-card flex max-h-[min(80vh,36rem)] w-full max-w-lg flex-col bg-fc-elevated/90 p-5 shadow-fc-glow"
    transition:scaleIn={{ duration: 0.18 }}
    role="alertdialog"
    aria-modal="true"
    aria-labelledby="fc-discard-title"
    aria-describedby="fc-discard-message"
  >
    <h2 id="fc-discard-title" class="text-base font-semibold text-fc-text">
      Discard unsaved changes?
    </h2>
    <p id="fc-discard-message" class="mt-2 text-sm text-fc-text-muted">
      You have unsaved edits. Closing will discard them.
    </p>

    <div
      class="mt-4 min-h-0 flex-1 overflow-y-auto rounded-fc border border-fc-border/60 bg-fc-bg/40"
      role="list"
      aria-label="Changed settings"
    >
      {#each entries as entry (entry.path)}
        <div
          class="border-b border-fc-border/40 px-3 py-2 last:border-b-0"
          role="listitem"
        >
          <p class="font-mono text-xs font-medium text-fc-text">{entry.path}</p>
          <div class="mt-1 grid gap-1 text-xs sm:grid-cols-2">
            <p class="text-fc-text-subtle">
              <span class="text-fc-text-muted">Saved:</span>
              <span class="break-all text-fc-text-muted">
                {formatDiffValue(entry.saved)}
              </span>
            </p>
            <p class="text-fc-text-subtle">
              <span class="text-fc-warning">Current:</span>
              <span class="break-all text-fc-text">
                {formatDiffValue(entry.current)}
              </span>
            </p>
          </div>
        </div>
      {/each}
      {#if entries.length === 0}
        <p class="px-3 py-2 text-xs text-fc-text-muted">No leaf diffs.</p>
      {/if}
    </div>

    <label class="mt-4 flex cursor-pointer items-center gap-2 text-sm text-fc-text-muted">
      <input
        type="checkbox"
        class="size-4 rounded border-fc-border accent-fc-accent"
        bind:checked={dontRemind}
      />
      Don't remind me again
    </label>

    <div class="mt-5 flex flex-wrap justify-end gap-2">
      <button
        type="button"
        class="fc-btn-danger"
        disabled={saving}
        onclick={() => ondiscard(dontRemind)}
      >
        Discard & Exit
      </button>
      <button
        type="button"
        class="fc-btn"
        bind:this={keepBtn}
        onclick={() => onkeep(dontRemind)}
      >
        Keep editing
      </button>
      <button
        type="button"
        class="fc-btn-accent"
        disabled={saving}
        onclick={() => onsaveexit(dontRemind)}
      >
        {saving ? 'Saving…' : 'Save & Exit'}
      </button>
    </div>
  </div>
</div>
