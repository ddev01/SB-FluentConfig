using System;
using System.Collections.Generic;
using System.Linq;
using FluentConfig.Core;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Per-control-kind <see cref="SchemaNode"/> builders for <see cref="PendingControl"/>.
    /// </summary>
    public sealed partial class PendingControl
    {
        private SchemaNode BuildNode()
        {
            switch (_kind)
            {
                case Kind.Toggle: return BuildToggle();
                case Kind.Textbox: return BuildTextbox();
                case Kind.Slider: return BuildSlider();
                case Kind.Button: return BuildButton();
                case Kind.NumberInput:
                case Kind.IntegerInput: return BuildNumberInput();
                case Kind.DurationInput: return BuildDurationInput();
                case Kind.Filepath: return BuildFilepath();
                case Kind.ColorPicker: return BuildColorPicker();
                case Kind.Dropdown: return BuildDropdown();
                case Kind.DynamicTextboxes: return BuildDynamicTextboxes();
                case Kind.PillInput: return BuildPill();
                default: return null;
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

        private TextboxNode BuildTextbox() => new TextboxNode
        {
            Id = _saveKey,
            Label = _label,
            SaveKey = _saveKey,
            Hint = _hint,
            DefaultValue = _defaultString,
            Password = _password,
            Multiline = _multiline,
        };

        private SliderNode BuildSlider()
        {
            // Prefer int range; accept double range by rounding so .Range(0.0, 100.0) works.
            int min = _rangeMinInt ?? (_rangeMinDbl.HasValue ? (int)Math.Round(_rangeMinDbl.Value) : 0);
            int max = _rangeMaxInt ?? (_rangeMaxDbl.HasValue ? (int)Math.Round(_rangeMaxDbl.Value) : 100);
            int? def = _defaultInt;
            if (!def.HasValue && _defaultDouble.HasValue)
                def = (int)Math.Round(_defaultDouble.Value);
            return new SliderNode
            {
                Id = _saveKey,
                Label = _label,
                SaveKey = _saveKey,
                Hint = _hint,
                Min = min,
                Max = max,
                DefaultValue = def,
            };
        }

        private ButtonNode BuildButton()
        {
            var btn = new ButtonNode
            {
                Id = _buttonId,
                Label = _label,
                Hint = _hint,
                Text = string.IsNullOrWhiteSpace(_buttonText) ? (_label ?? "OK") : _buttonText,
                Color = _colorHex,
            };
            if (_onClick != null)
                _session.RegisterButtonClick(_buttonId, _onClick);
            return btn;
        }

        private NumberInputNode BuildNumberInput() => new NumberInputNode
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

        private DurationInputNode BuildDurationInput() => new DurationInputNode
        {
            Id = _saveKey,
            Label = _label,
            SaveKey = _saveKey,
            Hint = _hint,
            DefaultValue = _defaultString,
            PermanentOption = _permanentOption,
        };

        private FilepathNode BuildFilepath()
        {
            var accept = FilepathValidation.NormalizeAccept(_accept);
            var node = new FilepathNode
            {
                Id = _saveKey,
                Label = _label,
                SaveKey = _saveKey,
                Hint = _hint,
                DefaultValue = _defaultString,
                HideBrowse = _hideBrowse,
                MustExist = _mustExist,
                Accept = accept,
            };
            _session.RegisterFilepathRule(new FilepathRule
            {
                SaveKey = _saveKey,
                Label = _label,
                MustExist = _mustExist,
                Accept = accept,
                PillSaveKey = _session.ItemTemplatePillKey,
            });
            return node;
        }

        private ColorPickerNode BuildColorPicker() => new ColorPickerNode
        {
            Id = _saveKey,
            Label = _label,
            SaveKey = _saveKey,
            Hint = _hint,
            DefaultValue = _defaultString,
        };

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

            Func<IList<DropdownOption>> refreshFn = null;
            if (_refresh != null || _refreshPairs != null)
            {
                refreshFn = () =>
                {
                    if (_refreshPairs != null)
                    {
                        return _refreshPairs()?.Select(p => new DropdownOption { Value = p.Value, Display = p.Display }).ToList()
                               ?? (IList<DropdownOption>)Array.Empty<DropdownOption>();
                    }
                    var arr = _refresh?.Invoke() ?? Array.Empty<string>();
                    return arr.Select(o => new DropdownOption { Value = o, Display = o }).ToList();
                };

                _session?.RegisterDropdownRefresh(_saveKey, refreshFn);

                // Refresh-only dropdowns (no static .Options) start empty unless seeded here.
                if (opts == null || opts.Count == 0)
                    opts = refreshFn();
            }

            var node = new DropdownNode
            {
                Id = _saveKey,
                Label = _label,
                SaveKey = _saveKey,
                Hint = _hint,
                Options = opts,
                ValueSaveKey = _valueKey,
                Refreshable = refreshFn != null,
                DefaultIndex = _defaultIndex,
                DefaultByValue = _defaultByValue,
                DefaultValue = _defaultString,
                Searchable = _searchable ? true : (bool?)null,
                AllowCustom = _allowCustom ? true : (bool?)null,
                Multiple = _multiple ? true : (bool?)null,
                DefaultValues = _defaultStrings,
            };

            return node;
        }

        private DynamicTextboxesNode BuildDynamicTextboxes() => new DynamicTextboxesNode
        {
            Id = _saveKey,
            Label = _label,
            SaveKey = _saveKey,
            Hint = _hint,
            Preset = _preset,
            AllowDuplicates = _allowDuplicates,
        };

        private PillInputNode BuildPill()
        {
            IList<SchemaNode> template = null;
            if (_itemTemplate != null)
            {
                var list = new SchemaNodeList();
                var previousPill = _session.ItemTemplatePillKey;
                _session.ItemTemplatePillKey = _saveKey;
                _session.PushItemTemplateScope();
                try
                {
                    var pb = new PanelBuilder(_session, list);
                    _itemTemplate(pb);
                    pb.FlushPending();
                    template = list.ToList();
                }
                finally
                {
                    _session.PopItemTemplateScope();
                    _session.ItemTemplatePillKey = previousPill;
                }
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
    }
}
