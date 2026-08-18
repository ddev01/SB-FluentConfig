using System;
using System.Collections.Generic;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Option methods applied to the pending control before it is flushed into the schema.
    /// </summary>
    public interface IControlOptions
    {
        void Hint(string text);
        void Default(bool value);
        void Default(string value);
        void Default(string[] values);
        void Default(int value);
        void Default(double value);
        void Range(int min, int max);
        void Range(double min, double max);
        void Step(double value);
        void Password();
        void Multiline();
        void ShowWhen(string key);
        void ShowWhen(string key, string equalsValue);
        void ShowWhen(string key, int equalsValue);
        void ShowWhenNot(string key, string equalsValue);
        void ShowWhenNot(string key, int equalsValue);
        void ShowWhen(string key, Comparator op, int value);
        void ShowWhen(string key, Comparator op, string compareKey);
        void Size(string spec);
        void Options(string[] options);
        void OptionsPairs(IEnumerable<(string Value, string Display)> pairOptions);
        void WithPairValue(string valueKey);
        void DefaultByValue(string value);
        void DefaultIndex(int index);
        void WithExclusive(string[] options);
        void MaxSelected(int n);
        void DefaultIndices(int[] indices);
        void Refresh(Func<string[]> callback);
        void RefreshPairs(Func<IEnumerable<(string Value, string Display)>> callback);
        void Searchable();
        void AllowCustom();
        void Multiple();
        void Preset(string[] values);
        void AllowDuplicates(bool value);
        void Color(string hex);
        void Text(string caption);
        void OnClick(Action<UiContext> callback);
        /// <summary>Filepath only: omit the built-in Browse button.</summary>
        void HideBrowse();
        /// <summary>Filepath only: non-empty values must exist on disk (default true). Empty stays allowed.</summary>
        void MustExist(bool value = true);
        /// <summary>Filepath only: allowed extensions (with or without a leading dot).</summary>
        void Accept(params string[] extensions);
        void WithPermanentOption(bool value);
        void WithStepper(bool value = true);
        /// <summary>
        /// Defines nested per-pill schema via a fluent sub-builder (never a WPF Panel).
        /// </summary>
        void ItemTemplate(Action<PanelBuilder> build);
        /// <summary>Invoked when a pill is added; receives item name + callback context (no Panel).</summary>
        void OnPillAdded(Action<string, CallbackContext> callback);
        /// <summary>Invoked when a pill is removed; receives item name + callback context (no Panel).</summary>
        void OnPillRemoved(Action<string, CallbackContext> callback);
    }
}
