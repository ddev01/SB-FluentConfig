<script lang="ts">
  import TrashIcon from '../icons/TrashIcon.svelte';

  interface Props {
    values: string[];
    labelFor: (value: string) => string;
    onremove: (value: string) => void;
  }

  let { values, labelFor, onremove }: Props = $props();
</script>

{#if values.length > 0}
  <div class="mb-1 flex flex-wrap gap-1">
    {#each values as v (v)}
      <span
        class="inline-flex items-center gap-1 rounded-fc bg-fc-accent/15 px-1.5 py-0.5 text-xs text-fc-accent"
      >
        {labelFor(v)}
        <button
          type="button"
          class="flex h-4 w-4 shrink-0 items-center justify-center rounded text-fc-text-subtle transition-colors hover:text-fc-danger"
          aria-label="Remove {labelFor(v)}"
          onclick={() => onremove(v)}
        >
          <TrashIcon class="h-3 w-3" />
        </button>
      </span>
    {/each}
  </div>
{/if}
