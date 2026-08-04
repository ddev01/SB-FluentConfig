import type { SettingsValues, VisibilityCondition } from '../protocol';
import { getPath } from './paths';

/** Reactive show/hide: true when the node should be rendered. */
export function isVisible(
  condition: VisibilityCondition | undefined,
  values: SettingsValues,
): boolean {
  if (!condition) return true;

  const actual = getPath(values, condition.saveKey);
  const expected = condition.equals !== undefined ? condition.equals : true;
  const matches = Object.is(actual, expected);
  return condition.inverted ? !matches : matches;
}
