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
});
