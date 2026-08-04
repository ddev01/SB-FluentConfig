<script lang="ts">
  import type { TitleNode } from '../../protocol';

  interface Props {
    node: TitleNode;
  }

  let { node }: Props = $props();
</script>

<h3 class="pt-3 pb-1 text-base font-semibold text-fc-text">{node.text}</h3>
