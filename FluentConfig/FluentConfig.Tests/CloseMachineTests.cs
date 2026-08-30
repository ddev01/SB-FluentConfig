using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using FluentConfig.Core;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Native / in-app close machine without WebView2: timeout liveness, alreadyConfirmed,
    /// allowClose × dontRemind, and sticky host-exit.
    /// </summary>
    [Collection("StaWpf")]
    public class CloseMachineTests
    {
        [Theory]
        [InlineData(false, false, false, false)]
        [InlineData(false, true, false, true)]
        [InlineData(true, false, true, false)]
        [InlineData(true, true, true, true)]
        public void NativeCloseReply_AllowCloseAndDontRemind(
            bool allowClose, bool dontRemind, bool expectClosed, bool expectPref)
        {
            RunSta(harness =>
            {
                harness.Session.DeliverNativeCloseReplyForTests(Reply(allowClose, dontRemind));

                Assert.Equal(expectClosed, harness.Closed);
                Assert.False(harness.Session.ClosePromptInFlightForTests);
                Assert.Equal(expectPref, DontRemindPref(harness.Store, harness.Title));
            });
        }

        [Fact]
        public void TimeoutAfterWebAlive_DoesNotClose()
        {
            RunSta(harness =>
            {
                harness.Bridge.DeliverWebMessageForTests("{}");
                Assert.True(harness.Bridge.WebAlive);

                harness.Session.DeliverNativeCloseReplyForTests(null);

                Assert.False(harness.Closed);
                Assert.False(harness.Session.ClosePromptInFlightForTests);
                Assert.False(harness.Session.CloseAlreadyConfirmedForTests);
            });
        }

        [Fact]
        public void TimeoutBeforeWebAlive_Closes()
        {
            RunSta(harness =>
            {
                Assert.False(harness.Bridge.WebAlive);

                harness.Session.DeliverNativeCloseReplyForTests(null);

                Assert.True(harness.Closed);
                Assert.True(harness.Session.CloseAlreadyConfirmedForTests);
            });
        }

        [Fact]
        public void AlreadyConfirmedTrue_ClosesWithoutAskingWeb()
        {
            RunSta(harness =>
            {
                harness.Session.HandleWebMessage(WindowCloseJson(alreadyConfirmed: true, dontRemindAgain: true));

                Assert.True(harness.Closed);
                Assert.True(harness.Session.CloseAlreadyConfirmedForTests);
                Assert.True(DontRemindPref(harness.Store, harness.Title));
                Assert.False(harness.Session.ClosePromptInFlightForTests);
            });
        }

        [Fact]
        public void AlreadyConfirmedFalse_DoesNotCloseUntilWebAllows()
        {
            RunSta(harness =>
            {
                harness.Session.HandleWebMessage(WindowCloseJson(alreadyConfirmed: false, dontRemindAgain: false));

                Assert.False(harness.Closed);
                Assert.True(harness.Session.ClosePromptInFlightForTests);
                Assert.False(harness.Session.CloseAlreadyConfirmedForTests);

                harness.Session.DeliverNativeCloseReplyForTests(Reply(allowClose: false, dontRemind: false));
                Assert.False(harness.Closed);

                harness.Session.RequestNativeCloseForTests();
                harness.Session.DeliverNativeCloseReplyForTests(Reply(allowClose: true, dontRemind: false));
                Assert.True(harness.Closed);
            });
        }

        [Fact]
        public void HostExitInProgress_SkipsNativeClose()
        {
            RunSta(harness =>
            {
                FluentConfigSession.SetHostExitInProgressForTests(true);
                try
                {
                    harness.Session.RequestNativeCloseForTests();
                    Assert.False(harness.Closed);
                    Assert.False(harness.Session.ClosePromptInFlightForTests);
                }
                finally
                {
                    FluentConfigSession.SetHostExitInProgressForTests(false);
                }
            });
        }

        [Fact]
        public void HostExit_EmptyTakeAll_ClearsStickyFlag()
        {
            RunSta(harness =>
            {
                FluentConfigWindowManager.Unregister(harness.Title);
                FluentConfigSession.SetHostExitInProgressForTests(true);
                FluentConfigSession.CloseAllWindowsForHostExitForTests();
                Assert.False(FluentConfigSession.HostExitInProgressForTests);
            });
        }

        [Fact]
        public void NewWindowRegister_ClearsStickyHostExitFlag()
        {
            RunSta(harness =>
            {
                FluentConfigSession.SetHostExitInProgressForTests(true);
                FluentConfigSession.EnsureHostExitHookForTests();
                Assert.False(FluentConfigSession.HostExitInProgressForTests);
            });
        }

        [Fact]
        public void HostExit_TearsDownRegisteredWindow_ThenClearsFlag()
        {
            RunSta(harness =>
            {
                FluentConfigSession.CloseAllWindowsForHostExitForTests();
                Assert.True(harness.Closed);
                Assert.False(FluentConfigSession.HostExitInProgressForTests);
            });
        }

        private static JToken Reply(bool allowClose, bool dontRemind)
        {
            return JObject.FromObject(new WindowCloseRequestedResult
            {
                AllowClose = allowClose,
                DontRemindAgain = dontRemind,
            }, ProtocolJson.CreateSerializer());
        }

        private static string WindowCloseJson(bool alreadyConfirmed, bool dontRemindAgain)
        {
            return ProtocolJson.Serialize(WireMessage.Request(1, RpcMethods.WindowClose, new WindowCloseParams
            {
                AlreadyConfirmed = alreadyConfirmed,
                DontRemindAgain = dontRemindAgain,
            }));
        }

        private static bool DontRemindPref(ConcurrentDictionary<string, string> store, string title)
        {
            if (!store.TryGetValue(GeneralSettingsStore.GlobalKey, out var json) || string.IsNullOrEmpty(json))
                return false;
            var root = JObject.Parse(json);
            return root["windows"]?[title]?["dontRemindDiscard"]?.Value<bool>() == true;
        }

        private static void RunSta(Action<CloseHarness> body)
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                CloseHarness harness = null;
                try
                {
                    harness = new CloseHarness();
                    body(harness);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    harness?.Dispose();
                    try
                    {
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

        private sealed class CloseHarness : IDisposable
        {
            public CloseHarness()
            {
                Title = "CloseMachine-" + Guid.NewGuid().ToString("N");
                Store = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
                var cph = TestCph.CreateMockCph(Store);
                Session = new FluentConfigSession(cph.Object, Title, "1.0");
                Window = new FluentConfigHostWindow(Title, "1.0");
                Window.Closed += (_, __) => Closed = true;
                Bridge = new HostBridge(Window, Session);
                Session.BindHostForTests(Window, Bridge);
                FluentConfigWindowManager.Register(Title, Window);
            }

            public string Title { get; }
            public ConcurrentDictionary<string, string> Store { get; }
            public FluentConfigSession Session { get; }
            public FluentConfigHostWindow Window { get; }
            public HostBridge Bridge { get; }
            public bool Closed { get; private set; }

            public void Dispose()
            {
                FluentConfigSession.SetHostExitInProgressForTests(false);
                try { Bridge?.Detach(); } catch { /* ignore */ }
                try { Window?.DisposeWebViewCore(); } catch { /* ignore */ }
                try
                {
                    if (Window != null && !Closed)
                        Window.CloseAllowed();
                }
                catch { /* ignore */ }
                FluentConfigWindowManager.Unregister(Title);
            }
        }
    }
}
