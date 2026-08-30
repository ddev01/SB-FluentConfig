namespace FluentConfig
{
    /// <summary>
    /// Fluent builder for nested schema (visibility groups, pill item templates, repeatable rows).
    /// Never holds a WPF Panel - only emits schema nodes.
    /// </summary>
    public class PanelBuilder : ControlHostBuilder<PanelBuilder>
    {
        internal PanelBuilder(FluentConfigSession session, SchemaNodeList nodes) : base(session, nodes)
        {
        }
    }
}
