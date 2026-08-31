import { describe, expect, it } from 'vitest';
import { cloneJson, deletePathImmutable, setPathImmutable } from './clone';

describe('cloneJson', () => {
  it('deep-copies nested objects', () => {
    const prev = { a: { x: 1 } };
    const next = cloneJson(prev);
    expect(next).toEqual(prev);
    expect(next).not.toBe(prev);
    expect(next.a).not.toBe(prev.a);
  });
});

describe('setPathImmutable', () => {
  it('shares sibling objects when setting a nested key', () => {
    const prev = { a: { x: 1, y: 2 }, b: { z: 3 } };
    const next = setPathImmutable(prev, 'a.x', 9);
    expect(next.a).not.toBe(prev.a);
    expect(next.b).toBe(prev.b);
    expect(next.a).toEqual({ x: 9, y: 2 });
    expect(prev.a.x).toBe(1);
  });

  it('shares untouched row objects on an indexed path', () => {
    const row0 = { name: 'a' };
    const row1 = { name: 'b' };
    const prev = { rows: [row0, row1], other: { k: 1 } };
    const next = setPathImmutable(prev, 'rows[0].name', 'A');
    expect(next).not.toBe(prev);
    expect(next.other).toBe(prev.other);
    expect(next.rows).not.toBe(prev.rows);
    expect(next.rows[0]).not.toBe(row0);
    expect(next.rows[1]).toBe(row1);
    expect(next.rows[0]).toEqual({ name: 'A' });
    expect(row0.name).toBe('a');
  });
});

describe('deletePathImmutable', () => {
  it('shares sibling objects when deleting a nested key', () => {
    const prev = { a: { x: 1, y: 2 }, b: { z: 3 } };
    const next = deletePathImmutable(prev, 'a.x');
    expect(next.a).not.toBe(prev.a);
    expect(next.b).toBe(prev.b);
    expect(next.a).toEqual({ y: 2 });
    expect(prev.a).toEqual({ x: 1, y: 2 });
  });

  it('splices arrays without mutating the source', () => {
    const prev = { items: ['a', 'b', 'c'], other: { k: 1 } };
    const next = deletePathImmutable(prev, 'items[1]');
    expect(next.items).toEqual(['a', 'c']);
    expect(next.other).toBe(prev.other);
    expect(prev.items).toEqual(['a', 'b', 'c']);
  });
});
