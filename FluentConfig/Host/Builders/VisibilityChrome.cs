namespace FluentConfig
{
    /// <summary>
    /// Visual chrome for a visibility <c>group</c> node (left-rail vs flat).
    /// Value-equals and comparator groups default to <see cref="Flat"/>.
    /// </summary>
    public enum VisibilityChrome
    {
        /// <summary>No indent rail. Children align with ungated siblings.</summary>
        Flat = 0,
        /// <summary>Indented left-border stack (toggle-gate look).</summary>
        Indented = 1,
    }
}
