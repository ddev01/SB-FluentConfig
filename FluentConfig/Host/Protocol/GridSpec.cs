using Newtonsoft.Json;

namespace FluentConfig.Protocol
{
    /// <summary>
    /// Layout container spec on a <see cref="GroupNode"/> (Grid / Row).
    /// Renders as CSS grid or flex; never literal Tailwind class strings on the wire.
    /// </summary>
    public sealed class GridSpec
    {
        /// <summary><c>grid</c> (CSS grid) or <c>row</c> (flex wrap). Default <c>grid</c>.</summary>
        [JsonProperty("mode")]
        public string Mode { get; set; } = "grid";

        /// <summary>Number of equal columns when <see cref="Mode"/> is <c>grid</c>.</summary>
        [JsonProperty("columns")]
        public int? Columns { get; set; }

        /// <summary>Gap on the Tailwind spacing scale (<c>gap * 4</c>px). Default 3.</summary>
        [JsonProperty("gap")]
        public int Gap { get; set; } = 3;

        /// <summary>
        /// CSS <c>align-items</c>: <c>start</c>|<c>center</c>|<c>end</c>|<c>stretch</c>|<c>baseline</c>.
        /// Null = browser default.
        /// </summary>
        [JsonProperty("align")]
        public string Align { get; set; }
    }
}
