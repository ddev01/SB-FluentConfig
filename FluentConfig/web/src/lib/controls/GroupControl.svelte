<script lang="ts">
  import type { GroupNode } from '../../protocol';
  import SchemaNodeView from './SchemaNodeView.svelte';

  interface Props {
    node: GroupNode;
  }

  let { node }: Props = $props();
</script>

<div class="space-y-1 border-l-2 border-zinc-800 pl-3">
  {#each node.children as child, i (child.type + String('id' in child ? child.id : i))}
    <SchemaNodeView node={child} />
  {/each}
</div>
