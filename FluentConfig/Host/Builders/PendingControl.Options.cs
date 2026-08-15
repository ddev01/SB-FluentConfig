using System;
using System.Collections.Generic;
using System.Linq;

namespace FluentConfig
{
    /// <summary>Fluent option setters + kind-guards for <see cref="PendingControl"/>.</summary>
    public sealed partial class PendingControl
    {
        private void RequireKind(string option, params Kind[] allowed)
        {
            if (_kind == Kind.None)
                throw new InvalidOperationException($".{option}() requires an active control.");
            foreach (var k in allowed)
            {
                if (_kind == k) return;
            }
            throw new InvalidOperationException(
                $".{option}() is not valid for {_kind}. Allowed: {string.Join(", ", allowed)}.");
        }

        public void Hint(string text) => _hint = text;
        public void Default(bool value)
        {
            RequireKind(nameof(Default), Kind.Toggle);
            _defaultBool = value;
        }
        public void Default(string value)
        {
            RequireKind(nameof(Default), Kind.Textbox, Kind.DurationInput, Kind.Filepath, Kind.ColorPicker, Kind.NumberInput, Kind.IntegerInput, Kind.Dropdown);
            _defaultString = value;
            if (_kind == Kind.Dropdown && !_multiple && string.IsNullOrEmpty(_defaultByValue))
                _defaultByValue = value;
        }
        public void Default(string[] values)
        {
            RequireKind(nameof(Default), Kind.Dropdown);
            if (!_multiple)
                throw new InvalidOperationException(".Default(string[]) requires .Multiple().");
            _defaultStrings = values ?? Array.Empty<string>();
        }
        public void Default(int value)
        {
            RequireKind(nameof(Default), Kind.Slider, Kind.NumberInput, Kind.IntegerInput);
            _defaultInt = value;
        }
        public void Default(double value)
        {
            RequireKind(nameof(Default), Kind.Slider, Kind.NumberInput, Kind.IntegerInput);
            _defaultDouble = value;
        }
        public void Range(int min, int max)
        {
            RequireKind(nameof(Range), Kind.Slider, Kind.NumberInput, Kind.IntegerInput);
            _rangeMinInt = min;
            _rangeMaxInt = max;
        }
        public void Range(double min, double max)
        {
            RequireKind(nameof(Range), Kind.Slider, Kind.NumberInput, Kind.IntegerInput);
            _rangeMinDbl = min;
            _rangeMaxDbl = max;
        }
        public void Step(double value)
        {
            RequireKind(nameof(Step), Kind.NumberInput, Kind.IntegerInput);
            _step = value;
        }
        public void Password()
        {
            RequireKind(nameof(Password), Kind.Textbox);
            _password = true;
        }
        public void Multiline()
        {
            RequireKind(nameof(Multiline), Kind.Textbox);
            _multiline = true;
        }
        public void ShowWhen(string key)
        {
            SetEqualsShowWhen(key, true, inverted: false);
        }

        public void ShowWhen(string key, string equalsValue)
        {
            SetEqualsShowWhen(key, equalsValue, inverted: false);
        }

        public void ShowWhen(string key, int equalsValue)
        {
            SetEqualsShowWhen(key, equalsValue, inverted: false);
        }

        public void ShowWhenNot(string key, string equalsValue)
        {
            SetEqualsShowWhen(key, equalsValue, inverted: true);
        }

        public void ShowWhenNot(string key, int equalsValue)
        {
            SetEqualsShowWhen(key, equalsValue, inverted: true);
        }

        private void SetEqualsShowWhen(string key, object equalsValue, bool inverted)
        {
            _showWhenKey = key;
            _showWhenOp = null;
            _showWhenValue = null;
            _showWhenCompareKey = null;
            _showWhenEquals = equalsValue;
            _showWhenInverted = inverted;
        }

        public void ShowWhen(string key, Comparator op, int value)
        {
            _showWhenKey = key;
            _showWhenOp = op;
            _showWhenValue = value;
            _showWhenCompareKey = null;
            _showWhenEquals = null;
            _showWhenInverted = false;
        }

        public void ShowWhen(string key, Comparator op, string compareKey)
        {
            if (string.IsNullOrWhiteSpace(compareKey))
                throw new ArgumentException("compareKey is required (non-null, non-empty).", nameof(compareKey));
            _showWhenKey = key;
            _showWhenOp = op;
            _showWhenValue = null;
            _showWhenCompareKey = compareKey.Trim();
            _showWhenEquals = null;
            _showWhenInverted = false;
        }

