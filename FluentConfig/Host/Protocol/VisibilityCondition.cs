using Newtonsoft.Json;

namespace FluentConfig.Protocol
{
    /// <summary>
    /// Conditional display for a schema node (maps ShowWhen / WithVisibility).
    /// Visible when the value at <see cref="SaveKey"/> equals <see cref="EqualsValue"/>;
    /// if <see cref="Inverted"/> is true, visibility is negated.
    /// Default EqualsValue is <c>true</c> (toggle on), matching ShowWhen(key) semantics.
    /// </summary>
    public sealed class VisibilityCondition
    {
        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        /// <summary>Expected value on the wire as "equals"; typically <c>true</c> for toggle-gated visibility.</summary>
        [JsonProperty("equals")]
        public object EqualsValue { get; set; } = true;

        [JsonProperty("inverted")]
        public bool Inverted { get; set; }
    }
}
