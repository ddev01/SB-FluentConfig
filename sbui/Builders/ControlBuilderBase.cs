using System;

namespace Sbui
{
    /// <summary>
    /// Shared base for control builders. Holds the common fields (strategy, tab, label, key, hint, showWhen)
    /// and default overrides for Hint/ShowWhen so concrete builders only add their specific logic.
    /// </summary>
    public abstract class ControlBuilderBase : ControlOptionsBase, IFlushableControlBuilder
    {
        protected readonly IAddStrategy Strategy;
        protected readonly string TabName;
        internal readonly string Label;
        internal readonly string Key;
        protected string HintText;
        protected string ShowWhenKey;

        protected ControlBuilderBase(IAddStrategy strategy, string tabName, string label, string key)
        {
            Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            TabName = tabName ?? "";
            Label = label ?? "";
            Key = key ?? "";
        }

        public override void Hint(string text) { HintText = text; }
        public override void ShowWhen(string key) { ShowWhenKey = key; }

        public abstract void Add();
    }
}
