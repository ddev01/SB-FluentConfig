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

  it('non-custom clears when missing from new options', () => {
    expect(
      resolveDropdownRefresh({
        allowCustom: false,
        multiple: false,
        current: 'gone',
        options: opts,
      }),
    ).toEqual({ action: 'clear' });
  });

  it('non-custom clears when options empty', () => {
    expect(
      resolveDropdownRefresh({
        allowCustom: false,
        multiple: false,
        current: 'gone',
        options: [],
      }),
    ).toEqual({ action: 'clear' });
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
});
