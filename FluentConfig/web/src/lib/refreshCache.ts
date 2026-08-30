/**
 * Session-scoped "run once" set for mount-time dropdown.refresh / filepath.validate.
 * `{#key activeSection.id}` remounts the tab; the first call per saveKey must still run (I2).
 */
const seen = new Set<string>();

/** True on the first call for `saveKey` this session; false afterwards. */
export function consumeOnce(saveKey: string): boolean {
  if (!saveKey) return true;
  if (seen.has(saveKey)) return false;
  seen.add(saveKey);
  return true;
}

/** Test helper — production never resets mid-session. */
export function resetRefreshCache(): void {
  seen.clear();
}
