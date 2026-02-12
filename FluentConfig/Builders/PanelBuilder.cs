using System;
using System.Windows.Controls;
using FluentConfig.Elements;

namespace FluentConfig
{
    /// <summary>
    /// Fluent builder for adding controls to a specific panel (e.g. inside PillInput or WithVisibility callbacks).
    /// </summary>
    public class PanelBuilder : ControlHostBuilder<PanelFluentWrapper>
    {
        internal readonly FluentConfig Ui;
        internal readonly Panel Panel;
        internal readonly string TabName;

        internal PanelBuilder(FluentConfig ui, Panel panel, string tabName)
            : base(CreateCore(ui, panel, tabName), new AddToPanelStrategy(ui, panel, tabName ?? ""))
        {
            Ui = ui ?? throw new ArgumentNullException(nameof(ui));
            Panel = panel ?? throw new ArgumentNullException(nameof(panel));
            TabName = tabName ?? "";
        }

        private static ControlBuilderCore CreateCore(FluentConfig ui, Panel panel, string tabName)
        {
            var strategy = new AddToPanelStrategy(ui, panel, tabName ?? "");
            return new ControlBuilderCore(strategy, tabName ?? "", ui);
        }

        protected override PanelFluentWrapper WrapControl(IFlushableControlBuilder b) => new PanelFluentWrapper(this, b);

        public PanelBuilder Title(string text)
        {
            AddTitle(text, TabName);
            return this;
        }

        public PanelBuilder Intro(string text)
        {
            AddIntro(text, TabName);
            return this;
        }

        public PanelBuilder Separator()
        {
            AddSeparator(TabName);
            return this;
        }

        public PanelBuilder WithVisibility(string toggleKey, bool inverted, Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null)
                Ui.WithVisibility(toggleKey, TabName, inverted, build);
            return this;
        }

        public PanelBuilder WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            FlushPending();
            if (buildRow != null)
                Ui.WithRepeatableRows(saveKey, TabName, buildRow);
            return this;
        }
    }
}
