namespace FluentConfig
{
    /// <summary>
    /// Numeric comparator for <see cref="IControlOptions.ShowWhen"/> / WithVisibility gates.
    /// Maps to wire operators <c>gte</c>|<c>lte</c>|<c>gt</c>|<c>lt</c>.
    /// </summary>
    public enum Comparator
    {
        GreaterOrEqual,
        LessOrEqual,
        GreaterThan,
        LessThan,
    }
}
