<script lang="ts">
  import type { SeparatorNode } from '../../protocol';

  interface Props {
    node: SeparatorNode;
  }

  let { node: _node }: Props = $props();
</script>

<hr class="my-3 border-0 border-t border-zinc-800" />
