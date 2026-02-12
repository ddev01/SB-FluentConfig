using System;
using System.Windows.Controls;

namespace FluentConfig
{
    /// <summary>
    /// Context passed to PillInput callbacks (WithSectionsPanel, OnPillAdded, OnPillRemoved). Provides GetValue, RemoveSettingsKeys, WithPanel.
    /// </summary>
    public class CallbackContext
    {
        private readonly FluentConfig _config;

        internal CallbackContext(FluentConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Gets a persisted setting value by key.</summary>
        public T GetValue<T>(string key)
        {
            return _config.GetValue<T>(key);
        }

        /// <summary>Removes the given keys from settings (e.g. when an alias/pill is removed). Call before or after removing UI.</summary>
        public void RemoveSettingsKeys(params string[] keys)
        {
            _config.RemoveSettingsKeys(keys);
        }

        /// <summary>Builds content into the given panel using the fluent PanelBuilder. Use for per-alias or per-section content.</summary>
        public void WithPanel(Panel panel, string tabName, Action<PanelBuilder> build)
        {
            if (panel == null || build == null) return;
            _config.WithPanel(panel, () => build(new PanelBuilder(_config, panel, tabName ?? "")));
        }
    }
}
