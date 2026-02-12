using System;
using Newtonsoft.Json.Linq;
using Streamer.bot.Plugin.Interface;

namespace FluentConfig.Core
{
    /// <summary>
    /// Owns loading/saving of JObject settings via CPH; no UI.
    /// </summary>
    public class SettingsManager
    {
        private readonly IInlineInvokeProxy _invokeProxy;
        private readonly string _settingsKey;
        private readonly Action<string> _log;
        private JObject _settings;

        public SettingsManager(IInlineInvokeProxy invokeProxy, string settingsKey, Action<string> log = null)
        {
            _invokeProxy = invokeProxy;
            _settingsKey = settingsKey ?? "";
            _log = log;
            _settings = new JObject();
        }

        /// <summary>
        /// Loads settings from CPH (when proxy and key are set); returns current settings JObject.
        /// </summary>
        public JObject Load()
        {
            if (_invokeProxy == null || string.IsNullOrEmpty(_settingsKey))
            {
                _settings = new JObject();
                return _settings;
            }
            try
            {
                string json = _invokeProxy.GetGlobalVar<string>(_settingsKey, true);
                if (!string.IsNullOrEmpty(json))
                    _settings = JObject.Parse(json);
                else
                    _settings = new JObject();
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[FluentConfig] SettingsManager.Load failed: {ex.Message}");
                _settings = new JObject();
            }
            return _settings;
        }

        /// <summary>
        /// Persists the given settings to CPH. Caller typically merges window size + form values.
        /// </summary>
        public void Save(JObject settings)
        {
            if (_invokeProxy == null || string.IsNullOrEmpty(_settingsKey) || settings == null)
                return;
            try
            {
                _invokeProxy.SetGlobalVar(_settingsKey, settings.ToString(), true);
                _settings = settings;
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[FluentConfig] SettingsManager.Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a typed value from the current settings; returns defaultValue if missing or invalid.
        /// </summary>
        public T GetValue<T>(string key, T defaultValue)
        {
            if (_settings == null || string.IsNullOrEmpty(key))
                return defaultValue;
            var token = _settings[key];
            if (token == null)
                return defaultValue;
            try
            {
                return token.ToObject<T>();
            }
            catch (Exception ex)
            {
                _log?.Invoke($"[FluentConfig] SettingsManager.GetValue failed for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Sets a value in the current in-memory settings. Call Save() to persist.
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            if (_settings == null)
                _settings = new JObject();
            if (string.IsNullOrEmpty(key))
                return;
            _settings[key] = JToken.FromObject(value);
        }

        /// <summary>
        /// Returns the current in-memory settings JObject (for merging window size, etc.).
        /// </summary>
        public JObject GetSettings()
        {
            return _settings ?? (_settings = new JObject());
        }
    }
}
