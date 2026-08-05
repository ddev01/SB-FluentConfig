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

/** Reactive show/hide: true when the node should be rendered. */
export function isVisible(
  condition: VisibilityCondition | undefined,
  values: SettingsValues,
  schemaDefault?: unknown,
): boolean {
  if (!condition) return true;

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
