using System;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Fluent builder for nested schema (visibility groups, pill item templates, repeatable rows).
    /// Never holds a WPF Panel - only emits <see cref = "SchemaNode"/>s.
    /// </summary>
    public class PanelBuilder : ControlHostBuilder<PanelFluentWrapper>
    {
        internal PanelBuilder(FluentConfigSession session, SchemaNodeList nodes) : base(session, nodes)
        {
        }

        protected override PanelFluentWrapper Wrap() => new PanelFluentWrapper(this, Pending);
        public PanelBuilder Title(string text)
        {
            AddTitle(text);
            return this;
        }

        public PanelBuilder Intro(string text)
        {
            AddIntro(text);
            return this;
        }

        public PanelBuilder Separator()
        {
            AddSeparator();
            return this;
        }

        /// <summary>
        /// Connection-status indicator (see <see cref = "SectionBuilder.ConnectionStatus"/>).
        /// </summary>
        public PanelBuilder ConnectionStatus(string label, string initialStatus, string buttonText = null, Action<UiContext> onClick = null)
        {
            AddConnectionStatus(label, initialStatus, buttonText, onClick);
            return this;
        }

        /// <summary>
        /// Nested visibility group. Use <c>inverted: true</c> (named) or <see cref="WithVisibilityWhenOff"/>.
        /// </summary>
        public PanelBuilder WithVisibility(string toggleKey, Action<PanelBuilder> build, bool inverted = false)
        {
            SchemaBuilderHelpers.AddVisibilityGroup(Session, Nodes, FlushPending, toggleKey, build, inverted);
            return this;
        }

        /// <summary>Nested group gated by a numeric comparator against a literal value.</summary>
        public PanelBuilder WithVisibility(string key, Comparator op, int value, Action<PanelBuilder> build)
        {
            SchemaBuilderHelpers.AddVisibilityGroup(
                Session,
                Nodes,
                FlushPending,
                new VisibilityCondition
                {
                    SaveKey = key,
                    EqualsValue = null,
                    Operator = LayoutTokens.OperatorString(op),
                    Value = value,
                },
                "vis_" + (key ?? "group") + "_" + LayoutTokens.OperatorString(op) + "_" + value,
                build,
                indented: false);
            return this;
        }

        /// <summary>Nested group gated by a numeric comparator against another live saveKey.</summary>
        public PanelBuilder WithVisibility(string key, Comparator op, string compareKey, Action<PanelBuilder> build)
        {
            SchemaBuilderHelpers.AddVisibilityGroup(
                Session,
                Nodes,
                FlushPending,
                new VisibilityCondition
                {
                    SaveKey = key,
                    EqualsValue = null,
                    Operator = LayoutTokens.OperatorString(op),
                    CompareKey = compareKey,
                },
                "vis_" + (key ?? "group") + "_" + LayoutTokens.OperatorString(op) + "_" + (compareKey ?? "key"),
                build,
                indented: false);
            return this;
        }

        public PanelBuilder WithVisibilityWhenOff(string toggleKey, Action<PanelBuilder> build) => WithVisibility(toggleKey, build, inverted: true);

        public PanelBuilder WithVisibility(
            string key,
            string equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat)
        {
            SchemaBuilderHelpers.AddEqualsVisibilityGroup(Session, Nodes, FlushPending, key, equalsValue, inverted: false, build, chrome);
            return this;
        }

        public PanelBuilder WithVisibility(
            string key,
            int equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat)
        {
            SchemaBuilderHelpers.AddEqualsVisibilityGroup(Session, Nodes, FlushPending, key, equalsValue, inverted: false, build, chrome);
            return this;
        }

        public PanelBuilder WithVisibilityWhenNot(
            string key,
            string equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat)
        {
            SchemaBuilderHelpers.AddEqualsVisibilityGroup(Session, Nodes, FlushPending, key, equalsValue, inverted: true, build, chrome);
            return this;
        }

        public PanelBuilder WithVisibilityWhenNot(
            string key,
            int equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat)
        {
            SchemaBuilderHelpers.AddEqualsVisibilityGroup(Session, Nodes, FlushPending, key, equalsValue, inverted: true, build, chrome);
            return this;
        }

        /// <summary>Adds a repeatable-rows control with a relative row schema.</summary>
        public PanelBuilder WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            SchemaBuilderHelpers.AddRepeatableRows(Session, Nodes, FlushPending, saveKey, buildRow);
            return this;
        }

        /// <summary>CSS grid container. Spec tokens: <c>grid-cols-N</c>, <c>gap-N</c> (space-separated).</summary>
        public PanelBuilder Grid(string spec, Action<PanelBuilder> build)
        {
            SchemaBuilderHelpers.AddLayoutGroup(Session, Nodes, FlushPending, LayoutTokens.ParseGridSpec(spec), build);
            return this;
        }

        /// <summary>Flex-wrap row container (default gap 3).</summary>
        public PanelBuilder Row(Action<PanelBuilder> build) => Row("", build);

        /// <summary>Flex-wrap row. Spec tokens: <c>gap-N</c>, <c>items-start|center|end|stretch|baseline</c>.</summary>
        public PanelBuilder Row(string spec, Action<PanelBuilder> build)
        {
            SchemaBuilderHelpers.AddLayoutGroup(Session, Nodes, FlushPending, LayoutTokens.ParseRowSpec(spec), build);
            return this;
        }

        /// <summary>
        /// Materializes controls for indices <paramref name="startIndex"/>..<paramref name="max"/>,
        /// gating indices above the driver's declared min with <c>ShowWhen(driver, gte, i)</c>.
        /// </summary>
        public PanelBuilder RepeatFor(string driverKey, Action<PanelBuilder, int> build, int? max = null, int startIndex = 1)
        {
            SchemaBuilderHelpers.AddRepeatFor(Session, Nodes, FlushPending, driverKey, build, max, startIndex);
            return this;
        }
    }
}