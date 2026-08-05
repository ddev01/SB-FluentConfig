/**
 * Deep equality + leaf-path diff for dirty-state tracking.
 * Compares plain JSON values (no functions / undefined keys after cloneJson).
 */

export type DiffEntry = {
  path: string;
  saved: unknown;
  current: unknown;
};

export function deepEqual(a: unknown, b: unknown): boolean {
  if (Object.is(a, b)) return true;
  if (a === null || b === null || typeof a !== typeof b) return false;
  if (typeof a !== 'object') return false;

  if (Array.isArray(a) || Array.isArray(b)) {
    if (!Array.isArray(a) || !Array.isArray(b) || a.length !== b.length) return false;
    for (let i = 0; i < a.length; i++) {
      if (!deepEqual(a[i], b[i])) return false;
    }
    return true;
  }

  const aObj = a as Record<string, unknown>;
  const bObj = b as Record<string, unknown>;
  const aKeys = Object.keys(aObj);
  const bKeys = Object.keys(bObj);
  if (aKeys.length !== bKeys.length) return false;
  for (const key of aKeys) {
    if (!Object.prototype.hasOwnProperty.call(bObj, key)) return false;
    if (!deepEqual(aObj[key], bObj[key])) return false;
  }
  return true;
}

/** Flatten changed leaf paths between two JSON trees. */
export function diffEntries(
  saved: unknown,
  current: unknown,
  prefix = '',
): DiffEntry[] {
  if (deepEqual(saved, current)) return [];

  const bothObjects =
    saved !== null &&
    current !== null &&
    typeof saved === 'object' &&
    typeof current === 'object' &&
    !Array.isArray(saved) &&
    !Array.isArray(current);

  if (bothObjects) {
    const s = saved as Record<string, unknown>;
    const c = current as Record<string, unknown>;
    const keys = new Set([...Object.keys(s), ...Object.keys(c)]);
    const out: DiffEntry[] = [];
    for (const key of [...keys].sort()) {
      const path = prefix ? `${prefix}.${key}` : key;
      out.push(...diffEntries(s[key], c[key], path));
    }
    return out;
  }

  const bothArrays = Array.isArray(saved) && Array.isArray(current);
  if (bothArrays) {
    const len = Math.max(saved.length, current.length);
    const out: DiffEntry[] = [];
    for (let i = 0; i < len; i++) {
      const path = prefix ? `${prefix}[${i}]` : `[${i}]`;
      out.push(...diffEntries(saved[i], current[i], path));
    }
    return out;
  }

  return [{ path: prefix || '(root)', saved, current }];
}

export function formatDiffValue(value: unknown): string {
  if (value === undefined) return '—';
  if (value === null) return 'null';
  if (typeof value === 'string') return value;
  if (typeof value === 'boolean' || typeof value === 'number') return String(value);
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}
