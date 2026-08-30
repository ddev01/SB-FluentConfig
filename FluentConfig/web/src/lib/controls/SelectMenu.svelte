<script lang="ts">
  import type { Snippet } from 'svelte';
  import {
    canOpenMenu,
    chipLabel as labelForValue,
    computeEmptyListMessage,
    computeOpenMenuStart,
    computeSuggestionOptions,
    matchCustomInput,
    type SelectMenuOption,
  } from '../selectMenuState';
  import SelectChips from './SelectChips.svelte';
  import SelectListbox from './SelectListbox.svelte';

  interface Props {
    id?: string;
    value?: string;
    options: SelectMenuOption[];
    placeholder?: string;
    ariaLabel?: string;
    disabled?: boolean;
    onchange: (value: string) => void;
    searchable?: boolean;
    allowCustom?: boolean;
    multiple?: boolean;
    values?: string[];
    onchangeValues?: (values: string[]) => void;
    addon?: Snippet;
  }

  let {
    id,
    value = '',
    options,
    placeholder,
    ariaLabel,
    disabled = false,
    onchange,
    searchable = false,
    allowCustom = false,
    multiple = false,
    values = [],
    onchangeValues,
    addon,
  }: Props = $props();

  const resolvedPlaceholder = $derived(
    placeholder ?? (allowCustom ? 'Select or type…' : 'Select…'),
  );

  const uid = `fc-select-${Math.random().toString(36).slice(2, 9)}`;
  const listboxId = `${uid}-listbox`;
  const optionId = (index: number) => `${listboxId}-opt-${index}`;

  let open = $state(false);
  let activeIndex = $state(-1);
  let filterText = $state('');
  let suppressOpenOnFocus = false;
  let containerEl = $state<HTMLDivElement | null>(null);
  let triggerEl = $state<HTMLButtonElement | HTMLInputElement | null>(null);
  let optionEls: (HTMLLIElement | null)[] = [];

  let selectedValues = $derived(multiple ? values : []);
  let suggestionOptions = $derived(
    computeSuggestionOptions({
      options,
      selectedValues,
      multiple,
      searchable,
      open,
      filterText,
    }),
  );

  let selectedIndex = $derived(options.findIndex((o) => o.value === value));
  let selectedOption = $derived(selectedIndex >= 0 ? options[selectedIndex] : undefined);
  let displayLabel = $derived(
    selectedOption?.display ?? (allowCustom && value ? value : ''),
  );
  let activeOptionId = $derived(
    open && activeIndex >= 0 && activeIndex < suggestionOptions.length
      ? optionId(activeIndex)
      : undefined,
  );

  let emptyListMessage = $derived(
    computeEmptyListMessage({
      filterText,
      allowCustom,
      multiple,
      optionsLength: options.length,
      suggestionLength: suggestionOptions.length,
    }),
  );

  function openMenu(): void {
    if (
      !canOpenMenu({
        disabled,
        open,
        suppressOpenOnFocus,
        allowCustom,
        optionsLength: options.length,
      })
    ) {
      return;
    }
    const start = computeOpenMenuStart({
      searchable,
      multiple,
      displayLabel,
      selectedIndex,
      options,
    });
    filterText = start.filterText;
    activeIndex = start.activeIndex;
    open = true;
  }

  function closeMenu(focusTrigger = false): void {
    open = false;
    filterText = '';
    if (!focusTrigger) return;
    suppressOpenOnFocus = true;
    triggerEl?.focus();
    queueMicrotask(() => {
      suppressOpenOnFocus = false;
    });
  }

  function toggleMenu(): void {
    if (open) closeMenu();
    else openMenu();
  }

  function emitSingle(next: string): void {
    if (next !== value) onchange(next);
  }

  function emitMultiple(next: string[]): void {
    onchangeValues?.(next);
  }

  function chooseFiltered(index: number): void {
    const opt = suggestionOptions[index];
    if (!opt) return;
    if (multiple) {
      emitMultiple([...selectedValues, opt.value]);
      filterText = '';
      activeIndex = 0;
      return;
    }
    emitSingle(opt.value);
    closeMenu(true);
  }

  function commitCustom(raw: string): void {
    const text = raw.trim();
    if (!text || !allowCustom) return;
    if (multiple) {
      if (!selectedValues.includes(text)) emitMultiple([...selectedValues, text]);
      filterText = '';
      return;
    }
    emitSingle(text);
    closeMenu(true);
  }

  function removeChip(v: string): void {
    emitMultiple(selectedValues.filter((x) => x !== v));
  }

  function moveActive(delta: number): void {
    if (suggestionOptions.length === 0) return;
    const len = suggestionOptions.length;
    activeIndex = (activeIndex + delta + len) % len;
    optionEls[activeIndex]?.scrollIntoView({ block: 'nearest' });
  }

  function onTriggerKeydown(e: KeyboardEvent): void {
    if (disabled) return;
    if (multiple && e.key === 'Backspace' && filterText === '' && selectedValues.length > 0) {
      e.preventDefault();
      removeChip(selectedValues[selectedValues.length - 1]!);
      return;
    }
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
        e.preventDefault();
        if (!open) {
          openMenu();
          break;
        }
        if (activeIndex >= 0 && suggestionOptions[activeIndex]) chooseFiltered(activeIndex);
        else if (allowCustom) commitCustom(searchable ? filterText : '');
        break;
      case ' ':
        if (searchable) break;
        e.preventDefault();
        if (!open) openMenu();
        else chooseFiltered(activeIndex);
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
        if (open && suggestionOptions.length) {
          e.preventDefault();
          activeIndex = suggestionOptions.length - 1;
        }
        break;
      case 'Tab':
        if (open) {
          if (allowCustom && searchable && filterText.trim()) commitCustom(filterText);
          closeMenu();
        }
        break;
    }
  }

  function onInputBlur(): void {
    if (!allowCustom || !searchable || multiple) return;
    const text = filterText.trim();
    if (!text) return;
    const match = matchCustomInput(options, text);
    if (match) emitSingle(match.value);
    else commitCustom(text);
  }

  function onWindowClick(e: MouseEvent): void {
    if (!open) return;
    if (containerEl && !containerEl.contains(e.target as Node)) closeMenu();
  }

  function inputShownValue(): string {
    if (!open) return multiple ? filterText : displayLabel;
    return filterText;
  }
