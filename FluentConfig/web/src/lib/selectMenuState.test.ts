import { describe, expect, it } from 'vitest';
import {
  canOpenMenu,
  chipLabel,
  computeEmptyListMessage,
  computeListBoxStyle,
  computeOpenMenuStart,
  computeSuggestionOptions,
  matchCustomInput,
} from './selectMenuState';

const opts = [
  { value: 'a', display: 'Alpha' },
  { value: 'b', display: 'Beta' },
];

describe('computeSuggestionOptions', () => {
  it('filters out selected chips in multiple mode', () => {
    expect(
      computeSuggestionOptions({
        options: opts,
        selectedValues: ['a'],
        multiple: true,
        searchable: false,
        open: true,
        filterText: '',
      }),
    ).toEqual([opts[1]]);
  });

  it('applies the search filter only while open', () => {
    const closed = computeSuggestionOptions({
      options: opts,
      selectedValues: [],
      multiple: false,
      searchable: true,
      open: false,
      filterText: 'bet',
    });
    expect(closed).toEqual(opts);
    const opened = computeSuggestionOptions({
      options: opts,
      selectedValues: [],
      multiple: false,
      searchable: true,
      open: true,
      filterText: 'bet',
    });
    expect(opened).toEqual([opts[1]]);
  });
});

describe('canOpenMenu', () => {
  it('blocks disabled, already-open, and suppressOpenOnFocus', () => {
    const base = {
      disabled: false,
      open: false,
      suppressOpenOnFocus: false,
      allowCustom: false,
      optionsLength: 2,
    };
    expect(canOpenMenu(base)).toBe(true);
    expect(canOpenMenu({ ...base, disabled: true })).toBe(false);
    expect(canOpenMenu({ ...base, open: true })).toBe(false);
    expect(canOpenMenu({ ...base, suppressOpenOnFocus: true })).toBe(false);
  });

  it('blocks empty non-custom lists but allows custom', () => {
    expect(
      canOpenMenu({
        disabled: false,
        open: false,
        suppressOpenOnFocus: false,
        allowCustom: false,
        optionsLength: 0,
      }),
    ).toBe(false);
    expect(
      canOpenMenu({
        disabled: false,
        open: false,
        suppressOpenOnFocus: false,
        allowCustom: true,
        optionsLength: 0,
      }),
    ).toBe(true);
  });
});

describe('computeOpenMenuStart', () => {
  it('seeds filterText from the display label for searchable single', () => {
    expect(
      computeOpenMenuStart({
        searchable: true,
        multiple: false,
        displayLabel: 'Alpha',
        selectedIndex: 0,
        options: opts,
      }),
    ).toEqual({ filterText: 'Alpha', activeIndex: 0 });
  });

  it('starts at 0 when nothing is selected', () => {
    expect(
      computeOpenMenuStart({
        searchable: false,
        multiple: false,
        displayLabel: '',
        selectedIndex: -1,
        options: opts,
      }).activeIndex,
    ).toBe(0);
  });
});

describe('computeEmptyListMessage', () => {
  it('prompts Enter when custom text is typed', () => {
    expect(
      computeEmptyListMessage({
        filterText: 'new',
        allowCustom: true,
        multiple: false,
        optionsLength: 2,
        suggestionLength: 0,
      }),
    ).toBe('Press Enter to add “new”');
  });

  it('reports all options selected in multiple mode', () => {
    expect(
      computeEmptyListMessage({
        filterText: '',
        allowCustom: false,
        multiple: true,
        optionsLength: 2,
        suggestionLength: 0,
      }),
    ).toBe('All options selected');
  });
});

describe('chipLabel / matchCustomInput', () => {
  it('falls back to the raw value when no option matches', () => {
    expect(chipLabel(opts, 'gone')).toBe('gone');
    expect(chipLabel(opts, 'a')).toBe('Alpha');
  });

  it('matches custom input on value or display', () => {
    expect(matchCustomInput(opts, 'a')?.value).toBe('a');
    expect(matchCustomInput(opts, 'beta')?.value).toBe('b');
    expect(matchCustomInput(opts, '  ')).toBeUndefined();
  });
});

describe('computeListBoxStyle', () => {
  it('opens below when there is room', () => {
    const css = computeListBoxStyle({
      left: 10,
      top: 100,
      bottom: 140,
      width: 200,
      viewportHeight: 800,
    });
    expect(css).toContain('position:fixed');
    expect(css).toContain('top:146px');
    expect(css).not.toContain('bottom:');
  });

  it('opens above when the trigger is near the bottom', () => {
    const css = computeListBoxStyle({
      left: 10,
      top: 760,
      bottom: 790,
      width: 200,
      viewportHeight: 800,
    });
    expect(css).toContain('bottom:46px');
    expect(css).not.toContain('top:');
  });
});
