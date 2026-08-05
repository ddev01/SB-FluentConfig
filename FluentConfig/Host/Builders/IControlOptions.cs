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
        void Default(int value);
        void Default(double value);
        void Range(int min, int max);
        void Range(double min, double max);
        void Step(double value);
        void Password();
        void Multiline();
        void ShowWhen(string key);
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
        void Preset(string[] values);
        void AllowDuplicates(bool value);
        void Color(string hex);
        void Text(string caption);
        void OnClick(Action<UiContext> callback);
        void WithPermanentOption(bool value);
        void WithStepper(bool value = true);
        /// <summary>
        /// Defines nested per-pill schema via a fluent sub-builder (never a WPF Panel).
        /// Replaces the old WithSectionsPanel(StackPanel, Panel, ...) leak.
        /// </summary>
        void ItemTemplate(Action<PanelBuilder> build);
        /// <summary>Invoked when a pill is added; receives item name + callback context (no Panel).</summary>
        void OnPillAdded(Action<string, CallbackContext> callback);
        /// <summary>Invoked when a pill is removed; receives item name + callback context (no Panel).</summary>
        void OnPillRemoved(Action<string, CallbackContext> callback);
    }
}
