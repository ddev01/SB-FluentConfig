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
        /// Adds a group visible when the toggle is on. Pass <c>inverted: true</c> to show when off.
        /// (inverted moved to a trailing named parameter — avoids the old positional-bool smell.)
        /// </summary>
        public SectionBuilder WithVisibility(string toggleKey, Action<PanelBuilder> build, bool inverted = false)
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

        /// <summary>Adds a group visible only when the toggle is OFF.</summary>
        public SectionBuilder WithVisibilityWhenOff(string toggleKey, Action<PanelBuilder> build)
            => WithVisibility(toggleKey, build, inverted: true);

        /// <summary>Adds a repeatable-rows control with a relative row schema.</summary>
        public SectionBuilder WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
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
