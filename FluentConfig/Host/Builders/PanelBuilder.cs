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
            SchemaBuilderHelpers.AddVisibilityGroup(Session, Nodes, FlushPending, toggleKey, build, inverted);
            return this;
        }

        public PanelBuilder WithVisibilityWhenOff(string toggleKey, Action<PanelBuilder> build)
            => WithVisibility(toggleKey, build, inverted: true);

        /// <summary>Adds a repeatable-rows control with a relative row schema.</summary>
        public PanelBuilder WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            SchemaBuilderHelpers.AddRepeatableRows(Session, Nodes, FlushPending, saveKey, buildRow);
            return this;
        }
    }
}
