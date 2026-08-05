using System;
using System.Collections.Generic;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Fluent builder for a section (tab). Emits schema nodes instead of WPF elements.
    /// </summary>
    public class SectionBuilder : ControlHostBuilder<SectionFluentWrapper>
    {
        private readonly string _sectionId;
        private readonly string _title;

        internal SectionBuilder(FluentConfigSession session, string sectionId, string title)
            : base(session, new SchemaNodeList())
        {
            _sectionId = sectionId ?? "";
            _title = title ?? "";
        }

        protected override SectionFluentWrapper Wrap() => new SectionFluentWrapper(this, Pending);

        public SectionBuilder Intro(string text)
        {
            AddIntro(text);
            return this;
        }

        public SectionBuilder Title(string text)
        {
            AddTitle(text);
            return this;
        }

        public SectionBuilder Separator()
        {
            AddSeparator();
            return this;
        }

        /// <summary>
        /// Connection-status indicator. Optional action button reuses the existing button.click RPC.
        /// Live updates: push a replacement node via <see cref="UiContext.PatchSchemaNode"/>.
        /// </summary>
        /// <param name="label">Control label.</param>
        /// <param name="initialStatus">"connected" | "disconnected" | "connecting" | "error"</param>
        /// <param name="buttonText">Optional action button label.</param>
        /// <param name="onClick">Optional click handler when <paramref name="buttonText"/> is set.</param>
        public SectionBuilder ConnectionStatus(
            string label,
            string initialStatus,
            string buttonText = null,
            Action<UiContext> onClick = null)
        {
            AddConnectionStatus(label, initialStatus, buttonText, onClick);
            return this;
        }

        /// <summary>
        /// Adds a group visible when the toggle is on. Pass <c>inverted: true</c> to show when off.
        /// (inverted moved to a trailing named parameter — avoids the old positional-bool smell.)
        /// </summary>
        public SectionBuilder WithVisibility(string toggleKey, Action<PanelBuilder> build, bool inverted = false)
        {
            SchemaBuilderHelpers.AddVisibilityGroup(Session, Nodes, FlushPending, toggleKey, build, inverted);
            return this;
        }

        /// <summary>Adds a group visible only when the toggle is OFF.</summary>
        public SectionBuilder WithVisibilityWhenOff(string toggleKey, Action<PanelBuilder> build)
            => WithVisibility(toggleKey, build, inverted: true);

        /// <summary>Adds a repeatable-rows control with a relative row schema.</summary>
        public SectionBuilder WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            SchemaBuilderHelpers.AddRepeatableRows(Session, Nodes, FlushPending, saveKey, buildRow);
            return this;
        }

        internal SectionSchema BuildSection()
        {
            FlushPending();
            return new SectionSchema
            {
                Id = _sectionId,
                Title = _title,
                Children = Nodes.ToList(),
            };
        }
    }
}
