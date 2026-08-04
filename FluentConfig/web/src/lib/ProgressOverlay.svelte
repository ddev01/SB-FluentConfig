<script lang="ts">
  import { appStore } from '../store/app.svelte';

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
  <div
    class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
    role="dialog"
    aria-modal="true"
    aria-labelledby="progress-title"
  >
    <div
      class="w-full max-w-sm rounded-lg border border-zinc-700 bg-zinc-900 p-5 shadow-xl"
    >
      <h2 id="progress-title" class="text-base font-semibold text-zinc-50">
        {p.title ?? 'Working…'}
      </h2>
      {#if p.message}
        <p class="mt-1 text-sm text-zinc-400">{p.message}</p>
      {/if}
      <div class="mt-4 h-2 overflow-hidden rounded-full bg-zinc-800">
        <div
          class="h-full rounded-full bg-sky-500 transition-[width] duration-150"
          style:width={`${percent}%`}
        ></div>
      </div>
      <p class="mt-2 text-right font-mono text-xs text-zinc-500 tabular-nums">
        {#if p.current != null && p.total != null}
          {p.current} / {p.total}
        {:else}
          {percent}%
        {/if}
      </p>
    </div>
  </div>
{/if}
