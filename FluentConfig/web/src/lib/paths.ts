/**
 * Nested settings path get/set matching host conventions:
 * `"settings.timeout"`, `"rows[0].name"`.
 */

const SEGMENT = /([^[.\]]+)|\[(\d+)\]/g;

function splitPath(path: string): Array<string | number> {
  const parts: Array<string | number> = [];
  for (const match of path.matchAll(SEGMENT)) {
    if (match[1] !== undefined) parts.push(match[1]);
    else if (match[2] !== undefined) parts.push(Number(match[2]));
  }
  return parts;
}

export function getPath(root: unknown, path: string): unknown {
  if (!path) return root;
  let cur: unknown = root;
  for (const key of splitPath(path)) {
    if (cur == null || typeof cur !== 'object') return undefined;
    cur = (cur as Record<string | number, unknown>)[key as string];
  }
  return cur;
}

export function setPath(root: Record<string, unknown>, path: string, value: unknown): void {
  const parts = splitPath(path);
  if (parts.length === 0) return;

  let cur: Record<string | number, unknown> = root;
  for (let i = 0; i < parts.length - 1; i++) {
    const key = parts[i]!;
    const nextKey = parts[i + 1]!;
    let next = cur[key as string];
    if (next == null || typeof next !== 'object') {
      next = typeof nextKey === 'number' ? [] : {};
      cur[key as string] = next;
    }
    cur = next as Record<string | number, unknown>;
  }

  const last = parts[parts.length - 1]!;
  cur[last as string] = value;
}

/** Delete a value at path (splices array indices; removes object keys). */
export function deletePath(root: Record<string, unknown>, path: string): void {
  const parts = splitPath(path);
  if (parts.length === 0) return;

  if (parts.length === 1) {
    const key = parts[0]!;
    if (typeof key === 'number' && Array.isArray(root)) {
      if (key >= 0 && key < root.length) root.splice(key, 1);
    } else {
      delete root[key as string];
    }
    return;
  }

  let cur: unknown = root;
  for (let i = 0; i < parts.length - 1; i++) {
    if (cur == null || typeof cur !== 'object') return;
    cur = (cur as Record<string | number, unknown>)[parts[i]! as string];
  }
  if (cur == null || typeof cur !== 'object') return;

  const last = parts[parts.length - 1]!;
  if (typeof last === 'number' && Array.isArray(cur)) {
    if (last >= 0 && last < cur.length) cur.splice(last, 1);
  } else {
    delete (cur as Record<string, unknown>)[last as string];
  }
}

/** Shallow path map into a deep object (values.patch.paths). */
export function applyPathMap(
  root: Record<string, unknown>,
  paths: Record<string, unknown>,
): void {
  for (const [path, value] of Object.entries(paths)) {
    setPath(root, path, value);
  }
}

/** Deep-merge `patch` into `target` (objects only; arrays replaced). */
export function deepMerge(
  target: Record<string, unknown>,
  patch: Record<string, unknown>,
): void {
  for (const [key, value] of Object.entries(patch)) {
    if (
      value !== null &&
      typeof value === 'object' &&
      !Array.isArray(value) &&
      target[key] !== null &&
      typeof target[key] === 'object' &&
      !Array.isArray(target[key])
    ) {
      deepMerge(target[key] as Record<string, unknown>, value as Record<string, unknown>);
    } else {
      target[key] = value;
    }
  }
}
