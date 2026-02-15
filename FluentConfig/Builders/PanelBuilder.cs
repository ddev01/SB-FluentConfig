using System;
using System.Windows;
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

        internal PanelBuilder(FluentConfig ui, IAddStrategy strategy, string tabName)
            : base(new ControlBuilderCore(strategy, tabName ?? "", ui), strategy)
        {
            Ui = ui ?? throw new ArgumentNullException(nameof(ui));
            Panel = null;
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

        public PanelBuilder WithVisibility(string[] dependencyKeys, Func<IRenderContext, bool> predicate, Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null && dependencyKeys != null && dependencyKeys.Length > 0 && predicate != null)
                Ui.WithVisibility(dependencyKeys, predicate, TabName, false, build);
            return this;
        }

        public PanelBuilder WithVisibility(string[] dependencyKeys, Func<IRenderContext, bool> predicate, bool inverted, Action<PanelBuilder> build)
        {
            FlushPending();
            if (build != null && dependencyKeys != null && dependencyKeys.Length > 0 && predicate != null)
                Ui.WithVisibility(dependencyKeys, predicate, TabName, inverted, build);
            return this;
        }

        public PanelBuilder WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            FlushPending();
            if (buildRow != null)
                Ui.WithRepeatableRows(saveKey, TabName, buildRow);
            return this;
        }

        public PanelBuilder Grid(int cols, Action<PanelBuilder> build, int gap = 16, string justify = null, string align = null, double padding = 0)
        {
            var colWidths = new string[Math.Max(1, cols)];
            for (int i = 0; i < colWidths.Length; i++)
                colWidths[i] = "*";
            return Grid(colWidths, build, gap, justify, align, padding);
        }

        public PanelBuilder Grid(string[] colWidths, Action<PanelBuilder> build, int gap = 16, string justify = null, string align = null, double padding = 0)
        {
            FlushPending();
            if (build == null) return this;
            var currentPanel = Ui.GetTargetPanel(TabName);
            if (currentPanel == null) return this;
            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 0) };
            var cols = colWidths != null && colWidths.Length > 0 ? colWidths.Length : 1;
            for (int i = 0; i < cols; i++)
            {
                var w = colWidths != null && i < colWidths.Length ? colWidths[i] : "*";
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = LayoutAlignmentHelper.ParseColumnWidth(w) });
            }
            currentPanel.Children.Add(grid);
            var hAlign = LayoutAlignmentHelper.ParseJustify(justify, HorizontalAlignment.Stretch);
            var vAlign = LayoutAlignmentHelper.ParseAlign(align, VerticalAlignment.Stretch);
            var defPadding = padding > 0 ? new Thickness(padding) : (Thickness?)null;
            Ui.PushLayout(new GridLayoutState(grid, cols, gap, hAlign, vAlign, defPadding));
            try
            {
                var layoutPb = new PanelBuilder(Ui, new AddToLayoutStrategy(Ui, TabName), TabName);
                build(layoutPb);
                layoutPb.FlushPending();
            }
            finally
            {
                Ui.PopLayout();
            }
            return this;
        }

        public PanelBuilder Flex(Action<PanelBuilder> build, int gap = 16, bool wrap = false, string justify = null, string align = null, double padding = 0)
        {
            FlushPending();
            if (build == null) return this;
            var currentPanel = Ui.GetTargetPanel(TabName);
            if (currentPanel == null) return this;
            Panel flexPanel;
            if (wrap)
            {
                flexPanel = new WrapPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 8, 0, 0)
                };
            }
            else
            {
                flexPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 8, 0, 0)
                };
            }
            currentPanel.Children.Add(flexPanel);
            var hAlign = LayoutAlignmentHelper.ParseJustify(justify, HorizontalAlignment.Stretch);
            var vAlign = LayoutAlignmentHelper.ParseAlign(align, VerticalAlignment.Stretch);
            var defPadding = padding > 0 ? new Thickness(padding) : (Thickness?)null;
            Ui.PushLayout(new FlexLayoutState(flexPanel, gap, vAlign, hAlign, vAlign, defPadding));
            try
            {
                var layoutPb = new PanelBuilder(Ui, new AddToLayoutStrategy(Ui, TabName), TabName);
                build(layoutPb);
                layoutPb.FlushPending();
            }
            finally
            {
                Ui.PopLayout();
            }
            return this;
        }

        public PanelBuilder Div(Action<PanelBuilder> build)
        {
            FlushPending();
            if (build == null) return this;
            var container = new StackPanel { Orientation = Orientation.Vertical };
            var pb = new PanelBuilder(Ui, container, TabName);
            build(pb);
            pb.FlushPending();
            if (Ui.IsInLayout)
            {
                var opts = LayoutCellOptions.Default;
                opts.ColSpan = 1;
                Ui.PlaceLayoutChild(container, opts);
            }
            else
            {
                var currentPanel = Ui.GetTargetPanel(TabName);
                if (currentPanel != null)
                    currentPanel.Children.Add(container);
            }
            return this;
        }
    }
}
