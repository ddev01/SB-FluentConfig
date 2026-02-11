using System;
using Sbui.Elements;

namespace Sbui
{
    /// <summary>
    /// Abstract base for SectionBuilder and PanelBuilder. Shares _core, _pending, FlushPending,
    /// and common control factory pattern. Derived classes supply strategy and wrapper creation.
    /// </summary>
    public abstract class ControlHostBuilder
    {
        protected readonly ControlBuilderCore _core;
        protected readonly IAddStrategy _strategy;
        protected IFlushableControlBuilder _pending;

        protected ControlHostBuilder(ControlBuilderCore core, IAddStrategy strategy)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        /// <summary>Flush any pending control (auto-add). Called automatically when starting the next control.</summary>
        internal void FlushPending()
        {
            _pending?.Add();
            _pending = null;
        }

        internal IFlushableControlBuilder GetPending() => _pending;

        /// <summary>Section/panel description text (intro).</summary>
        protected void AddIntro(string text, string tabName)
        {
            FlushPending();
            var el = new DescriptionElement(text ?? "", tabName);
            _strategy.Add(el, null);
        }

        /// <summary>Section/panel title.</summary>
        protected void AddTitle(string text, string tabName)
        {
            FlushPending();
            var el = new TitleElement(text ?? "", tabName);
            _strategy.Add(el, null);
        }

        /// <summary>Horizontal separator.</summary>
        protected void AddSeparator(string tabName)
        {
            FlushPending();
            var el = new InlineSeparatorElement(tabName);
            _strategy.Add(el, null);
        }

        /// <summary>Common pattern: flush pending, create builder, set pending, return wrapper.</summary>
        protected TWrapper CreateControl<TWrapper>(IFlushableControlBuilder builder, Func<IFlushableControlBuilder, TWrapper> createWrapper)
        {
            FlushPending();
            _pending = builder;
            return createWrapper(builder);
        }
    }
}
