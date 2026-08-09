<script lang="ts">
  import { getContext } from 'svelte';
  import { FIT_WIDTH_CONTEXT } from '../layout';

  interface Props {
    label?: string;
    hint?: string;
    forId?: string;
    children: import('svelte').Snippet;
  }

  let { label, hint, forId, children }: Props = $props();

  const fitCtx = getContext<{ style?: string } | undefined>(FIT_WIDTH_CONTEXT);
  let fitStyle = $derived(fitCtx?.style);
</script>

<div class="flex flex-col gap-1 py-2">
  {#if label}
    <label class="fc-label" for={forId}>{label}</label>
  {/if}
  {#if fitStyle}
    <div style={fitStyle}>
      {@render children()}
    </div>
  {:else}
    {@render children()}
  {/if}
  {#if hint}
    <p class="fc-hint mt-0.5">{hint}</p>
  {/if}
</div>
