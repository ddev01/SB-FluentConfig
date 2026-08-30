import { describe, expect, it } from 'vitest';
import { isDirtySet, rebuildDirtyKeys, syncDirtyKey } from './dirtyKeys';

describe('syncDirtyKey', () => {
  it('adds a path when the live value differs from saved', () => {
    const dirty = new Set<string>();
    syncDirtyKey(dirty, 'timeout', { timeout: 5 }, 9);
    expect([...dirty]).toEqual(['timeout']);
  });

  it('drops a path when the live value matches saved', () => {
    const dirty = new Set(['timeout']);
    syncDirtyKey(dirty, 'timeout', { timeout: 5 }, 5);
    expect(dirty.size).toBe(0);
  });

  it('treats empty string as live, not equal to a missing key', () => {
    const dirty = new Set<string>();
    syncDirtyKey(dirty, 'reward', {}, '');
    expect(dirty.has('reward')).toBe(true);
    syncDirtyKey(dirty, 'reward', { reward: '' }, '');
    expect(dirty.has('reward')).toBe(false);
  });

  it('diffs nested and indexed paths', () => {
    const dirty = new Set<string>();
    const saved = { rows: [{ name: 'A' }] };
    syncDirtyKey(dirty, 'rows[0].name', saved, 'B');
    expect(dirty.has('rows[0].name')).toBe(true);
    syncDirtyKey(dirty, 'rows[0].name', saved, 'A');
    expect(dirty.has('rows[0].name')).toBe(false);
  });
});

describe('rebuildDirtyKeys', () => {
  it('is empty when blobs match', () => {
    expect(isDirtySet(rebuildDirtyKeys({ a: 1 }, { a: 1 }))).toBe(false);
  });

  it('collects leaf paths after a host-style dual-write leaves user edits', () => {
    const saved = { a: 1, b: 2 };
    const current = { a: 9, b: 2 };
    expect([...rebuildDirtyKeys(saved, current)].sort()).toEqual(['a']);
  });
});

describe('isDirtySet', () => {
  it('is size > 0', () => {
    expect(isDirtySet(new Set())).toBe(false);
    expect(isDirtySet(new Set(['k']))).toBe(true);
  });
});