</script>

<svelte:window onclick={onWindowClick} />

<div class="relative min-w-0 flex-1" bind:this={containerEl}>
  {#if multiple}
    <SelectChips values={selectedValues} labelFor={(v) => labelForValue(options, v)} onremove={removeChip} />
  {/if}

  <div class="flex items-center gap-2">
    {#if searchable}
      <div class="relative min-w-0 flex-1">
        <input
          {id}
          bind:this={triggerEl}
          type="text"
          role="combobox"
          aria-haspopup="listbox"
          aria-expanded={open}
          aria-controls={listboxId}
          aria-activedescendant={activeOptionId}
          aria-autocomplete="list"
          aria-label={ariaLabel}
          {disabled}
          placeholder={resolvedPlaceholder}
          class="fc-input w-full pr-8 {open ? 'border-fc-accent ring-2 ring-fc-ring/40' : ''}"
          value={inputShownValue()}
          onfocus={openMenu}
          onclick={() => {
            if (!open) openMenu();
          }}
          oninput={(e) => {
            filterText = (e.currentTarget as HTMLInputElement).value;
            if (!open) openMenu();
            activeIndex = suggestionOptions.length ? 0 : -1;
          }}
          onblur={onInputBlur}
          onkeydown={onTriggerKeydown}
        />
        <svg
          class="pointer-events-none absolute right-2 top-1/2 h-4 w-4 -translate-y-1/2 text-fc-text-subtle {open
            ? 'rotate-180 text-fc-accent'
            : ''}"
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
      </div>
    {:else}
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
        class="fc-input flex min-w-0 flex-1 items-center justify-between gap-2 text-left
          {open ? 'border-fc-accent ring-2 ring-fc-ring/40' : ''}"
        onclick={toggleMenu}
        onkeydown={onTriggerKeydown}
      >
        <span class="truncate {selectedOption || (allowCustom && value) ? 'text-fc-text' : 'text-fc-text-subtle'}">
          {displayLabel || resolvedPlaceholder}
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
    {/if}
    {@render addon?.()}
  </div>

  {#if open}
    <SelectListbox
      {listboxId}
      {suggestionOptions}
      {activeIndex}
      {value}
      {selectedValues}
      {emptyListMessage}
      {optionId}
      {optionEls}
      onactive={(index) => (activeIndex = index)}
      onchoose={chooseFiltered}
    />
  {/if}
</div>
