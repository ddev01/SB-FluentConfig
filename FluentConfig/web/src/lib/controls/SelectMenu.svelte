<script lang="ts">
  import { scaleIn } from '../motion';
  import { filterSelectOptions } from '../dropdownRefresh';
  import TrashIcon from '../icons/TrashIcon.svelte';

  export interface SelectMenuOption {
    value: string;
    display: string;
  }

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
  let containerEl = $state<HTMLDivElement | null>(null);
  let triggerEl = $state<HTMLButtonElement | HTMLInputElement | null>(null);
  let optionEls: (HTMLLIElement | null)[] = [];

  let selectedValues = $derived(multiple ? values : []);
  let suggestionOptions = $derived.by(() => {
    const base = multiple
      ? options.filter((o) => !selectedValues.includes(o.value))
      : options;
    return searchable ? filterSelectOptions(base, open ? filterText : '') : base;
  });

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

  let emptyListMessage = $derived.by(() => {
    const typed = filterText.trim();
    if (allowCustom && typed) return `Press Enter to add “${typed}”`;
    if (
      multiple &&
      !typed &&
      options.length > 0 &&
      suggestionOptions.length === 0
    ) {
      return 'All options selected';
    }
    if (allowCustom) return 'Type to add a custom value';
    return 'No options';
  });

  function openMenu(): void {
    if (disabled || open) return;
    if (!allowCustom && options.length === 0) return;
    filterText = searchable && !multiple ? displayLabel : '';
    const list = searchable ? filterSelectOptions(options, '') : options;
    activeIndex = !multiple && selectedIndex >= 0 ? selectedIndex : list.length ? 0 : -1;
    open = true;
  }

  function closeMenu(focusTrigger = false): void {
    open = false;
    filterText = '';
    if (focusTrigger) triggerEl?.focus();
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

  function chipLabel(v: string): string {
    return options.find((o) => o.value === v)?.display ?? v;
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
    const match = options.find(
      (o) => o.value === text || o.display.toLowerCase() === text.toLowerCase(),
    );
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

<div class="relative" bind:this={containerEl}>
  {#if multiple && selectedValues.length > 0}
    <div class="mb-1 flex flex-wrap gap-1">
      {#each selectedValues as v (v)}
        <span
          class="inline-flex items-center gap-1 rounded-fc bg-fc-accent/15 px-1.5 py-0.5 text-xs text-fc-accent"
        >
          {chipLabel(v)}
          <button
            type="button"
            class="flex h-4 w-4 shrink-0 items-center justify-center rounded text-fc-text-subtle transition-colors hover:text-fc-danger"
            aria-label="Remove {chipLabel(v)}"
            onclick={() => removeChip(v)}
          >
            <TrashIcon class="h-3 w-3" />
          </button>
        </span>
      {/each}
    </div>
  {/if}

  {#if searchable}
    <div class="relative">
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
      class="fc-input flex w-full items-center justify-between gap-2 text-left
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

  {#if open}
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
          onmouseenter={() => (activeIndex = index)}
          onclick={() => chooseFiltered(index)}
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
  {/if}
</div>
