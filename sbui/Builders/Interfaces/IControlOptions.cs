using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Option methods that can be applied to control builders (Hint, Default, Range, etc.).
    /// Implemented by control builders so the fluent wrapper can forward without dynamic.
    /// Unsupported methods are no-op.
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
        void ToggleDefault(bool value);
        void Color(string hex);
        void Text(string caption);
        void OnClick(Action<UiContext> callback);
        void WithPermanentOption(bool value);
        void WithStepper(bool value = true);
        void WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build);
        void OnPillAdded(Action<string, StackPanel, CallbackContext> build);
        void OnPillRemoved(Action<string, StackPanel, CallbackContext> build);
    }
}
