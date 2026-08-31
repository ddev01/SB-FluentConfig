<script lang="ts">
  import type { Snippet } from 'svelte';
  import TrashIcon from '../icons/TrashIcon.svelte';
  import { scaleIn } from '../motion';
  import {
    canOpenMenu,
    chipLabel as labelForValue,
    computeEmptyListMessage,
    computeListBoxStyle,
    computeOpenMenuStart,
    computeSuggestionOptions,
    isInsideSelectMenu,
    matchCustomInput,
    type SelectMenuOption,
  } from '../selectMenuState';

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
  let listEl = $state<HTMLUListElement | null>(null);
  let optionEls: (HTMLLIElement | null)[] = [];
  let listStyle = $state('');

  function positionList(): void {
    if (!triggerEl) return;
    const r = triggerEl.getBoundingClientRect();
    listStyle = computeListBoxStyle({
      left: r.left,
      top: r.top,
      bottom: r.bottom,
      width: r.width,
      viewportHeight: window.innerHeight,
    });
  }

  $effect(() => {
    if (!open) return;
    const el = listEl;
    if (el) document.body.appendChild(el);
    positionList();
    const update = () => positionList();
    window.addEventListener('scroll', update, true);
    window.addEventListener('resize', update);
    return () => {
      window.removeEventListener('scroll', update, true);
      window.removeEventListener('resize', update);
      if (el?.parentElement === document.body) el.remove();
    };
  });

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
    positionList();
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
    if (isInsideSelectMenu(e.target, containerEl, listEl)) return;
    closeMenu();
  }

  function inputShownValue(): string {
    if (!open) return multiple ? filterText : displayLabel;
    return filterText;
  }
</script>

<svelte:window onclick={onWindowClick} />

<div class="relative min-w-0 flex-1" bind:this={containerEl}>
  {#if multiple && selectedValues.length > 0}
    <div class="mb-1 flex flex-wrap gap-1">
      {#each selectedValues as v (v)}
        <span
          class="inline-flex items-center gap-1 rounded-fc bg-fc-accent/15 px-1.5 py-0.5 text-xs text-fc-accent"
        >
          {labelForValue(options, v)}
          <button
            type="button"
            class="flex h-4 w-4 shrink-0 items-center justify-center rounded text-fc-text-subtle transition-colors hover:text-fc-danger"
            aria-label="Remove {labelForValue(options, v)}"
            onclick={() => removeChip(v)}
          >
            <TrashIcon class="h-3 w-3" />
          </button>
        </span>
      {/each}
    </div>
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
        {@render chevron('pointer-events-none absolute right-2 top-1/2 -translate-y-1/2')}
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
        {@render chevron('shrink-0 transition-transform duration-150')}
      </button>
    {/if}
    {@render addon?.()}
  </div>

  {#if open}
    <ul
      bind:this={listEl}
      id={listboxId}
      role="listbox"
      tabindex="-1"
      style={listStyle || undefined}
      class="fc-card max-h-64 w-full origin-top overflow-auto p-1 shadow-[0_12px_32px_-16px_rgba(0,0,0,0.55)]"
      in:scaleIn={{ duration: 0.12 }}
    >
      {#each suggestionOptions as opt, index (opt.value)}
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <li
          id={optionId(index)}
          bind:this={optionEls[index]}
          role="option"
          aria-selected={opt.value === value || selectedValues.includes(opt.value)}
          class="rounded-fc px-2.5 py-1.5 text-sm transition-colors duration-100
            {index === activeIndex
            ? 'bg-fc-accent/15 text-fc-accent'
            : opt.value === value
              ? 'text-fc-accent'
              : 'text-fc-text'}"
          onmouseenter={() => (activeIndex = index)}
          onmousedown={(e) => e.preventDefault()}
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

{#snippet chevron(posClass: string)}
  <svg
    class="h-4 w-4 text-fc-text-subtle {posClass} {open ? 'rotate-180 text-fc-accent' : ''}"
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
{/snippet}
