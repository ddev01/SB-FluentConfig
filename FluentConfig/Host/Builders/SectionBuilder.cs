using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Fluent builder for a section (tab). Emits schema nodes instead of WPF elements.
    /// </summary>
    public class SectionBuilder : ControlHostBuilder<SectionBuilder>
    {
        private readonly string _sectionId;
        private readonly string _title;

        internal SectionBuilder(FluentConfigSession session, string sectionId, string title)
            : base(session, new SchemaNodeList())
        {
            _sectionId = sectionId ?? "";
            _title = title ?? "";
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
