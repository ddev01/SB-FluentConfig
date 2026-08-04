<script lang="ts">
  interface Props {
    label?: string;
    hint?: string;
    forId?: string;
    children: import('svelte').Snippet;
  }

  let { label, hint, forId, children }: Props = $props();
</script>

<div class="flex flex-col gap-1.5 py-2">
  {#if label}
    <label class="fc-label" for={forId}>{label}</label>
  {/if}
  {@render children()}
  {#if hint}
    <p class="fc-hint">{hint}</p>
  {/if}
</div>
