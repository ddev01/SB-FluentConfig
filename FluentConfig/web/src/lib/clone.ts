import { deletePath, setPath, splitPath } from './paths';

/**
 * Deep-clone plain JSON data.
 * Prefer this over `structuredClone` for app state: Svelte 5 `$state` values are
 * Proxies, and `structuredClone` / WebView2 object `postMessage` reject them with
 * "could not be cloned".
 */
export function cloneJson<T>(value: T): T {
  if (value === undefined) return value;
  return JSON.parse(JSON.stringify(value)) as T;
}

function shallowCloneContainer(
  value: unknown,
  nextKey: string | number,
): Record<string | number, unknown> {
  if (Array.isArray(value)) return value.slice() as unknown as Record<string | number, unknown>;
  if (value !== null && typeof value === 'object') {
    return { ...(value as Record<string, unknown>) };
  }
  return (typeof nextKey === 'number' ? [] : {}) as Record<string | number, unknown>;
}

/**
 * Shallow-clone the root and every ancestor on `path`, leaving sibling objects
 * shared. Always returns a new root so `$state` invalidates. Do not use
 * `structuredClone` here — `$state` values are Proxies.
 */
function clonePathAncestors(
  root: Record<string, unknown>,
  path: string,
): Record<string, unknown> {
  const parts = splitPath(path);
  const newRoot: Record<string, unknown> = { ...root };
  if (parts.length <= 1) return newRoot;

  let src: unknown = root;
  let dst: Record<string | number, unknown> = newRoot;
  for (let i = 0; i < parts.length - 1; i++) {
    const key = parts[i]!;
    const nextKey = parts[i + 1]!;
    const srcChild =
      src !== null && typeof src === 'object'
        ? (src as Record<string | number, unknown>)[key as string]
        : undefined;
    const cloned = shallowCloneContainer(srcChild, nextKey);
    dst[key as string] = cloned;
    src = srcChild;
    dst = cloned;
  }
  return newRoot;
}

/** Set `path` on a new tree that shares objects not on the path. */
export function setPathImmutable(
  root: Record<string, unknown>,
  path: string,
  value: unknown,
): Record<string, unknown> {
  const next = clonePathAncestors(root, path);
  setPath(next, path, value);
  return next;
}

/** Delete `path` on a new tree that shares objects not on the path. */
export function deletePathImmutable(
  root: Record<string, unknown>,
  path: string,
): Record<string, unknown> {
  const next = clonePathAncestors(root, path);
  deletePath(next, path);
  return next;
}
