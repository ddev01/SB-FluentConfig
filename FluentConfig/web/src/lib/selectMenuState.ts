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
