using Newtonsoft.Json;

namespace FluentConfig.Protocol
{
    /// <summary>
    /// Optional per-node layout hint (Span / Size). Width/MinWidth/MaxWidth are
    /// host-resolved CSS values (e.g. <c>fit-content</c>, <c>50%</c>, <c>80px</c>).
    /// </summary>
    public sealed class LayoutHint
    {
        /// <summary>CSS grid-column span (integer columns).</summary>
        [JsonProperty("span")]
        public int? Span { get; set; }

        /// <summary>Resolved CSS <c>width</c> value.</summary>
        [JsonProperty("width")]
        public string Width { get; set; }

        /// <summary>Resolved CSS <c>min-width</c> value.</summary>
        [JsonProperty("minWidth")]
        public string MinWidth { get; set; }

        /// <summary>Resolved CSS <c>max-width</c> value.</summary>
        [JsonProperty("maxWidth")]
        public string MaxWidth { get; set; }

        /// <summary>Flex grow (Row only). <c>true</c> → 1, <c>false</c> → 0.</summary>
        [JsonProperty("grow")]
        public bool? Grow { get; set; }

        /// <summary>Flex shrink (Row only). <c>true</c> → 1, <c>false</c> → 0.</summary>
        [JsonProperty("shrink")]
        public bool? Shrink { get; set; }
    }
}
