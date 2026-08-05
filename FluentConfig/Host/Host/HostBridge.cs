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
            var tracer = _session.PerfTracer;
            tracer.BeginPhase("WebView.NavigationCompleted");
            var doc = _window.TakeBootstrapDocument();
            if (doc != null)
            {
                tracer.BeginPhase("Bridge.BootstrapSent");
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
        /// Host→web request without blocking the UI thread. <paramref name="onCompleted"/> runs on
        /// the window dispatcher with the result (or null on error / timeout / detach).
        /// Preferred for native close — WebView2 may not deliver replies during DispatcherFrame waits.
        /// </summary>
        public void SendRequest(string method, object paramsObj, Action<JToken> onCompleted, int timeoutMs = 60000)
        {
            if (onCompleted == null) throw new ArgumentNullException(nameof(onCompleted));

            var id = Interlocked.Increment(ref _nextId);
            var tcs = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[id] = tcs;
            Send(WireMessage.Request(id, method, paramsObj));

            var dispatcher = _window.Dispatcher;
            var timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(Math.Max(1000, timeoutMs)),
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (_pending.TryRemove(id, out var pending))
                    pending.TrySetResult(null);
            };
            timer.Start();

            tcs.Task.ContinueWith(_ =>
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    timer.Stop();
                    JToken result = null;
                    try
                    {
                        if (tcs.Task.Status == TaskStatus.RanToCompletion)
                            result = tcs.Task.Result;
                    }
                    catch
                    {
                        result = null;
                    }
                    onCompleted(result);
                }));
            });
        }

        /// <summary>
        /// Sends a host→web request and blocks (via DispatcherFrame) until the web replies.
        /// Must be called on the UI thread (DispatcherFrame requires it). Prefer
        /// <see cref="SendRequest"/> when the reply must arrive via WebMessageReceived on this thread.
        /// </summary>
        public JToken SendRequestAndWait(string method, object paramsObj, int timeoutMs = 60000)
        {
            if (!_window.Dispatcher.CheckAccess())
                throw new InvalidOperationException(
                    "SendRequestAndWait must be called on the UI thread.");

            var id = Interlocked.Increment(ref _nextId);
            var tcs = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);
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
            // Unblock the frame as soon as the reply arrives (don't wait for the next 50ms tick).
            tcs.Task.ContinueWith(_ =>
            {
                timer.Stop();
                frame.Continue = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
            timer.Start();
            Dispatcher.PushFrame(frame);

            _pending.TryRemove(id, out _);
            if (tcs.Task.Status == TaskStatus.RanToCompletion)
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