        public void Size(string spec)
        {
            var hint = LayoutTokens.ParseSize(spec);
            if ((hint.Grow.HasValue || hint.Shrink.HasValue) &&
                !string.Equals(_target.ContainerMode, "row", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $".Size(\"{spec}\") uses grow/shrink, which only apply directly inside a Row(...) container.");
            }
            if (hint.Span.HasValue &&
                !string.Equals(_target.ContainerMode, "grid", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $".Size(\"{spec}\") uses col-span, which only apply directly inside a Grid(...) container.");
            }
            _sizeHint = hint;
        }

        public void Options(string[] options)
        {
            RequireKind(nameof(Options), Kind.Dropdown);
            _options = options;
            _pairOptions = null;
        }
        public void OptionsPairs(IEnumerable<(string Value, string Display)> pairOptions)
        {
            RequireKind(nameof(OptionsPairs), Kind.Dropdown);
            _pairOptions = pairOptions?.ToList();
            _options = _pairOptions?.Select(p => p.Display).ToArray();
        }
        public void WithPairValue(string valueKey)
        {
            RequireKind(nameof(WithPairValue), Kind.Dropdown);
            _valueKey = valueKey;
        }
        public void DefaultByValue(string value)
        {
            RequireKind(nameof(DefaultByValue), Kind.Dropdown);
            _defaultByValue = value;
        }
        public void DefaultIndex(int index)
        {
            RequireKind(nameof(DefaultIndex), Kind.Dropdown, Kind.Toggle);
            _defaultIndex = index;
            _defaultIndices = new[] { index };
        }
        public void WithExclusive(string[] options)
        {
            RequireKind(nameof(WithExclusive), Kind.Toggle);
            _exclusiveOptions = options ?? Array.Empty<string>();
        }
        public void MaxSelected(int n)
        {
            RequireKind(nameof(MaxSelected), Kind.Toggle);
            _maxSelected = Math.Max(1, n);
        }
        public void DefaultIndices(int[] indices)
        {
            RequireKind(nameof(DefaultIndices), Kind.Toggle);
            _defaultIndices = indices;
        }
        public void Refresh(Func<string[]> callback)
        {
            RequireKind(nameof(Refresh), Kind.Dropdown);
            _refresh = callback;
            _refreshPairs = null;
        }
        public void RefreshPairs(Func<IEnumerable<(string Value, string Display)>> callback)
        {
            RequireKind(nameof(RefreshPairs), Kind.Dropdown);
            _refresh = null;
            _refreshPairs = callback;
        }
        public void Searchable()
        {
            RequireKind(nameof(Searchable), Kind.Dropdown);
            _searchable = true;
        }
        public void AllowCustom()
        {
            RequireKind(nameof(AllowCustom), Kind.Dropdown);
            _allowCustom = true;
        }
        public void Multiple()
        {
            RequireKind(nameof(Multiple), Kind.Dropdown);
            _multiple = true;
        }
        public void Preset(string[] values)
        {
            RequireKind(nameof(Preset), Kind.DynamicTextboxes);
            _preset = values;
        }
        public void AllowDuplicates(bool value)
        {
            RequireKind(nameof(AllowDuplicates), Kind.DynamicTextboxes);
            _allowDuplicates = value;
        }
        public void Color(string hex)
        {
            RequireKind(nameof(Color), Kind.Button);
            _colorHex = hex;
        }
        public void Text(string caption)
        {
            RequireKind(nameof(Text), Kind.Button);
            _buttonText = caption;
        }
        public void OnClick(Action<UiContext> callback)
        {
            RequireKind(nameof(OnClick), Kind.Button);
            _onClick = callback;
        }
        public void WithPermanentOption(bool value)
        {
            RequireKind(nameof(WithPermanentOption), Kind.DurationInput);
            _permanentOption = value;
        }
        public void WithStepper(bool value = true)
        {
            RequireKind(nameof(WithStepper), Kind.NumberInput, Kind.IntegerInput);
            _stepper = value;
        }
        public void ItemTemplate(Action<PanelBuilder> build)
        {
            RequireKind(nameof(ItemTemplate), Kind.PillInput);
            _itemTemplate = build;
        }
        public void OnPillAdded(Action<string, CallbackContext> callback)
        {
            RequireKind(nameof(OnPillAdded), Kind.PillInput);
            _onPillAdded = callback;
        }
        public void OnPillRemoved(Action<string, CallbackContext> callback)
        {
            RequireKind(nameof(OnPillRemoved), Kind.PillInput);
            _onPillRemoved = callback;
        }
    }
}
