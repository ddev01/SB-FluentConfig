using System;
using System.Collections.Generic;
using System.Linq;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Mutable pending control; flushed into a <see cref="SchemaNode"/> when the next control starts.
    /// </summary>
    public sealed class PendingControl : IControlOptions
    {
        private enum Kind
        {
            None,
            Toggle,
            Textbox,
            Slider,
            Button,
            NumberInput,
            IntegerInput,
            DurationInput,
            Filepath,
            ColorPicker,
            Dropdown,
            DynamicTextboxes,
            PillInput,
        }

        private readonly FluentConfigSession _session;
        private readonly SchemaNodeList _target;
        private Kind _kind;
        private string _label;
        private string _saveKey;
        private string _hint;
        private string _showWhenKey;

        private bool? _defaultBool;
        private string _defaultString;
        private int? _defaultInt;
        private double? _defaultDouble;
        private int? _rangeMinInt;
        private int? _rangeMaxInt;
        private double? _rangeMinDbl;
        private double? _rangeMaxDbl;
        private double? _step;
        private bool _password;
        private bool _multiline;
        private string[] _options;
        private List<(string Value, string Display)> _pairOptions;
        private string _valueKey;
        private string _defaultByValue;
        private int? _defaultIndex;
        private string[] _exclusiveOptions;
        private int _maxSelected = 1;
        private int[] _defaultIndices;
        private Func<string[]> _refresh;
        private Func<IEnumerable<(string Value, string Display)>> _refreshPairs;
        private string[] _preset;
        private bool _allowDuplicates = true;
        private string _colorHex;
        private string _buttonText;
        private Action<UiContext> _onClick;
        private bool _permanentOption = true;
        private bool _stepper;
        private string _valueType;
        private Action<PanelBuilder> _itemTemplate;
        private Action<string, CallbackContext> _onPillAdded;
        private Action<string, CallbackContext> _onPillRemoved;
        private string _buttonId;

        public PendingControl(FluentConfigSession session, SchemaNodeList target)
        {
            _session = session;
            _target = target;
        }

        public void BeginToggle(string label, string key) { Reset(Kind.Toggle, label, key); }
        public void BeginTextbox(string label, string key) { Reset(Kind.Textbox, label, key); }
        public void BeginSlider(string label, string key) { Reset(Kind.Slider, label, key); }
        public void BeginButton(string label) { Reset(Kind.Button, label, null); _buttonId = "btn_" + Guid.NewGuid().ToString("N").Substring(0, 8); }
        public void BeginNumberInput(string label, string key) { Reset(Kind.NumberInput, label, key); _valueType = "double"; }
        public void BeginIntegerInput(string label, string key) { Reset(Kind.IntegerInput, label, key); _valueType = "int"; }
        public void BeginInput(string label, string key) { Reset(Kind.NumberInput, label, key); _valueType = "string"; }
        public void BeginDurationInput(string label, string key) { Reset(Kind.DurationInput, label, key); }
        public void BeginFilepath(string label, string key) { Reset(Kind.Filepath, label, key); }
        public void BeginColorPicker(string label, string key) { Reset(Kind.ColorPicker, label, key); }
        public void BeginDropdown(string label, string key) { Reset(Kind.Dropdown, label, key); }
        public void BeginDynamicTextboxes(string label, string key) { Reset(Kind.DynamicTextboxes, label, key); }
        public void BeginPillInput(string label, string key) { Reset(Kind.PillInput, label, key); }

        private void Reset(Kind kind, string label, string key)
        {
            _kind = kind;
            _label = label ?? "";
            _saveKey = key;
            _hint = null;
            _showWhenKey = null;
            _defaultBool = null;
            _defaultString = null;
            _defaultInt = null;
            _defaultDouble = null;
            _rangeMinInt = null;
            _rangeMaxInt = null;
            _rangeMinDbl = null;
            _rangeMaxDbl = null;
            _step = null;
            _password = false;
            _multiline = false;
            _options = null;
            _pairOptions = null;
            _valueKey = null;
            _defaultByValue = null;
            _defaultIndex = null;
            _exclusiveOptions = null;
            _maxSelected = 1;
            _defaultIndices = null;
            _refresh = null;
            _refreshPairs = null;
            _preset = null;
            _allowDuplicates = true;
            _colorHex = null;
            _buttonText = null;
            _onClick = null;
            _permanentOption = true;
            _stepper = false;
            _valueType = kind == Kind.IntegerInput ? "int" : kind == Kind.NumberInput ? "double" : null;
            _itemTemplate = null;
            _onPillAdded = null;
            _onPillRemoved = null;
            if (kind != Kind.Button)
                _buttonId = null;
        }

        public void Flush()
        {
            if (_kind == Kind.None) return;
            var node = BuildNode();
            if (node != null)
            {
                if (!string.IsNullOrEmpty(_showWhenKey))
                {
                    node.Visibility = new VisibilityCondition
                    {
                        SaveKey = _saveKeyPath(_showWhenKey),
                        EqualsValue = true,
                        Inverted = false,
                    };
                }
                _target.Add(node);
            }
            _kind = Kind.None;
        }

        private static string _saveKeyPath(string key) => key ?? "";

        private SchemaNode BuildNode()
        {
            switch (_kind)
            {
                case Kind.Toggle:
                    return BuildToggle();
                case Kind.Textbox:
                    return new TextboxNode
                    {
                        Id = _saveKey,
                        Label = _label,
                        SaveKey = _saveKey,
                        Hint = _hint,
                        DefaultValue = _defaultString,
                        Password = _password,
                        Multiline = _multiline,
                    };
                case Kind.Slider:
                    return new SliderNode
                    {
                        Id = _saveKey,
                        Label = _label,
                        SaveKey = _saveKey,
                        Hint = _hint,
                        Min = _rangeMinInt ?? 0,
                        Max = _rangeMaxInt ?? 100,
                        DefaultValue = _defaultInt,
                    };
                case Kind.Button:
                    var btn = new ButtonNode
                    {
                        Id = _buttonId,
                        Label = _label,
                        Hint = _hint,
                        Text = _buttonText ?? "OK",
                        Color = _colorHex,
                    };
                    if (_onClick != null)
                        _session.RegisterButtonClick(_buttonId, _onClick);
                    return btn;
                case Kind.NumberInput:
                case Kind.IntegerInput:
                    return new NumberInputNode
                    {
                        Id = _saveKey,
                        Label = _label,
                        SaveKey = _saveKey,
                        Hint = _hint,
                        ValueType = _valueType ?? "double",
                        Min = _rangeMinDbl ?? _rangeMinInt,
                        Max = _rangeMaxDbl ?? _rangeMaxInt,
                        Step = _step,
                        DefaultValue = (object)_defaultDouble ?? _defaultInt ?? (object)_defaultString,
                        Stepper = _stepper,
                    };
                case Kind.DurationInput:
                    return new DurationInputNode
                    {
                        Id = _saveKey,
                        Label = _label,
                        SaveKey = _saveKey,
                        Hint = _hint,
                        DefaultValue = _defaultString,
                        PermanentOption = _permanentOption,
                    };
                case Kind.Filepath:
                    return new FilepathNode
                    {
                        Id = _saveKey,
                        Label = _label,
                        SaveKey = _saveKey,
                        Hint = _hint,
                        DefaultValue = _defaultString,
                    };
                case Kind.ColorPicker:
                    return new ColorPickerNode
                    {
                        Id = _saveKey,
                        Label = _label,
                        SaveKey = _saveKey,
                        Hint = _hint,
                        DefaultValue = _defaultString,
                    };
                case Kind.Dropdown:
                    return BuildDropdown();
                case Kind.DynamicTextboxes:
                    return new DynamicTextboxesNode
                    {
                        Id = _saveKey,
                        Label = _label,
                        SaveKey = _saveKey,
                        Hint = _hint,
                        Preset = _preset,
                        AllowDuplicates = _allowDuplicates,
                    };
                case Kind.PillInput:
                    return BuildPill();
                default:
                    return null;
            }
        }

        private ToggleNode BuildToggle()
        {
            var node = new ToggleNode
            {
                Id = _saveKey,
                Label = _label,
                SaveKey = _saveKey,
                Hint = _hint,
            };
            if (_exclusiveOptions != null && _exclusiveOptions.Length > 0)
            {
                node.Exclusive = new ExclusiveToggleOptions
                {
                    Options = _exclusiveOptions,
                    MaxSelected = _maxSelected,
                    DefaultIndex = _defaultIndices != null && _defaultIndices.Length == 1 ? _defaultIndices[0] : (int?)null,
                    DefaultIndices = _defaultIndices,
                };
            }
            else
            {
                node.DefaultValue = _defaultBool;
            }
            return node;
        }

        private DropdownNode BuildDropdown()
        {
            IList<DropdownOption> opts = null;
            if (_pairOptions != null)
            {
                opts = _pairOptions.Select(p => new DropdownOption { Value = p.Value, Display = p.Display }).ToList();
            }
            else if (_options != null)
            {
                opts = _options.Select(o => new DropdownOption { Value = o, Display = o }).ToList();
            }

            var node = new DropdownNode
            {
                Id = _saveKey,
                Label = _label,
                SaveKey = _saveKey,
                Hint = _hint,
                Options = opts,
                ValueSaveKey = _valueKey,
                Refreshable = _refresh != null || _refreshPairs != null,
                DefaultIndex = _defaultIndex,
                DefaultByValue = _defaultByValue,
            };

            if (_refresh != null || _refreshPairs != null)
            {
                _session.RegisterDropdownRefresh(_saveKey, () =>
                {
                    if (_refreshPairs != null)
                    {
                        return _refreshPairs()?.Select(p => new DropdownOption { Value = p.Value, Display = p.Display }).ToList()
                               ?? (IList<DropdownOption>)Array.Empty<DropdownOption>();
                    }
                    var arr = _refresh?.Invoke() ?? Array.Empty<string>();
                    return arr.Select(o => new DropdownOption { Value = o, Display = o }).ToList();
                });
            }

            return node;
        }

        private PillInputNode BuildPill()
        {
            IList<SchemaNode> template = null;
            if (_itemTemplate != null)
            {
                var list = new SchemaNodeList();
                var pb = new PanelBuilder(_session, list);
                _itemTemplate(pb);
                pb.FlushPending();
                template = list.ToList();
            }

            var node = new PillInputNode
            {
                Id = _saveKey,
                Label = _label,
                SaveKey = _saveKey,
                Hint = _hint,
                ItemTemplate = template,
            };

            _session.RegisterPillCallbacks(_saveKey, template, _onPillAdded, _onPillRemoved);
            return node;
        }

        // ── IControlOptions ──

        public void Hint(string text) => _hint = text;
        public void Default(bool value) => _defaultBool = value;
        public void Default(string value) => _defaultString = value;
        public void Default(int value) => _defaultInt = value;
        public void Default(double value) => _defaultDouble = value;
        public void Range(int min, int max) { _rangeMinInt = min; _rangeMaxInt = max; }
        public void Range(double min, double max) { _rangeMinDbl = min; _rangeMaxDbl = max; }
        public void Step(double value) => _step = value;
        public void Password() => _password = true;
        public void Multiline() => _multiline = true;
        public void ShowWhen(string key) => _showWhenKey = key;
        public void Options(string[] options) { _options = options; _pairOptions = null; }
        public void OptionsPairs(IEnumerable<(string Value, string Display)> pairOptions)
        {
            _pairOptions = pairOptions?.ToList();
            _options = _pairOptions?.Select(p => p.Display).ToArray();
        }
        public void WithPairValue(string valueKey) => _valueKey = valueKey;
        public void DefaultByValue(string value) => _defaultByValue = value;
        public void DefaultIndex(int index) { _defaultIndex = index; _defaultIndices = new[] { index }; }
        public void WithExclusive(string[] options) => _exclusiveOptions = options ?? Array.Empty<string>();
        public void MaxSelected(int n) => _maxSelected = Math.Max(1, n);
        public void DefaultIndices(int[] indices) => _defaultIndices = indices;
        public void Refresh(Func<string[]> callback) { _refresh = callback; _refreshPairs = null; }
        public void RefreshPairs(Func<IEnumerable<(string Value, string Display)>> callback)
        {
            _refresh = null;
            _refreshPairs = callback;
        }
        public void Preset(string[] values) => _preset = values;
        public void AllowDuplicates(bool value) => _allowDuplicates = value;
        public void ToggleDefault(bool value) => _defaultBool = value;
        public void Color(string hex) => _colorHex = hex;
        public void Text(string caption) => _buttonText = caption;
        public void OnClick(Action<UiContext> callback) => _onClick = callback;
        public void WithPermanentOption(bool value) => _permanentOption = value;
        public void WithStepper(bool value = true) => _stepper = value;
        public void ItemTemplate(Action<PanelBuilder> build) => _itemTemplate = build;
        public void OnPillAdded(Action<string, CallbackContext> callback) => _onPillAdded = callback;
        public void OnPillRemoved(Action<string, CallbackContext> callback) => _onPillRemoved = callback;
        public void Type(string value) => _valueType = value;
    }
}
