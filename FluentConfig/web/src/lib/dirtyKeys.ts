/**
 * Per-path dirty tracking for the settings blob.
 * Vitest cannot import runes modules, so the store delegates here.
 */
import { deepEqual, diffEntries } from './diff';
import { getPath } from './paths';

/** Mark `saveKey` dirty iff `nextValue` differs from the saved blob at that path. */
export function syncDirtyKey(
  dirty: Set<string>,
  saveKey: string,
  savedRoot: unknown,
  nextValue: unknown,
): void {
  if (deepEqual(getPath(savedRoot, saveKey), nextValue)) dirty.delete(saveKey);
  else dirty.add(saveKey);
}

/** Rebuild the dirty set from a full saved-vs-current diff (bootstrap / save / host patch). */
export function rebuildDirtyKeys(saved: unknown, current: unknown): Set<string> {
  const dirty = new Set<string>();
  for (const entry of diffEntries(saved, current)) {
    dirty.add(entry.path);
  }
  return dirty;
}

export function isDirtySet(dirty: Set<string>): boolean {
  return dirty.size > 0;
}
