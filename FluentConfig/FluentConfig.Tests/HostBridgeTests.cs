using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    [Collection("StaWpf")]
    public class HostBridgeTests
    {
        [Fact]
        public void SendRequestAndWait_OffUiThread_ThrowsInvalidOperationException()
        {
            Exception caught = null;
            Exception setupError = null;

            var uiThread = new Thread(() =>
            {
                HostBridge bridge = null;
                FluentConfigHostWindow window = null;
                try
                {
                    try
                    {
                        if (Application.Current == null)
                            new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    }
                    catch (InvalidOperationException)
                    {
                        // One Application per AppDomain.
                    }

                    var cph = TestCph.CreateMockCph();
                    var session = new FluentConfigSession(cph.Object, "Bridge Thread Guard", "1.0");
                    window = new FluentConfigHostWindow("Bridge Thread Guard", "1.0");
                    bridge = new HostBridge(window, session);

                    var worker = new Thread(() =>
                    {
                        try
                        {
                            bridge.SendRequestAndWait(RpcMethods.WindowCloseRequested, new { });
                        }
                        catch (Exception ex)
                        {
                            caught = ex;
                        }
                    });
                    worker.SetApartmentState(ApartmentState.MTA);
                    worker.Start();
                    worker.Join();
                }
                catch (Exception ex)
                {
                    setupError = ex;
                }
                finally
                {
                    try
                    {
                        bridge?.Detach();
                        window?.DisposeWebViewCore();
                        window?.Close();
                        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                    }
                    catch { /* ignore */ }
                }
            });
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            uiThread.Join();

            if (setupError != null)
                throw setupError;

            Assert.NotNull(caught);
            var invalid = Assert.IsType<InvalidOperationException>(caught);
            Assert.Contains("UI thread", invalid.Message);
        }

        [Fact]
        public void OnWebMessage_SetsWebAlive()
        {
            RunSta((bridge, _) =>
            {
                Assert.False(bridge.WebAlive);
                bridge.DeliverWebMessageForTests("{}");
                Assert.True(bridge.WebAlive);
            });
        }

        [Fact]
        public void SendRequest_Timeout_CompletesWithNull()
        {
            RunSta((bridge, dispatcher) =>
            {
                JToken result = new JObject();
                var frame = new DispatcherFrame();
                var safety = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
                {
                    Interval = TimeSpan.FromSeconds(8),
                };
                safety.Tick += (s, e) =>
                {
                    safety.Stop();
                    frame.Continue = false;
                };
                safety.Start();

                bridge.SendRequest(RpcMethods.WindowCloseRequested, new { }, token =>
                {
                    result = token;
                    frame.Continue = false;
                }, timeoutMs: 1000);

                Dispatcher.PushFrame(frame);
                safety.Stop();
                Assert.Null(result);
            });
        }

        [Fact]
        public void Detach_CompletesPendingRequestsWithNull()
        {
            RunSta((bridge, dispatcher) =>
            {
                JToken result = new JObject();
                var frame = new DispatcherFrame();
                var safety = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
                {
                    Interval = TimeSpan.FromSeconds(5),
                };
                safety.Tick += (s, e) =>
                {
                    safety.Stop();
                    frame.Continue = false;
                };
                safety.Start();

                bridge.SendRequest(RpcMethods.WindowCloseRequested, new { }, token =>
                {
                    result = token;
                    frame.Continue = false;
                }, timeoutMs: 60000);

                bridge.Detach();
                Dispatcher.PushFrame(frame);
                safety.Stop();
                Assert.Null(result);
            });
        }

        private static void RunSta(Action<HostBridge, Dispatcher> body)
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                HostBridge bridge = null;
                FluentConfigHostWindow window = null;
                try
                {
                    try
                    {
                        if (Application.Current == null)
                            new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    }
                    catch (InvalidOperationException)
                    {
                        // One Application per AppDomain.
                    }

                    var cph = TestCph.CreateMockCph();
                    var session = new FluentConfigSession(cph.Object, "Bridge Coverage", "1.0");
                    window = new FluentConfigHostWindow("Bridge Coverage", "1.0");
                    bridge = new HostBridge(window, session);
                    body(bridge, Dispatcher.CurrentDispatcher);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    try
                    {
                        bridge?.Detach();
                        window?.DisposeWebViewCore();
                        window?.Close();
                        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                    }
                    catch { /* ignore */ }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (error != null)
                throw error;
        }
    }
}
