using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Protocol
{
    /// <summary>
    /// Full UI document pushed at bootstrap (and optionally replaced later).
    /// Sections + current values — the web UI renders from this alone.
    /// </summary>
    public sealed class UiDocument
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>"light" | "dark" | "system" — host color-scheme hint for Tailwind dark: variant.</summary>
        [JsonProperty("colorScheme")]
        public string ColorScheme { get; set; }

        [JsonProperty("sections")]
        public IList<SectionSchema> Sections { get; set; }

        /// <summary>Current settings blob (nested JSON object). Paths match saveKey conventions.</summary>
        [JsonProperty("values")]
        public JObject Values { get; set; }

        /// <summary>FluentConfig framework version (host-injected, not author-configurable).</summary>
        [JsonProperty("frameworkVersion")]
        public string FrameworkVersion { get; set; }

        /// <summary>FluentConfig GitHub repo URL (host-injected, for footer branding).</summary>
        [JsonProperty("repoUrl")]
        public string RepoUrl { get; set; }

        /// <summary>
        /// When true, the web UI skips the discard-changes confirm on close
        /// (loaded from CPH global FluentConfig_Prefs_{title}).
        /// </summary>
        [JsonProperty("dontRemindDiscard")]
        public bool DontRemindDiscard { get; set; }
    }

    public sealed class SectionSchema
    {
        /// <summary>Stable section/tab id (e.g. "General").</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("children")]
        public IList<SchemaNode> Children { get; set; }
    }
}
