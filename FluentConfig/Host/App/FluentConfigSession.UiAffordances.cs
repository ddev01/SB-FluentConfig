using System;
using System.Threading;
using System.Windows;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;

namespace FluentConfig
{
    /// <summary>UiContext affordances: toast, dialogs, progress, logging.</summary>
    public sealed partial class FluentConfigSession
    {
        internal void Toast(string message)
        {
            _bridge?.SendRequestFireAndForget(RpcMethods.Toast, new ToastParams { Message = message });
        }

        internal void Popup(string title, string message)
        {
            _bridge?.SendRequestFireAndForget(RpcMethods.DialogPopup, new PopupParams { Title = title, Message = message });
        }

        internal bool ShowConfirmDialog(string title, string message, string yesButton, string noButton)
        {
            if (_bridge == null)
                return MessageBox.Show(message, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes;

            var resultJson = _bridge.SendRequestAndWait(RpcMethods.DialogConfirm, new ConfirmParams
            {
                Title = title,
                Message = message,
                ConfirmText = yesButton,
                CancelText = noButton,
            });

            try
            {
                var parsed = resultJson?.ToObject<ConfirmResult>(ProtocolJson.CreateSerializer());
                return parsed != null && parsed.Confirmed;
            }
            catch
            {
                return false;
            }
        }

        internal IProgressReporter ShowProgressWindow(string title, string message, string progressLabel, int total)
        {
            var id = "progress-" + Interlocked.Increment(ref _nextRequestId);
            return new BridgeProgressReporter(_bridge, _window?.Dispatcher, id, title, message, total);
        }

        internal void Log(string message) => FluentConfigApp.LogInternal(message);

        internal long NextRequestId() => Interlocked.Increment(ref _nextRequestId);
    }

    internal sealed class BridgeProgressReporter : IProgressReporter
    {
        private readonly HostBridge _bridge;
        private readonly System.Windows.Threading.Dispatcher _dispatcher;
        private readonly string _id;
        private readonly string _title;
        private readonly int _total;
        private string _message;

        public BridgeProgressReporter(
            HostBridge bridge,
            System.Windows.Threading.Dispatcher dispatcher,
            string id,
            string title,
            string message,
            int total)
        {
            _bridge = bridge;
            _dispatcher = dispatcher;
            _id = id;
            _title = title;
            _message = message;
            _total = total;
            Push(0, false);
        }

        public void Report(int current)
        {
            Push(current, false);
        }

        public void Close()
        {
            Push(_total, true);
        }

        private void Push(int current, bool done)
        {
            if (_bridge == null) return;

            // Example OnClick handlers often Report from Task.Run â€” marshal to the UI
            // dispatcher so WebView2 posts aren't racing a busy/non-UI thread.
            if (_dispatcher != null && !_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(new Action(() => Push(current, done)));
                return;
            }

            double? percent = _total > 0 ? (100.0 * current / _total) : (double?)null;
            _bridge.Send(WireMessage.Push(PushEventNames.Progress, new ProgressPayload
            {
                Id = _id,
                Title = _title,
                Message = _message,
                Current = current,
                Total = _total,
                Percent = percent,
                Done = done,
            }));
        }
    }
}
