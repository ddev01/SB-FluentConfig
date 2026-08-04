<script lang="ts">
  import type { DescriptionNode } from '../../protocol';

  interface Props {
    node: DescriptionNode;
  }

  let { node }: Props = $props();
</script>

<p class="py-1 text-sm leading-relaxed text-zinc-400">{node.text}</p>
