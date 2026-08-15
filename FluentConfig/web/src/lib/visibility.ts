import type { DropdownNode, SchemaNode, SectionSchema, SettingsValues, VisibilityCondition } from '../protocol';
import { getPath } from './paths';

/** Effective wire value for visibility: live value, else schema default when known. */
export function effectiveValue(
  values: SettingsValues,
  saveKey: string,
  schemaDefault?: unknown,
): unknown {
  const live = getPath(values, saveKey);
  if (live !== undefined && live !== null) return live;
  return schemaDefault;
}

function toNumber(value: unknown): number | null {
  if (typeof value === 'number' && Number.isFinite(value)) return value;
  if (typeof value === 'string' && value.trim() !== '') {
    const n = Number(value);
    if (Number.isFinite(n)) return n;
  }
  if (typeof value === 'boolean') return value ? 1 : 0;
  return null;
}

function compare(op: string, left: number, right: number): boolean {
  switch (op) {
    case 'gte':
      return left >= right;
    case 'lte':
      return left <= right;
    case 'gt':
      return left > right;
    case 'lt':
      return left < right;
    default:
      return false;
  }
}

/** Equals-path matching (no operator). */
export function equalsMatches(actual: unknown, expected: unknown): boolean {
  if (typeof expected === 'boolean') return actual === expected;
  if (typeof expected === 'number') return toNumber(actual) === expected;
  if (typeof expected === 'string') return String(actual) === expected;
  if (expected === null) return actual === undefined || actual === null;
  return Object.is(actual, expected);
}

function dropdownDriverDefault(node: DropdownNode, saveKey: string): unknown {
  const options = node.options ?? [];
  const pairDisplay = !!node.valueSaveKey && node.saveKey === saveKey;
  if (node.defaultByValue) {
    const match = options.find((o) => o.value === node.defaultByValue);
    if (match) return pairDisplay ? (match.display ?? match.value) : (match.value ?? match.display);
    if (!pairDisplay) return node.defaultByValue;
  }
  if (node.defaultIndex !== undefined && options[node.defaultIndex]) {
    const opt = options[node.defaultIndex]!;
    return pairDisplay ? (opt.display ?? opt.value) : (opt.value ?? opt.display);
  }
  if (node.defaultValue != null && node.defaultValue !== '' && !pairDisplay) {
    return node.defaultValue;
  }
  const first = options[0];
  if (!first) return undefined;
  return pairDisplay ? (first.display ?? first.value) : (first.value ?? first.display);
}

function walkDriverDefault(nodes: SchemaNode[] | undefined, saveKey: string): unknown {
  if (!nodes) return undefined;
  for (const node of nodes) {
    if (node.type === 'toggle' && node.saveKey === saveKey) {
      return node.defaultValue;
    }
    if (node.type === 'dropdown' && (node.saveKey === saveKey || node.valueSaveKey === saveKey)) {
      return dropdownDriverDefault(node, saveKey);
    }
    if (
      node.type !== 'group' &&
      node.type !== 'pill-input' &&
      node.type !== 'repeatable-rows' &&
      'saveKey' in node &&
      (node as { saveKey?: string }).saveKey === saveKey &&
      'defaultValue' in node
    ) {
      return (node as { defaultValue?: unknown }).defaultValue;
    }
    if (node.type === 'group') {
      const found = walkDriverDefault(node.children, saveKey);
      if (found !== undefined) return found;
    }
    if (node.type === 'pill-input') {
      const found = walkDriverDefault(node.itemTemplate, saveKey);
      if (found !== undefined) return found;
      for (const item of node.items ?? []) {
        const nested = walkDriverDefault(item.children, saveKey);
        if (nested !== undefined) return nested;
      }
    }
    if (node.type === 'repeatable-rows') {
      const found = walkDriverDefault(node.rowSchema, saveKey);
      if (found !== undefined) return found;
    }
  }
  return undefined;
}

/** Resolve a driver control's default from the live schema (first-paint). */
export function driverDefaultFromSchema(
  sections: SectionSchema[] | undefined,
  saveKey: string,
): unknown {
  if (!sections || !saveKey) return undefined;
  for (const section of sections) {
    const found = walkDriverDefault(section.children, saveKey);
    if (found !== undefined) return found;
  }
  return undefined;
}

/** Reactive show/hide: true when the node should be rendered. */
export function isVisible(
  condition: VisibilityCondition | undefined,
  values: SettingsValues,
  schemaDefault?: unknown,
  sections?: SectionSchema[],
): boolean {
  if (!condition) return true;

  const fallback =
    schemaDefault !== undefined ? schemaDefault : driverDefaultFromSchema(sections, condition.saveKey);

  if (condition.operator) {
    const left = toNumber(effectiveValue(values, condition.saveKey, fallback));
    const rightRaw =
      condition.compareKey !== undefined && condition.compareKey !== ''
        ? effectiveValue(values, condition.compareKey)
        : condition.value;
    const right = toNumber(rightRaw);
    // Missing/NaN → hide (conservative for RepeatFor indices above min).
    const matches = left !== null && right !== null && compare(condition.operator, left, right);
    return condition.inverted ? !matches : matches;
  }

  const actual = effectiveValue(values, condition.saveKey, fallback);
  const expected = condition.equals !== undefined ? condition.equals : true;
  const matches = equalsMatches(actual, expected);
  return condition.inverted ? !matches : matches;
}

/** Resolve a node's own default for use when its saveKey is absent from values. */
export function nodeDefaultValue(node: SchemaNode): unknown {
  if (!('defaultValue' in node)) return undefined;
  return (node as { defaultValue?: unknown }).defaultValue;
}
