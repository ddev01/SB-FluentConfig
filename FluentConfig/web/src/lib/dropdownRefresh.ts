export type NamedOption = { value: string; display: string };

export function filterSelectOptions(options: NamedOption[], query: string): NamedOption[] {
  const q = query.trim().toLowerCase();
  if (!q) return options;
  return options.filter(
    (o) => o.display.toLowerCase().includes(q) || o.value.toLowerCase().includes(q),
  );
}

export type RefreshDecision =
  | { action: 'keep' }
  | { action: 'keepValues'; values: string[] }
  | { action: 'snap'; option: NamedOption }
  | { action: 'clear' };

export function resolveDropdownRefresh(args: {
  allowCustom: boolean;
  multiple: boolean;
  current: string | string[];
  options: NamedOption[];
}): RefreshDecision {
  const options = args.options ?? [];
  const inList = (v: string) => options.some((o) => o.value === v || o.display === v);

  if (args.multiple) {
    const cur = Array.isArray(args.current) ? args.current : [];
    if (args.allowCustom) return { action: 'keep' };
    const kept = cur.filter(inList);
    if (kept.length === cur.length) return { action: 'keep' };
    if (kept.length > 0) return { action: 'keepValues', values: kept };
    return { action: 'clear' };
  }

  const v = typeof args.current === 'string' ? args.current : '';
  if (v !== '' && inList(v)) return { action: 'keep' };
  if (args.allowCustom && v !== '') return { action: 'keep' };
  if (options.length > 0) return { action: 'snap', option: options[0]! };
  return { action: 'clear' };
}
