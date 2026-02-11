namespace Sbui
{
    /// <summary>
    /// Implemented by control builders so SectionBuilder/PanelBuilder can auto-flush (call Add()) when the next control or section content is added.
    /// </summary>
    public interface IFlushableControlBuilder
    {
        void Add();
    }
}
