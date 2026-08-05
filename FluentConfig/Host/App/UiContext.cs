using System;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;

namespace FluentConfig
{
    /// <summary>
    /// Context passed to button OnClick callbacks. Provides Pending (live values), Toast, Popup, dialogs.
    /// </summary>
    public class UiContext
    {
        private readonly FluentConfigSession _session;
        private JObject _pendingValues;

        internal UiContext(FluentConfigSession session, JObject pendingValues = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _pendingValues = pendingValues;
        }

        /// <summary>Gets the current value of a control by saveKey (from the latest values snapshot).</summary>
        public T Pending<T>(string key)
        {
            return _session.GetPendingValue<T>(key, _pendingValues);
        }

        public void Toast(string message) => _session.Toast(message);

        public void Popup(string title, string message) => _session.Popup(title, message);

        /// <summary>
        /// Shows a confirm dialog via the web UI (dialog.confirm RPC). Returns true if confirmed.
        /// </summary>
        public bool ShowConfirmDialog(string title, string message, string yesButton, string noButton)
        {
            return _session.ShowConfirmDialog(title, message, yesButton, noButton);
        }

        public IProgressReporter ShowProgressWindow(string title, string message, string progressLabel, int total)
        {
            return _session.ShowProgressWindow(title, message, progressLabel, total);
        }

        /// <summary>
        /// Push a live schema.patch (e.g. update a <see cref="ConnectionStatusNode"/> after reconnect).
        /// </summary>
        public void PatchSchemaNode(string sectionId, string nodeId, SchemaNode node)
            => _session.PushSchemaPatch(sectionId, nodeId, node);

        public void Log(string message) => _session.Log(message);
    }
}
