using System;
using System.Collections.Generic;
using System.Text;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Mutable pending control; flushed into a <see cref="SchemaNode"/> when the next control starts.
    /// </summary>
    public sealed partial class PendingControl : IControlOptions
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

        public void BeginToggle(string label, string key) { Reset(Kind.Toggle, label, RequireSaveKey(key)); }
        public void BeginTextbox(string label, string key) { Reset(Kind.Textbox, label, RequireSaveKey(key)); }
        public void BeginSlider(string label, string key) { Reset(Kind.Slider, label, RequireSaveKey(key)); }
        public void BeginButton(string label) { Reset(Kind.Button, label, null); _buttonId = StableButtonId(label); }
        public void BeginNumberInput(string label, string key) { Reset(Kind.NumberInput, label, RequireSaveKey(key)); _valueType = "double"; }
        public void BeginIntegerInput(string label, string key) { Reset(Kind.IntegerInput, label, RequireSaveKey(key)); _valueType = "int"; }
        public void BeginDurationInput(string label, string key) { Reset(Kind.DurationInput, label, RequireSaveKey(key)); }
        public void BeginFilepath(string label, string key) { Reset(Kind.Filepath, label, RequireSaveKey(key)); }
        public void BeginColorPicker(string label, string key) { Reset(Kind.ColorPicker, label, RequireSaveKey(key)); }
        public void BeginDropdown(string label, string key) { Reset(Kind.Dropdown, label, RequireSaveKey(key)); }
        public void BeginDynamicTextboxes(string label, string key) { Reset(Kind.DynamicTextboxes, label, RequireSaveKey(key)); }
        public void BeginPillInput(string label, string key) { Reset(Kind.PillInput, label, RequireSaveKey(key)); }

        private static string RequireSaveKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("saveKey is required (non-null, non-empty).", nameof(key));
            return key.Trim();
        }

        private static string StableButtonId(string label)
        {
            var slug = string.IsNullOrWhiteSpace(label) ? "button" : label.Trim().ToLowerInvariant();
            var sb = new StringBuilder("btn_");
            foreach (var ch in slug)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else if (ch == ' ' || ch == '-' || ch == '_') sb.Append('_');
            }
            if (sb.Length <= 4) sb.Append("action");
            return sb.ToString();
        }

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
            if (_kind != Kind.Button && string.IsNullOrWhiteSpace(_saveKey))
                throw new InvalidOperationException($"Control '{_label}' ({_kind}) requires a non-empty saveKey.");
            if (_kind != Kind.Button && !_target.TryReserveSaveKey(_saveKey))
                throw new InvalidOperationException($"Duplicate saveKey '{_saveKey}' within the same section/panel.");
            if (_kind == Kind.Toggle && _exclusiveOptions != null && _exclusiveOptions.Length > 0 && _defaultBool.HasValue)
                throw new InvalidOperationException(
                    "Cannot combine .Default(bool) with .WithExclusive(...). Use .DefaultIndex / .DefaultIndices instead.");
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
    }
}

