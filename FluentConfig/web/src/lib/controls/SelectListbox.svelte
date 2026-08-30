<script lang="ts">
  import { scaleIn } from '../motion';
  import type { SelectMenuOption } from '../selectMenuState';

  interface Props {
    listboxId: string;
    suggestionOptions: SelectMenuOption[];
    activeIndex: number;
    value: string;
    selectedValues: string[];
    emptyListMessage: string;
    optionId: (index: number) => string;
    optionEls: (HTMLLIElement | null)[];
    onactive: (index: number) => void;
    onchoose: (index: number) => void;
  }

  let {
    listboxId,
    suggestionOptions,
    activeIndex,
    value,
    selectedValues,
    emptyListMessage,
    optionId,
    optionEls,
    onactive,
    onchoose,
  }: Props = $props();
</script>

<ul
  id={listboxId}
  role="listbox"
  tabindex="-1"
  class="fc-card absolute z-20 mt-1.5 max-h-64 w-full origin-top overflow-auto p-1 shadow-[0_16px_40px_-12px_rgba(0,0,0,0.55)]"
  transition:scaleIn={{ duration: 0.12 }}
>
  {#each suggestionOptions as opt, index (opt.value)}
    <!-- svelte-ignore a11y_click_events_have_key_events -->
    <li
      id={optionId(index)}
      bind:this={optionEls[index]}
      role="option"
      aria-selected={opt.value === value || selectedValues.includes(opt.value)}
      class="flex items-center justify-between gap-2 rounded-fc px-2.5 py-1.5 text-sm transition-colors duration-100
        {index === activeIndex
        ? 'bg-fc-accent/15 text-fc-accent'
        : opt.value === value
          ? 'text-fc-accent'
          : 'text-fc-text'}"
      onmouseenter={() => onactive(index)}
      onmousedown={(e) => e.preventDefault()}
      onclick={() => onchoose(index)}
    >
      <span class="truncate">{opt.display}</span>
    </li>
  {/each}
  {#if suggestionOptions.length === 0}
    <li class="px-2.5 py-1.5 text-sm text-fc-text-subtle">
      {emptyListMessage}
    </li>
  {/if}
</ul>
