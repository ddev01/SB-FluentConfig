import { describe, expect, it } from 'vitest';
import { reconcileRowIds } from './rowIdentity';

describe('reconcileRowIds', () => {
  it('returns the same array when length matches', () => {
    const ids = [1, 2];
    const result = reconcileRowIds(ids, 2, 3);
    expect(result.ids).toBe(ids);
    expect(result.nextId).toBe(3);
  });

  it('appends new ids when length grows', () => {
    expect(reconcileRowIds([10, 20], 4, 21)).toEqual({
      ids: [10, 20, 21, 22],
      nextId: 23,
    });
  });

  it('drops from the end when length shrinks', () => {
    expect(reconcileRowIds([10, 20, 30], 2, 31)).toEqual({
      ids: [10, 20],
      nextId: 31,
    });
  });

  it('seeds from an empty list', () => {
    expect(reconcileRowIds([], 3, 0)).toEqual({ ids: [0, 1, 2], nextId: 3 });
  });

  it('clears to empty without changing nextId', () => {
    expect(reconcileRowIds([4, 5], 0, 6)).toEqual({ ids: [], nextId: 6 });
  });
});
