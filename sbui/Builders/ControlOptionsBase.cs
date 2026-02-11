using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Sbui
{
    /// <summary>
    /// Base for control builders that implement IControlOptions. Provides virtual no-op defaults;
    /// override only the methods relevant to each control type.
    /// </summary>
    public abstract class ControlOptionsBase : IControlOptions
    {
        public virtual void Hint(string text) { }
        public virtual void Default(bool value) { }
        public virtual void Default(string value) { }
        public virtual void Default(int value) { }
        public virtual void Default(double value) { }
        public virtual void Range(int min, int max) { }
        public virtual void Range(double min, double max) { }
        public virtual void Step(double value) { }
        public virtual void Password() { }
        public virtual void Multiline() { }
        public virtual void ShowWhen(string key) { }
        public virtual void Options(string[] options) { }
        public virtual void OptionsPairs(IEnumerable<(string Value, string Display)> pairOptions) { }
        public virtual void WithPairValue(string valueKey) { }
        public virtual void DefaultByValue(string value) { }
        public virtual void DefaultIndex(int index) { }
        public virtual void WithCompeting(string[] options) { }
        public virtual void MaxSelected(int n) { }
        public virtual void DefaultIndices(int[] indices) { }
        public virtual void Refresh(Func<string[]> callback) { }
        public virtual void RefreshPairs(Func<IEnumerable<(string Value, string Display)>> callback) { }
        public virtual void Preset(string[] values) { }
        public virtual void ToggleDefault(bool value) { }
        public virtual void Color(string hex) { }
        public virtual void Text(string caption) { }
        public virtual void OnClick(Action<UiContext> callback) { }
        public virtual void WithPermanentOption(bool value) { }
        public virtual void WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) { }
        public virtual void OnPillAdded(Action<string, StackPanel, CallbackContext> build) { }
        public virtual void OnPillRemoved(Action<string, StackPanel, CallbackContext> build) { }
    }
}
