import { describe, expect, it } from 'vitest';
import { filterSelectOptions, resolveDropdownRefresh } from './dropdownRefresh';

describe('filterSelectOptions', () => {
  const opts = [
    { value: 'timer-a', display: 'Timer A' },
    { value: 'timer-b', display: 'Nightly' },
  ];

  it('filters case-insensitive on display and value', () => {
    expect(filterSelectOptions(opts, 'timer-a')).toEqual([opts[0]]);
    expect(filterSelectOptions(opts, 'NIGHT')).toEqual([opts[1]]);
  });

  it('empty query returns all', () => {
    expect(filterSelectOptions(opts, '')).toEqual(opts);
    expect(filterSelectOptions(opts, '  ')).toEqual(opts);
  });
});

describe('resolveDropdownRefresh', () => {
  const opts = [
    { value: 'a', display: 'A' },
    { value: 'b', display: 'B' },
  ];

  it('allowCustom keeps a value missing from new options, including empty list', () => {
    expect(
      resolveDropdownRefresh({
        allowCustom: true,
        multiple: false,
        current: 'custom-guid',
        options: opts,
      }),
    ).toEqual({ action: 'keep' });
    expect(
      resolveDropdownRefresh({
        allowCustom: true,
        multiple: false,
        current: 'custom-guid',
        options: [],
      }),
    ).toEqual({ action: 'keep' });
  });

  it('empty selection stays empty and does not snap to first', () => {
    expect(
      resolveDropdownRefresh({
        allowCustom: false,
        multiple: false,
        current: '',
        options: opts,
      }),
    ).toEqual({ action: 'keep' });
    expect(
      resolveDropdownRefresh({
        allowCustom: true,
        multiple: false,
        current: '',
        options: opts,
      }),
    ).toEqual({ action: 'keep' });
  });

  it('non-custom clears when missing from a non-empty list', () => {
    expect(
      resolveDropdownRefresh({
        allowCustom: false,
        multiple: false,
        current: 'gone',
        options: opts,
      }),
    ).toEqual({ action: 'clear' });
  });

  it('empty options keep the current seed (single and multiple)', () => {
    expect(
      resolveDropdownRefresh({
        allowCustom: false,
        multiple: false,
        current: 'rew_2',
        options: [],
      }),
    ).toEqual({ action: 'keep' });
    expect(
      resolveDropdownRefresh({
        allowCustom: false,
        multiple: true,
        current: ['a'],
        options: [],
      }),
    ).toEqual({ action: 'keep' });
  });

  it('multiple allowCustom keeps chips', () => {
    expect(
      resolveDropdownRefresh({
        allowCustom: true,
        multiple: true,
        current: ['gone', 'a'],
        options: opts,
      }),
    ).toEqual({ action: 'keep' });
  });

  it('prunes invalid multi chips', () => {
    expect(
      resolveDropdownRefresh({
        allowCustom: false,
        multiple: true,
        current: ['a', 'gone', 'b'],
        options: opts,
      }),
    ).toEqual({ action: 'keepValues', values: ['a', 'b'] });
  });

  it('never returns snap', () => {
    const cases = [
      { allowCustom: false, multiple: false, current: '', options: opts },
      { allowCustom: false, multiple: false, current: 'gone', options: opts },
      { allowCustom: false, multiple: false, current: 'a', options: opts },
      { allowCustom: true, multiple: false, current: 'custom', options: [] },
      { allowCustom: false, multiple: true, current: ['gone'], options: opts },
      { allowCustom: false, multiple: true, current: ['a', 'gone'], options: opts },
      { allowCustom: true, multiple: true, current: ['gone'], options: opts },
    ];
    for (const c of cases) {
      expect(['keep', 'keepValues', 'clear']).toContain(resolveDropdownRefresh(c).action);
    }
  });
});
