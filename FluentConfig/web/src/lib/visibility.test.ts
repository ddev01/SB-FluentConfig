import { describe, expect, it } from 'vitest';
import type { SectionSchema } from '../protocol';
import { equalsMatches, isVisible } from './visibility';

describe('equalsMatches', () => {
  it('matches booleans without coercing strings', () => {
    expect(equalsMatches(true, true)).toBe(true);
    expect(equalsMatches(false, true)).toBe(false);
    expect(equalsMatches('true', true)).toBe(false);
  });

  it('matches numbers including dropdown string "1"', () => {
    expect(equalsMatches('1', 1)).toBe(true);
    expect(equalsMatches(1, 1)).toBe(true);
    expect(equalsMatches('2', 1)).toBe(false);
  });

  it('matches strings exactly', () => {
    expect(equalsMatches('random', 'random')).toBe(true);
    expect(equalsMatches('free', 'random')).toBe(false);
    expect(equalsMatches(1, '1')).toBe(true);
  });

  it('matches null as missing', () => {
    expect(equalsMatches(null, null)).toBe(true);
    expect(equalsMatches(undefined, null)).toBe(true);
    expect(equalsMatches('x', null)).toBe(false);
  });
});

describe('isVisible equals', () => {
  it('toggle true/false', () => {
    expect(isVisible({ saveKey: 'on', equals: true }, { on: true })).toBe(true);
    expect(isVisible({ saveKey: 'on', equals: true }, { on: false })).toBe(false);
  });

  it('string equals and inverted', () => {
    expect(isVisible({ saveKey: 'mode', equals: 'random' }, { mode: 'random' })).toBe(true);
    expect(isVisible({ saveKey: 'mode', equals: 'random' }, { mode: 'free' })).toBe(false);
    expect(
      isVisible({ saveKey: 'mode', equals: 'free', inverted: true }, { mode: 'random' }),
    ).toBe(true);
    expect(
      isVisible({ saveKey: 'mode', equals: 'free', inverted: true }, { mode: 'free' }),
    ).toBe(false);
  });

  it('uses dropdown defaultByValue when live value is missing', () => {
    const sections: SectionSchema[] = [
      {
        id: 'g',
        title: 'G',
        children: [
          {
            type: 'dropdown',
            label: 'Mode',
            saveKey: 'default_mode',
            options: [
              { value: 'discount', display: 'discount' },
              { value: 'random', display: 'random' },
            ],
            defaultByValue: 'random',
          },
        ],
      },
    ];
    expect(isVisible({ saveKey: 'default_mode', equals: 'random' }, {}, undefined, sections)).toBe(
      true,
    );
    expect(isVisible({ saveKey: 'default_mode', equals: 'discount' }, {}, undefined, sections)).toBe(
      false,
    );
  });

  it('operator path still works', () => {
    expect(isVisible({ saveKey: 'n', operator: 'gte', value: 4 }, { n: 4 })).toBe(true);
  });
});
