using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using FluentConfig.Protocol;
using Xunit;

namespace FluentConfig.Tests
{
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
                    if (Application.Current == null)
                        new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

                    var cph = Phase2HostSmokeTests.CreateMockCph();
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
                        if (window != null)
                        {
                            typeof(FluentConfigHostWindow)
                                .GetMethod("DisposeWebViewCore", BindingFlags.Instance | BindingFlags.NonPublic)
                                ?.Invoke(window, null);
                            window.Close();
                        }
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
    }
}
