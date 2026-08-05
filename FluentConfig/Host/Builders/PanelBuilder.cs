using System;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Fluent builder for nested schema (visibility groups, pill item templates, repeatable rows).
    /// Never holds a WPF Panel — only emits <see cref="SchemaNode"/>s.
    /// </summary>
    public class PanelBuilder : ControlHostBuilder<PanelFluentWrapper>
    {
        internal PanelBuilder(FluentConfigSession session, SchemaNodeList nodes)
            : base(session, nodes) { }

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
        /// Connection-status indicator (see <see cref="SectionBuilder.ConnectionStatus"/>).
        /// </summary>
        public PanelBuilder ConnectionStatus(
            string label,
            string initialStatus,
            string buttonText = null,
            Action<UiContext> onClick = null)
        {
            AddConnectionStatus(label, initialStatus, buttonText, onClick);
            return this;
        }

        /// <summary>
        /// Nested visibility group. Use <c>inverted: true</c> (named) or <see cref="WithVisibilityWhenOff"/>.
        /// </summary>
        public PanelBuilder WithVisibility(string toggleKey, Action<PanelBuilder> build, bool inverted = false)
        {
            FlushPending();
            if (build == null) return this;

            var list = new SchemaNodeList();
            var pb = new PanelBuilder(Session, list);
            build(pb);
            pb.FlushPending();

            Nodes.Add(new GroupNode
            {
                Id = "vis_" + (toggleKey ?? "group"),
                Visibility = new VisibilityCondition
                {
                    SaveKey = toggleKey,
                    EqualsValue = true,
                    Inverted = inverted,
                },
                Children = list.ToList(),
            });
            return this;
        }

        public PanelBuilder WithVisibilityWhenOff(string toggleKey, Action<PanelBuilder> build)
            => WithVisibility(toggleKey, build, inverted: true);

        /// <summary>Adds a repeatable-rows control with a relative row schema.</summary>
        public PanelBuilder WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            FlushPending();
            if (buildRow == null) return this;

            var list = new SchemaNodeList();
            var pb = new PanelBuilder(Session, list);
            buildRow(pb);
            pb.FlushPending();

            Nodes.Add(new RepeatableRowsNode
            {
                Id = saveKey,
                SaveKey = saveKey,
                RowSchema = list.ToList(),
            });
            return this;
        }
    }
}
