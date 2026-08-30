import { describe, expect, it, beforeEach } from 'vitest';
import { consumeOnce, resetRefreshCache } from './refreshCache';

describe('refreshCache', () => {
  beforeEach(() => {
    resetRefreshCache();
  });

  it('allows the first refresh per saveKey and suppresses repeats', () => {
    expect(consumeOnce('reward')).toBe(true);
    expect(consumeOnce('reward')).toBe(false);
    expect(consumeOnce('other')).toBe(true);
  });

  it('treats an empty key as always-allow (no cache entry)', () => {
    expect(consumeOnce('')).toBe(true);
    expect(consumeOnce('')).toBe(true);
  });
});
