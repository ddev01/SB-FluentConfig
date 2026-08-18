<script lang="ts">
  import type { GroupNode } from '../../protocol';
  import { schemaNodeEachKey } from '../schemaNodeKey';
  import SchemaNodeView from './SchemaNodeView.svelte';

  interface Props {
    node: GroupNode;
  }

  let { node }: Props = $props();

  /**
   * RepeatFor wraps indices above the driver's min in a visibility group
   * (`id` = `repeat_{driver}_{i}`). Those gates must stay visually flat so
   * gated rows align with ungated siblings. Toggle WithVisibility groups
   * omit `indented` and keep the left-border stack. Value-equals / comparator
   * groups set `indented: false`.
   */
  const isRepeatForGate = $derived(
    typeof node.id === 'string' && node.id.startsWith('repeat_'),
  );
  const isFlat = $derived(node.indented === false || isRepeatForGate);

  const gridStyle = $derived.by(() => {
    const grid = node.grid;
    if (!grid) return '';

    const gapPx = (grid.gap ?? 3) * 4;
    const parts = [`gap: ${gapPx}px`];

    if (grid.mode === 'row') {
      parts.unshift('display: flex', 'flex-wrap: wrap');
    } else {
      const columns = grid.columns ?? 1;
      parts.unshift(
        'display: grid',
        `grid-template-columns: repeat(${columns}, minmax(0, 1fr))`,
      );
    }

    if (grid.align) {
      parts.push(`align-items: ${grid.align}`);
    }

    return parts.join('; ');
  });
</script>

{#snippet children()}
  {#each node.children ?? [] as child, i (schemaNodeEachKey(child, i))}
    <SchemaNodeView node={child} />
  {/each}
{/snippet}

{#if node.grid}
  <div style={gridStyle}>
    {@render children()}
  </div>
{:else if isFlat}
  {@render children()}
{:else}
  <div class="space-y-1 border-l-2 border-fc-accent/25 pl-3">
    {@render children()}
  </div>
{/if}
