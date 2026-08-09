import { describe, expect, it } from 'vitest';
import { deletePath, getPath, setPath } from './paths';
import { effectiveValue, isVisible } from './visibility';

describe('paths', () => {
  it('set/get nested and indexed paths', () => {
    const root: Record<string, unknown> = {};
    setPath(root, 'rows[0].name', 'A');
    expect(getPath(root, 'rows[0].name')).toBe('A');
  });

  it('deletePath splices arrays', () => {
    const root: Record<string, unknown> = { items: ['a', 'b', 'c'] };
    deletePath(root, 'items[1]');
    expect(root.items).toEqual(['a', 'c']);
  });
});

describe('visibility', () => {
  it('falls back to schema default when key missing', () => {
    expect(isVisible({ saveKey: 'on', equals: true }, {}, true)).toBe(true);
    expect(isVisible({ saveKey: 'on', equals: true }, {}, false)).toBe(false);
    expect(effectiveValue({}, 'on', true)).toBe(true);
  });

  it('comparator gte against literal value', () => {
    expect(isVisible({ saveKey: 'n', operator: 'gte', value: 4 }, { n: 4 })).toBe(true);
    expect(isVisible({ saveKey: 'n', operator: 'gte', value: 4 }, { n: 3 })).toBe(false);
    expect(isVisible({ saveKey: 'n', operator: 'gt', value: 4 }, { n: 4 })).toBe(false);
    expect(isVisible({ saveKey: 'n', operator: 'lte', value: 2 }, { n: 2 })).toBe(true);
    expect(isVisible({ saveKey: 'n', operator: 'lt', value: 2 }, { n: 2 })).toBe(false);
  });

  it('comparator against compareKey', () => {
    expect(
      isVisible({ saveKey: 'a', operator: 'gte', compareKey: 'b' }, { a: 5, b: 3 }),
    ).toBe(true);
    expect(
      isVisible({ saveKey: 'a', operator: 'gte', compareKey: 'b' }, { a: 2, b: 3 }),
    ).toBe(false);
  });

  it('comparator hides when driver value missing or NaN', () => {
    expect(isVisible({ saveKey: 'n', operator: 'gte', value: 2 }, {})).toBe(false);
    expect(isVisible({ saveKey: 'n', operator: 'gte', value: 2 }, { n: 'nope' })).toBe(false);
  });

  it('comparator inverted negates result', () => {
    expect(
      isVisible({ saveKey: 'n', operator: 'gte', value: 4, inverted: true }, { n: 4 }),
    ).toBe(false);
    expect(
      isVisible({ saveKey: 'n', operator: 'gte', value: 4, inverted: true }, { n: 3 }),
    ).toBe(true);
  });
});
