using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FluentConfig.Core;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Per-title window registry and close callbacks.
    /// </summary>
    public class FluentConfigWindowManagerTests
    {
        [Fact]
        public void AlreadyOpened_WhenNotOpen_ReturnsFalse()
        {
            RunSta(() =>
            {
                ClearWindows();
                try
                {
                    Assert.False(FluentConfigWindowManager.AlreadyOpened("Test", "1.0", null));
                }
                finally
                {
                    ClearWindows();
                }
            });
        }

        [Fact]
        public void AlreadyOpened_DifferentTitles_AreIndependent()
        {
            RunSta(() =>
            {
                ClearWindows();
                try
                {
                    FluentConfigWindowManager.Register("Title A", new Window());
                    Assert.False(FluentConfigWindowManager.AlreadyOpened("Title B", "1.0", null));
                    Assert.True(FluentConfigWindowManager.AlreadyOpened("Title A", "1.0", null));
                }
                finally
                {
                    ClearWindows();
                }
            });
        }

        [Fact]
        public void AlreadyOpened_SameTitle_ReturnsTrueWithoutAffectingOtherTitle()
        {
            RunSta(() =>
            {
                ClearWindows();
                try
                {
                    FluentConfigWindowManager.Register("Title A", new Window());
                    FluentConfigWindowManager.Register("Title B", new Window());

                    Assert.True(FluentConfigWindowManager.AlreadyOpened("Title A", "2.0", null));
                    Assert.False(FluentConfigWindowManager.AlreadyOpened("Title C", "1.0", null));
                    Assert.True(FluentConfigWindowManager.IsOpen);
                }
                finally
                {
                    ClearWindows();
                }
            });
        }

        [Fact]
        public void AlreadyOpened_WhenOpen_InvokesLog()
        {
            RunSta(() =>
            {
                ClearWindows();
                string loggedMessage = null;
                try
                {
                    FluentConfigWindowManager.Register("MyPlugin", new Window());
                    FluentConfigWindowManager.AlreadyOpened("MyPlugin", "2.0", msg => loggedMessage = msg);
                    Assert.NotNull(loggedMessage);
                    Assert.Contains("MyPlugin", loggedMessage);
                    Assert.Contains("2.0", loggedMessage);
                }
                finally
                {
                    ClearWindows();
                }
            });
        }

        [Fact]
        public void IsOpen_ReflectsRegisteredWindows()
        {
            RunSta(() =>
            {
                ClearWindows();
                try
                {
                    Assert.False(FluentConfigWindowManager.IsOpen);
                    FluentConfigWindowManager.Register("One", new Window());
                    Assert.True(FluentConfigWindowManager.IsOpen);
                    FluentConfigWindowManager.Register("Two", new Window());
                    Assert.True(FluentConfigWindowManager.IsOpen);
                }
                finally
                {
                    ClearWindows();
                }
            });
        }

        [Fact]
        public void SetWindowClosedCallback_InvokesOnClose()
        {
            double receivedWidth = 0, receivedHeight = 0;
            FluentConfigWindowManager.SetWindowClosedCallback((w, h) =>
            {
                receivedWidth = w;
                receivedHeight = h;
            });
            try
            {
                FluentConfigWindowManager.InvokeWindowClosedCallback(10, 20, 800, 600);
                Assert.Equal(800, receivedWidth);
                Assert.Equal(600, receivedHeight);
            }
            finally
            {
                FluentConfigWindowManager.SetWindowClosedCallback((Action<double, double>)null);
                ClearWindows();
            }
        }

        [Fact]
        public void SetWindowClosedGeometryCallback_InvokesOnClose()
        {
            double left = 0, top = 0, width = 0, height = 0;
            FluentConfigWindowManager.SetWindowClosedCallback((double l, double t, double w, double h) =>
            {
                left = l;
                top = t;
                width = w;
                height = h;
            });
            try
            {
                FluentConfigWindowManager.InvokeWindowClosedCallback(10, 20, 800, 600);
                Assert.Equal(10, left);
                Assert.Equal(20, top);
                Assert.Equal(800, width);
                Assert.Equal(600, height);
            }
            finally
            {
                FluentConfigWindowManager.SetWindowClosedCallback((Action<double, double, double, double>)null);
                ClearWindows();
            }
        }

        [Fact]
        public void InvokeWindowClosedCallback_NullCallback_DoesNotThrow()
        {
            FluentConfigWindowManager.SetWindowClosedCallback((Action<double, double>)null);
            FluentConfigWindowManager.SetWindowClosedCallback((Action<double, double, double, double>)null);
            FluentConfigWindowManager.InvokeWindowClosedCallback(0, 0, 100, 100);
        }

        [Fact]
        public void ConcurrentIsOpen_DoesNotThrow()
        {
            RunSta(() =>
            {
                ClearWindows();
                FluentConfigWindowManager.Register("Concurrent-A", new Window());
                FluentConfigWindowManager.Register("Concurrent-B", new Window());
            });

            try
            {
                Parallel.For(0, 100, _ =>
                {
                    var open = FluentConfigWindowManager.IsOpen;
                    Assert.True(open);
                });
            }
            finally
            {
                RunSta(ClearWindows);
            }
        }

        private static void ClearWindows()
        {
            var windows = FluentConfigWindowManager.TakeAllWindows();
            foreach (var w in windows)
            {
                try { w?.Close(); } catch { /* ignore */ }
            }
        }

        private static void RunSta(Action action)
        {
            Exception error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    error = ex;
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
