using Newtonsoft.Json;

namespace FluentConfig.Protocol
{
    /// <summary>
    /// Conditional display for a schema node (maps ShowWhen / WithVisibility).
    /// Legacy: visible when the value at <see cref="SaveKey"/> equals <see cref="EqualsValue"/>
    /// (default <c>true</c>). When <see cref="Operator"/> is set, compares numerically against
    /// <see cref="Value"/> or the live value at <see cref="CompareKey"/> instead.
    /// If <see cref="Inverted"/> is true, visibility is negated.
    /// </summary>
    public sealed class VisibilityCondition
    {
        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        /// <summary>Expected value on the wire as "equals"; typically <c>true</c> for toggle-gated visibility. Null when using <see cref="Operator"/>.</summary>
        [JsonProperty("equals")]
        public object EqualsValue { get; set; } = true;

        [JsonProperty("inverted")]
        public bool Inverted { get; set; }

        /// <summary>Comparator operator: <c>gte</c>|<c>lte</c>|<c>gt</c>|<c>lt</c>. Null = legacy equals.</summary>
        [JsonProperty("operator")]
        public string Operator { get; set; }

        /// <summary>Literal numeric comparand when <see cref="Operator"/> is set. Mutually exclusive with <see cref="CompareKey"/>.</summary>
        [JsonProperty("value")]
        public double? Value { get; set; }

        /// <summary>Live saveKey whose value is the right-hand comparand. Mutually exclusive with <see cref="Value"/>.</summary>
        [JsonProperty("compareKey")]
        public string CompareKey { get; set; }
    }
}
