import type { SchemaNode, SettingsValues, VisibilityCondition } from '../protocol';
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

/** Reactive show/hide: true when the node should be rendered. */
export function isVisible(
  condition: VisibilityCondition | undefined,
  values: SettingsValues,
  schemaDefault?: unknown,
): boolean {
  if (!condition) return true;

  if (condition.operator) {
    const left = toNumber(effectiveValue(values, condition.saveKey, schemaDefault));
    const rightRaw =
      condition.compareKey !== undefined && condition.compareKey !== ''
        ? effectiveValue(values, condition.compareKey)
        : condition.value;
    const right = toNumber(rightRaw);
    // Missing/NaN → hide (conservative for RepeatFor indices above min).
    const matches = left !== null && right !== null && compare(condition.operator, left, right);
    return condition.inverted ? !matches : matches;
  }

  const actual = effectiveValue(values, condition.saveKey, schemaDefault);
  const expected = condition.equals !== undefined ? condition.equals : true;
  const matches = Object.is(actual, expected);
  return condition.inverted ? !matches : matches;
}

/** Resolve a node's own default for use when its saveKey is absent from values. */
export function nodeDefaultValue(node: SchemaNode): unknown {
  if (!('defaultValue' in node)) return undefined;
  return (node as { defaultValue?: unknown }).defaultValue;
}
