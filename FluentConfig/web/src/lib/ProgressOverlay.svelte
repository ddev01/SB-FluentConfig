<script lang="ts">
  import { appStore } from '../store/app.svelte';
  import { fadeIn, scaleIn } from './motion';

  let p = $derived(appStore.progress);
  let percent = $derived.by(() => {
    if (!p) return 0;
    if (p.percent != null) return Math.min(100, Math.max(0, p.percent));
    if (p.current != null && p.total != null && p.total > 0) {
      return Math.round((p.current / p.total) * 100);
    }
    return 0;
  });
</script>

{#if p}
  {@const progress = p}
  <div
    class="fixed inset-0 z-50 flex items-center justify-center bg-fc-bg/70 p-4 backdrop-blur-sm"
    role="dialog"
    aria-modal="true"
    aria-labelledby="progress-title"
    transition:fadeIn={{ duration: 0.15 }}
  >
    <div
      class="fc-card w-full max-w-sm bg-fc-elevated/90 p-5 shadow-fc-glow"
      transition:scaleIn={{ duration: 0.18 }}
    >
      <h2 id="progress-title" class="text-base font-semibold text-fc-text">
        {progress.title ?? 'Working…'}
      </h2>
      {#if progress.message}
        <p class="mt-1 text-sm text-fc-text-muted">{progress.message}</p>
      {/if}
      <div class="mt-4 h-2 overflow-hidden rounded-full bg-fc-surface">
        <div
          class="h-full rounded-full bg-fc-accent shadow-fc-glow transition-[width] duration-150"
          style:width={`${percent}%`}
        ></div>
      </div>
      <p class="mt-2 text-right font-mono text-xs text-fc-text-subtle tabular-nums">
        {#if progress.current != null && progress.total != null}
          {progress.current} / {progress.total}
        {:else}
          {percent}%
        {/if}
      </p>
    </div>
  </div>
{/if}
