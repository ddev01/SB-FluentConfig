<script lang="ts">
  import type { SeparatorNode } from '../../protocol';

  interface Props {
    node: SeparatorNode;
  }

  let { node: _node }: Props = $props();
</script>

<hr
  class="my-3 h-px border-0 bg-gradient-to-r from-transparent via-fc-border-strong to-transparent"
/>
