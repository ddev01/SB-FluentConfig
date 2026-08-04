using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Protocol
{
    /// <summary>Well-known push event names (host → web, or web → host where noted).</summary>
    public static class PushEventNames
    {
        /// <summary>Initial (or full replace) schema + values. Payload: <see cref="UiDocument"/>.</summary>
        public const string Bootstrap = "bootstrap";

        /// <summary>Progress window update. Payload: <see cref="ProgressPayload"/>.</summary>
        public const string Progress = "progress";

        /// <summary>Patch one or more live values without full bootstrap. Payload: <see cref="ValuesPatchPayload"/>.</summary>
        public const string ValuesPatch = "values.patch";

        /// <summary>
        /// Update-available notice (also expressible as an update-notice schema node in bootstrap).
        /// Payload: <see cref="UpdateAvailablePayload"/>.
        /// </summary>
        public const string UpdateAvailable = "update.available";

        /// <summary>Partial schema replace (e.g. after pill.changed). Payload: <see cref="SchemaPatchPayload"/>.</summary>
        public const string SchemaPatch = "schema.patch";
    }

    public sealed class ProgressPayload
    {
        /// <summary>Progress session id (matches ShowProgressWindow).</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>0–100, or absolute current when <see cref="Total"/> is set.</summary>
        [JsonProperty("percent")]
        public double? Percent { get; set; }

        [JsonProperty("current")]
        public int? Current { get; set; }

        [JsonProperty("total")]
        public int? Total { get; set; }

        [JsonProperty("done")]
        public bool Done { get; set; }
    }

    public sealed class ValuesPatchPayload
    {
        /// <summary>Sparse path→value map (e.g. "rate_value": 1.5, "rows[0].label": "x").</summary>
        [JsonProperty("paths")]
        public JObject Paths { get; set; }

        /// <summary>Optional deep-merge partial values object (alternative to Paths).</summary>
        [JsonProperty("values")]
        public JObject Values { get; set; }
    }

    public sealed class UpdateAvailablePayload
    {
        [JsonProperty("noticeId")]
        public string NoticeId { get; set; }

        [JsonProperty("currentVersion")]
        public string CurrentVersion { get; set; }

        [JsonProperty("latestVersion")]
        public string LatestVersion { get; set; }

        [JsonProperty("releaseNotes")]
        public string ReleaseNotes { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("repo")]
        public string Repo { get; set; }
    }

    public sealed class SchemaPatchPayload
    {
        /// <summary>Section id to patch, or null for document-level.</summary>
        [JsonProperty("sectionId")]
        public string SectionId { get; set; }

        /// <summary>Node id within the section (e.g. pill-input id) whose subtree is replaced.</summary>
        [JsonProperty("nodeId")]
        public string NodeId { get; set; }

        /// <summary>Replacement node, or for pill items a partial PillInputNode with updated items.</summary>
        [JsonProperty("node")]
        public SchemaNode Node { get; set; }
    }
}
