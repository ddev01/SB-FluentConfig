import { describe, expect, it } from 'vitest';
import { schemaNodeEachKey } from './schemaNodeKey';

describe('schemaNodeEachKey', () => {
  it('keeps two id-less groups unique', () => {
    const a = { type: 'group' };
    const b = { type: 'group', id: undefined };
    const keys = [schemaNodeEachKey(a, 4, 'test'), schemaNodeEachKey(b, 5, 'test')];
    expect(new Set(keys).size).toBe(2);
  });
});
