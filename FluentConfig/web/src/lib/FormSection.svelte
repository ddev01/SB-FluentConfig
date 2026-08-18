<script lang="ts">
  import type { SectionSchema } from '../protocol';
  import { schemaNodeEachKey } from './schemaNodeKey';
  import SchemaNodeView from './controls/SchemaNodeView.svelte';

  interface Props {
    section: SectionSchema;
  }

  let { section }: Props = $props();
</script>

<section class="space-y-1" aria-labelledby={`section-${section.id}`}>
  <h2
    id={`section-${section.id}`}
    class="mb-3 flex items-center gap-2 text-lg font-semibold tracking-tight text-fc-text"
  >
    <span
      class="h-4 w-1 rounded-full bg-fc-accent shadow-fc-glow"
      aria-hidden="true"
    ></span>
    {section.title}
  </h2>
  {#each section.children as child, i (schemaNodeEachKey(child, i))}
    <SchemaNodeView node={child} />
  {/each}
</section>
