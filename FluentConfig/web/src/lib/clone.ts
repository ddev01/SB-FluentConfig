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
