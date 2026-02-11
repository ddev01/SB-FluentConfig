using System;
using System.Windows.Controls;
using Sbui.Elements;

namespace Sbui
{
    /// <summary>
    /// Builder for a display-only image. No save key. Use .Image(url) or .Image(url, maxHeight).
    /// </summary>
    public class ImageBuilder : IFlushableControlBuilder, IControlOptions
    {
        private readonly IAddStrategy _strategy;
        private readonly string _tabName;
        private readonly string _imageUrl;
        private readonly double? _maxHeight;
        private string _showWhenKey;

        internal ImageBuilder(IAddStrategy strategy, string tabName, string imageUrl, double? maxHeight = null)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _tabName = tabName ?? "";
            _imageUrl = imageUrl ?? "";
            _maxHeight = maxHeight;
        }

        void IControlOptions.Hint(string text) { }
        void IControlOptions.Default(bool value) { }
        void IControlOptions.Default(string value) { }
        void IControlOptions.Default(int value) { }
        void IControlOptions.Default(double value) { }
        void IControlOptions.Range(int min, int max) { }
        void IControlOptions.Range(double min, double max) { }
        void IControlOptions.Step(double value) { }
        void IControlOptions.Password() { }
        void IControlOptions.ShowWhen(string key) { _showWhenKey = key; }
        void IControlOptions.Options(string[] options) { }
        void IControlOptions.OptionsPairs(System.Collections.Generic.IList<(string display, string id)> pairs) { }
        void IControlOptions.DefaultIndex(int index) { }
        void IControlOptions.Refresh(Func<string[]> callback) { }
        void IControlOptions.RefreshPairs(Func<System.Collections.Generic.IList<(string display, string id)>> callback) { }
        void IControlOptions.WithPairValue(string displayKey, string idKey) { }
        void IControlOptions.Preset(string[] values) { }
        void IControlOptions.AllowDuplicates(bool value) { }
        void IControlOptions.ToggleDefault(bool value) { }
        void IControlOptions.Color(string hex) { }
        void IControlOptions.Text(string caption) { }
        void IControlOptions.OnClick(Action<UiContext> callback) { }
        void IControlOptions.InitialStatus(string status) { }
        void IControlOptions.OnLoadCheck(Action<UiContext> callback) { }
        void IControlOptions.WithPermanentOption(bool value) { }
        void IControlOptions.Units(string[] labels) { }
        void IControlOptions.AsTimePicker() { }
        void IControlOptions.Placeholder(string text) { }
        void IControlOptions.WithSectionsPanel(Action<StackPanel, Panel, CallbackContext> build) { }
        void IControlOptions.OnPillAdded(Action<string, StackPanel, CallbackContext> build) { }
        void IControlOptions.OnPillRemoved(Action<string, StackPanel, CallbackContext> build) { }
        void IControlOptions.Type(string value) { }

        public void Add()
        {
            var el = new ImageElement(_tabName, _imageUrl, _maxHeight, _showWhenKey);
            _strategy.Add(el, _showWhenKey);
        }
    }
}
