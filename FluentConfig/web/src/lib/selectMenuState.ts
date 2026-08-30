import { filterSelectOptions, type NamedOption } from './dropdownRefresh';

export type SelectMenuOption = NamedOption;

export function computeSuggestionOptions(args: {
  options: SelectMenuOption[];
  selectedValues: string[];
  multiple: boolean;
  searchable: boolean;
  open: boolean;
  filterText: string;
}): SelectMenuOption[] {
  const base = args.multiple
    ? args.options.filter((o) => !args.selectedValues.includes(o.value))
    : args.options;
  return args.searchable ? filterSelectOptions(base, args.open ? args.filterText : '') : base;
}

export function computeEmptyListMessage(args: {
  filterText: string;
  allowCustom: boolean;
  multiple: boolean;
  optionsLength: number;
  suggestionLength: number;
}): string {
  const typed = args.filterText.trim();
  if (args.allowCustom && typed) return `Press Enter to add “${typed}”`;
  if (
    args.multiple &&
    !typed &&
    args.optionsLength > 0 &&
    args.suggestionLength === 0
  ) {
    return 'All options selected';
  }
  if (args.allowCustom) return 'Type to add a custom value';
  return 'No options';
}

export function canOpenMenu(args: {
  disabled: boolean;
  open: boolean;
  suppressOpenOnFocus: boolean;
  allowCustom: boolean;
  optionsLength: number;
}): boolean {
  if (args.disabled || args.open || args.suppressOpenOnFocus) return false;
  if (!args.allowCustom && args.optionsLength === 0) return false;
  return true;
}

/** Matches SelectMenu.openMenu: filter uses empty query; activeIndex uses full list. */
export function computeOpenMenuStart(args: {
  searchable: boolean;
  multiple: boolean;
  displayLabel: string;
  selectedIndex: number;
  options: SelectMenuOption[];
}): { filterText: string; activeIndex: number } {
  const filterText = args.searchable && !args.multiple ? args.displayLabel : '';
  const list = args.searchable ? filterSelectOptions(args.options, '') : args.options;
  const activeIndex =
    !args.multiple && args.selectedIndex >= 0 ? args.selectedIndex : list.length ? 0 : -1;
  return { filterText, activeIndex };
}

export function chipLabel(options: SelectMenuOption[], value: string): string {
  return options.find((o) => o.value === value)?.display ?? value;
}

export function matchCustomInput(
  options: SelectMenuOption[],
  text: string,
): SelectMenuOption | undefined {
  const typed = text.trim();
  if (!typed) return undefined;
  return options.find(
    (o) => o.value === typed || o.display.toLowerCase() === typed.toLowerCase(),
  );
}

/** Fixed-position list box so `.fc-main-scroll` overflow cannot clip it. */
export function computeListBoxStyle(trigger: {
  left: number;
  top: number;
  bottom: number;
  width: number;
  viewportHeight: number;
}): string {
  const gap = 6;
  const spaceBelow = trigger.viewportHeight - trigger.bottom - gap;
  const spaceAbove = trigger.top - gap;
  const preferBelow = spaceBelow >= 120 || spaceBelow >= spaceAbove;
  const maxH = Math.min(256, Math.max(80, preferBelow ? spaceBelow : spaceAbove));
  const edge = preferBelow
    ? `top:${trigger.bottom + gap}px`
    : `bottom:${trigger.viewportHeight - trigger.top + gap}px`;
  return `position:fixed;z-index:50;left:${trigger.left}px;width:${trigger.width}px;${edge};max-height:${maxH}px`;
}
