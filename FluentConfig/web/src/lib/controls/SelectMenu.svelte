<script lang="ts">
  import { scaleIn } from '../motion';

  export interface SelectMenuOption {
    value: string;
    display: string;
  }

  interface Props {
    id?: string;
    value: string;
    options: SelectMenuOption[];
    placeholder?: string;
    ariaLabel?: string;
    disabled?: boolean;
    onchange: (value: string) => void;
  }

  let {
    id,
    value,
    options,
    placeholder = 'Select…',
    ariaLabel,
    disabled = false,
    onchange,
  }: Props = $props();

  const uid = `fc-select-${Math.random().toString(36).slice(2, 9)}`;
  const listboxId = `${uid}-listbox`;
  const optionId = (index: number) => `${listboxId}-opt-${index}`;

  let open = $state(false);
  let activeIndex = $state(-1);
  let containerEl = $state<HTMLDivElement | null>(null);
  let triggerEl = $state<HTMLButtonElement | null>(null);
  let optionEls: (HTMLLIElement | null)[] = [];

  let selectedIndex = $derived(options.findIndex((o) => o.value === value));
  let selectedOption = $derived(selectedIndex >= 0 ? options[selectedIndex] : undefined);
  let activeOptionId = $derived(
    open && activeIndex >= 0 && activeIndex < options.length ? optionId(activeIndex) : undefined,
  );

  function openMenu(): void {
    if (disabled || options.length === 0) return;
    activeIndex = selectedIndex >= 0 ? selectedIndex : 0;
    open = true;
  }

  function closeMenu(focusTrigger = false): void {
    open = false;
    if (focusTrigger) triggerEl?.focus();
  }

  function toggleMenu(): void {
    if (open) closeMenu();
    else openMenu();
  }

  function choose(index: number): void {
    const opt = options[index];
    if (!opt) return;
    if (opt.value !== value) onchange(opt.value);
    closeMenu(true);
  }

  function moveActive(delta: number): void {
    if (options.length === 0) return;
    activeIndex = (activeIndex + delta + options.length) % options.length;
    optionEls[activeIndex]?.scrollIntoView({ block: 'nearest' });
  }

  function onTriggerKeydown(e: KeyboardEvent): void {
    if (disabled) return;
    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        if (!open) openMenu();
        else moveActive(1);
        break;
      case 'ArrowUp':
        e.preventDefault();
        if (!open) openMenu();
        else moveActive(-1);
        break;
      case 'Enter':
      case ' ':
        e.preventDefault();
        if (!open) openMenu();
        else choose(activeIndex);
        break;
      case 'Escape':
        if (open) {
          e.preventDefault();
          closeMenu();
        }
        break;
      case 'Home':
        if (open) {
          e.preventDefault();
          activeIndex = 0;
        }
        break;
      case 'End':
        if (open) {
          e.preventDefault();
          activeIndex = options.length - 1;
        }
        break;
      case 'Tab':
        if (open) closeMenu();
        break;
    }
  }

  function onWindowClick(e: MouseEvent): void {
    if (!open) return;
    if (containerEl && !containerEl.contains(e.target as Node)) closeMenu();
  }
</script>

<svelte:window onclick={onWindowClick} />

<div class="relative" bind:this={containerEl}>
  <button
    {id}
    bind:this={triggerEl}
    type="button"
    role="combobox"
    aria-haspopup="listbox"
    aria-expanded={open}
    aria-controls={listboxId}
    aria-activedescendant={activeOptionId}
    aria-label={ariaLabel}
    {disabled}
    class="fc-input flex w-full items-center justify-between gap-2 text-left
      {open ? 'border-fc-accent ring-2 ring-fc-ring/40' : ''}"
    onclick={toggleMenu}
    onkeydown={onTriggerKeydown}
  >
    <span class="truncate {selectedOption ? 'text-fc-text' : 'text-fc-text-subtle'}">
      {selectedOption?.display ?? placeholder}
    </span>
    <svg
      class="h-4 w-4 shrink-0 text-fc-text-subtle transition-transform duration-150
        {open ? 'rotate-180 text-fc-accent' : ''}"
      viewBox="0 0 20 20"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M5 7.5L10 12.5L15 7.5"
        stroke="currentColor"
        stroke-width="1.6"
        stroke-linecap="round"
        stroke-linejoin="round"
      />
    </svg>
  </button>

  {#if open}
    <ul
      id={listboxId}
      role="listbox"
      tabindex="-1"
      class="fc-card absolute z-20 mt-1.5 max-h-64 w-full origin-top overflow-auto p-1 shadow-[0_16px_40px_-12px_rgba(0,0,0,0.55)]"
      transition:scaleIn={{ duration: 0.12 }}
    >
      {#each options as opt, index (opt.value)}
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <li
          id={optionId(index)}
          bind:this={optionEls[index]}
          role="option"
          aria-selected={opt.value === value}
          class="flex items-center justify-between gap-2 rounded-fc px-2.5 py-1.5 text-sm transition-colors duration-100
            {index === activeIndex
            ? 'bg-fc-accent/15 text-fc-accent'
            : opt.value === value
              ? 'text-fc-accent'
              : 'text-fc-text'}"
          onmouseenter={() => (activeIndex = index)}
          onclick={() => choose(index)}
        >
          <span class="truncate">{opt.display}</span>
          {#if opt.value === value}
            <svg class="h-3.5 w-3.5 shrink-0" viewBox="0 0 20 20" fill="none" aria-hidden="true">
              <path
                d="M4 10.5L8 14.5L16 6"
                stroke="currentColor"
                stroke-width="1.8"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          {/if}
        </li>
      {/each}
      {#if options.length === 0}
        <li class="px-2.5 py-1.5 text-sm text-fc-text-subtle">No options</li>
      {/if}
    </ul>
  {/if}
</div>
