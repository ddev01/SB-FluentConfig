import { describe, expect, it } from 'vitest';
import { formatDuration, parseDuration } from './duration';

describe('parseDuration', () => {
  it('parses permanent', () => {
    expect(parseDuration('permanent')).toEqual({
      amount: 0,
      unit: 'seconds',
      permanent: true,
    });
  });

  it('parses amount+unit', () => {
    expect(parseDuration('30seconds')).toEqual({
      amount: 30,
      unit: 'seconds',
      permanent: false,
    });
    expect(parseDuration('5minutes')).toEqual({
      amount: 5,
      unit: 'minutes',
      permanent: false,
    });
  });

  it('falls back for invalid input', () => {
    expect(parseDuration('nope')).toEqual({
      amount: 30,
      unit: 'seconds',
      permanent: false,
    });
  });
});

describe('formatDuration', () => {
  it('writes permanent', () => {
    expect(formatDuration(30, 'minutes', true)).toBe('permanent');
  });

  it('writes amount+unit and floors', () => {
    expect(formatDuration(5.9, 'hours', false)).toBe('5hours');
  });

  it('round-trips uncheck restore inputs', () => {
    const stored = formatDuration(0 || 30, 'seconds', false);
    expect(stored).toBe('30seconds');
    expect(parseDuration(stored).permanent).toBe(false);
  });
});
