using System;
using System.Collections.Generic;
using System.Linq;
using FluentConfig.Core;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;

namespace FluentConfig
{
    /// <summary>Schema document build and pill template expansion.</summary>
    public sealed partial class FluentConfigSession
    {
        /// <summary>
        /// Loads settings and builds the UiDocument without opening a window (tests / harnesses).
        /// </summary>
        internal UiDocument BuildDocumentForTests()
        {
            var settings = _settingsManager.Load();
            _latestValues = settings ?? new JObject();
            var prefs = WindowPrefsStore.Load(_cph, ScopeKey);
            _dontRemindDiscard = prefs?.DontRemindDiscard ?? false;
            return BuildDocument();
        }

        private UiDocument BuildDocument()
        {
            _filepathRules.Clear();
            var sections = _deferredSections.Select(f => f()).ToList();
            SchemaValidation.AssertUniqueSaveKeys(sections);
            // Update notices are pushed via update.available after a deferred HTTP check
            // (never injected here — that would block Show on network I/O).
            ExpandPillItems(sections, _latestValues);
            // First menu open writes schema defaults for missing keys only (never overwrites
            // user values). Runtime actions can then use CPH.GetGlobalVar on the settings blob.
            var defaults = SettingsDefaultsCollector.Collect(sections, _latestValues);
            if (SettingsSync.SeedMissingDefaults(_settingsManager, defaults, persist: true) > 0)
                _latestValues = _settingsManager.GetSettings() ?? new JObject();
            return new UiDocument
            {
                Title = _title,
                Version = _version,
                // Only dark is supported today (no .ColorScheme builder).
                ColorScheme = "dark",
                Sections = sections,
                Values = _latestValues,
                FrameworkVersion = FrameworkVersion,
                RepoUrl = _repoUrl,
                DontRemindDiscard = _dontRemindDiscard,
            };
        }

        private void ExpandPillItems(IList<SectionSchema> sections, JObject values)
        {
            if (sections == null)
                return;
            foreach (var section in sections)
                ExpandPillNodes(section.Children, values);
        }

        private void ExpandPillNodes(IList<SchemaNode> nodes, JObject values)
        {
            if (nodes == null)
                return;
            foreach (var node in nodes)
            {
                if (node is PillInputNode pill && !string.IsNullOrEmpty(pill.SaveKey))
                {
                    var names = values?[pill.SaveKey] as JArray;
                    if (names != null && pill.ItemTemplate != null)
                    {
                        try
                        {
                            var templateJson = ProtocolJson.Serialize(pill.ItemTemplate);
                            pill.Items = names.Select(t => t?.ToString()).Where(n => !string.IsNullOrEmpty(n)).Select(n => new PillItemSchema { Name = n, Children = ExpandTemplateJson(templateJson, n), }).ToList();
                        }
                        catch (Exception ex)
                        {
                            Log($"[FluentConfig] pill expand failed: {ex.Message}");
                        }
                    }
                }
                else if (node is GroupNode group)
                {
                    ExpandPillNodes(group.Children, values);
                }
            }
        }

        /// <summary>
        /// Substitute <c>{name}</c> only inside leaf string values of the schema tree
        /// (avoids corrupting JSON when the pill name contains quotes/backslashes).
        /// </summary>
        private static IList<SchemaNode> ExpandTemplate(IList<SchemaNode> template, string name)
        {
            if (template == null)
                return new List<SchemaNode>();
            return ExpandTemplateJson(ProtocolJson.Serialize(template), name);
        }

        private static IList<SchemaNode> ExpandTemplateJson(string templateJson, string name)
        {
            var root = JToken.Parse(templateJson);
            ReplaceNamePlaceholders(root, name ?? "");
            // Use ProtocolJson.Serialize — not JToken.ToString(Formatting). Streamer.bot may load an
            // older Newtonsoft.Json without that overload (MissingMethodException at runtime).
            return ProtocolJson.Deserialize<List<SchemaNode>>(ProtocolJson.Serialize(root)) ?? new List<SchemaNode>();
        }

        private static void ReplaceNamePlaceholders(JToken token, string name)
        {
            if (token == null)
                return;
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in ((JObject)token).Properties())
                        ReplaceNamePlaceholders(prop.Value, name);
                    break;
                case JTokenType.Array:
                    foreach (var item in (JArray)token)
                        ReplaceNamePlaceholders(item, name);
                    break;
                case JTokenType.String:
                    var s = token.Value<string>();
                    if (s != null && s.IndexOf("{name}", StringComparison.Ordinal) >= 0)
                        ((JValue)token).Value = s.Replace("{name}", name);
                    break;
            }
        }
    }
}
