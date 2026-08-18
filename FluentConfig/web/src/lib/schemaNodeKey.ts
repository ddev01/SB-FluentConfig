/** Stable unique {#each} key for schema children. Always includes index.
 * Do not use `'id' in node` — Svelte 5 proxies make that true with id undefined,
 * so two groups collide as `group-undefined`. */
export function schemaNodeEachKey(
  child: { type?: string; id?: string } | null | undefined,
  index: number,
  extra = '',
): string {
  const type = child?.type ?? 'node';
  const id = typeof child?.id === 'string' && child.id.length > 0 ? child.id : '';
  return extra ? `${index}:${type}:${id}:${extra}` : `${index}:${type}:${id}`;
}
