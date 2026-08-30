using FluentConfig.Core;
using FluentConfig.Protocol;
using FluentConfig.Runtime;
using Newtonsoft.Json.Linq;

namespace FluentConfig
{
    /// <summary>Value accessors, settings mutation, and live schema/value patches.</summary>
    public sealed partial class FluentConfigSession
    {
        // ── Values / settings ──
        internal T GetValue<T>(string key) => _settingsManager.GetValue(key, default(T));
        internal void RemoveSettingsKeys(params string[] keys)
        {
            _settingsManager.RemoveKeys(keys);
            _settingsManager.Save(_settingsManager.GetSettings());
            _latestValues = _settingsManager.GetSettings();
        }

        internal T GetPendingValue<T>(string key, JObject snapshot)
        {
            var source = snapshot ?? _latestValues ?? _settingsManager.GetSettings();
            if (source == null || string.IsNullOrEmpty(key))
                return default;
            var token = SettingsPathHelper.GetNestedValue(source, key) ?? source[key];
            if (token == null || token.Type == JTokenType.Null)
                return default;
            try
            {
                return token.ToObject<T>();
            }
            catch
            {
                return default;
            }
        }

        internal void PushSchemaPatch(string sectionId, string nodeId, SchemaNode node)
        {
            _bridge?.Send(WireMessage.Push(PushEventNames.SchemaPatch, new SchemaPatchPayload { SectionId = sectionId, NodeId = nodeId, Node = node, }));
        }

        internal void PatchLiveValue<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                return;
            _settingsManager.SetValue(key, value);
            _settingsManager.Save(_settingsManager.GetSettings());
            _latestValues = _settingsManager.GetSettings();
            var token = value == null ? JValue.CreateNull() : JToken.FromObject(value);
            _bridge?.Send(WireMessage.Push(PushEventNames.ValuesPatch, new ValuesPatchPayload { Paths = new JObject { [key] = token }, }));
        }

        internal void LogExistingSettings()
        {
            // Load from CPH — in-memory may still be empty when called from the build delegate
            // before Show() (common pattern: .LogExistingSettings() inside ShowOrFocus).
            var settings = _settingsManager.Load();
            _latestValues = settings ?? new JObject();
            var redacted = SettingsRedaction.RedactObject(settings);
            Log($"[FluentConfig] Existing settings for '{_title}': {redacted}");
        }

        /// <summary>Exposes in-memory settings for tests after save RPCs.</summary>
        internal JObject GetSettingsForTests() => _settingsManager.GetSettings();
    }
}
