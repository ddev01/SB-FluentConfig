import { describe, expect, it } from 'vitest';
import { roundToStep, stepDecimals } from './number';

describe('roundToStep', () => {
  it('roundToStep(0.1+0.2, 0.1) === 0.3', () => {
    expect(roundToStep(0.1 + 0.2, 0.1)).toBe(0.3);
  });

  it('leaves non-finite values alone', () => {
    expect(roundToStep(Number.NaN, 0.1)).toBeNaN();
    expect(roundToStep(1, 0)).toBe(1);
  });
});

describe('stepDecimals', () => {
  it('counts fraction digits including scientific notation', () => {
    expect(stepDecimals(1)).toBe(0);
    expect(stepDecimals(0.1)).toBe(1);
    expect(stepDecimals(1e-3)).toBe(3);
  });
});
