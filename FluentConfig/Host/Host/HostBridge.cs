using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;

namespace FluentConfig
{
    /// <summary>
    /// WebView2 postMessage bridge: bootstrap push, request/response correlation, event pushes.
    /// </summary>
    internal sealed class HostBridge
    {
        private readonly FluentConfigHostWindow _window;
        private readonly FluentConfigSession _session;
        private readonly ConcurrentDictionary<long, TaskCompletionSource<JToken>> _pending =
            new ConcurrentDictionary<long, TaskCompletionSource<JToken>>();
        private long _nextId = 1;

        public HostBridge(FluentConfigHostWindow window, FluentConfigSession session)
        {
            _window = window;
            _session = session;
            _window.WebMessageReceived += OnWebMessage;
            _window.NavigationCompleted += OnNavigationCompleted;
        }

        /// <summary>
        /// Unsubscribe window events and cancel in-flight request waits before WebView2 dispose.
        /// </summary>
        public void Detach()
        {
            _window.WebMessageReceived -= OnWebMessage;
            _window.NavigationCompleted -= OnNavigationCompleted;

            foreach (var pair in _pending)
            {
                if (_pending.TryRemove(pair.Key, out var tcs))
                    tcs.TrySetResult(null);
            }
        }

        public void Start(UiDocument document)
        {
            _window.SetBootstrapDocument(document);
            _window.BeginNavigate();
        }

        private void OnNavigationCompleted()
        {
            var doc = _window.TakeBootstrapDocument();
            if (doc != null)
            {
                Send(WireMessage.Push(PushEventNames.Bootstrap, doc));
                _session.OnWebReady();
            }
        }

        private void OnWebMessage(string json) => _session.HandleWebMessage(json);

        public void Send(WireMessage message)
        {
            if (message == null) return;
            var json = ProtocolJson.Serialize(message);
            _window.PostWebMessage(json);
        }

        public void SendRequestFireAndForget(string method, object paramsObj)
        {
            var id = Interlocked.Increment(ref _nextId);
            Send(WireMessage.Request(id, method, paramsObj));
        }

        /// <summary>
        /// Sends a host→web request and blocks (via DispatcherFrame) until the web replies.
        /// </summary>
        public JToken SendRequestAndWait(string method, object paramsObj, int timeoutMs = 60000)
        {
            var id = Interlocked.Increment(ref _nextId);
            var tcs = new TaskCompletionSource<JToken>();
            _pending[id] = tcs;
            Send(WireMessage.Request(id, method, paramsObj));

            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            timer.Tick += (s, e) =>
            {
                if (tcs.Task.IsCompleted || sw.ElapsedMilliseconds > timeoutMs)
                {
                    timer.Stop();
                    frame.Continue = false;
                }
            };
            timer.Start();
            Dispatcher.PushFrame(frame);

            _pending.TryRemove(id, out _);
            if (tcs.Task.IsCompleted)
                return tcs.Task.Result;
            return null;
        }

        public void CompletePendingResponse(WireMessage msg)
        {
            if (msg.Id == null) return;
            if (_pending.TryRemove(msg.Id.Value, out var tcs))
            {
                if (msg.Error != null)
                    tcs.TrySetResult(null);
                else
                    tcs.TrySetResult(msg.Result);
            }
        }
    }
}
