using System;

namespace FluentConfig
{
    /// <summary>
    /// Context passed to PillInput callbacks. Provides GetValue / RemoveSettingsKeys — never UI-framework containers.
    /// </summary>
    public class CallbackContext
    {
        private readonly FluentConfigSession _session;

        internal CallbackContext(FluentConfigSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>Gets a persisted setting value by key.</summary>
        public T GetValue<T>(string key) => _session.GetValue<T>(key);

        /// <summary>Removes the given keys from settings (e.g. when a pill item is removed).</summary>
        public void RemoveSettingsKeys(params string[] keys) => _session.RemoveSettingsKeys(keys);
    }
}
