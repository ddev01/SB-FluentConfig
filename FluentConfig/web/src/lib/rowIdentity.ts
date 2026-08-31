/**
 * Component-local RepeatableRows identity. Never persisted / setValue'd.
 * Host patches that change length append new ids or drop from the end only;
 * user deletes splice in the same function as setRows.
 */
export function reconcileRowIds(
  ids: readonly number[],
  length: number,
  nextId: number,
): { ids: number[]; nextId: number } {
  const safeLen = Math.max(0, length);
  if (ids.length === safeLen) return { ids: ids as number[], nextId };

  if (safeLen > ids.length) {
    const next = ids.slice();
    let n = nextId;
    while (next.length < safeLen) next.push(n++);
    return { ids: next, nextId: n };
  }

  return { ids: ids.slice(0, safeLen), nextId };
}
