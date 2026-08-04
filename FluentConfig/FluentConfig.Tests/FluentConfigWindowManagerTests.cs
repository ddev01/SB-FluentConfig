using System;
using System.Threading.Tasks;
using FluentConfig.Core;
using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// Behavior spec for duplicate-window guard + thread-safe static state.
    /// </summary>
    public class FluentConfigWindowManagerTests
    {
        [Fact]
        public void AlreadyOpened_WhenNotOpen_ReturnsFalse()
        {
            FluentConfigWindowManager.SetOpened(false);
            try
            {
                Assert.False(FluentConfigWindowManager.AlreadyOpened("Test", "1.0", null));
            }
            finally
            {
                FluentConfigWindowManager.SetOpened(false);
            }
        }

        [Fact]
        public void AlreadyOpened_WhenOpen_ReturnsTrue()
        {
            FluentConfigWindowManager.SetOpened(true);
            try
            {
                Assert.True(FluentConfigWindowManager.AlreadyOpened("Test", "1.0", null));
            }
            finally
            {
                FluentConfigWindowManager.SetOpened(false);
            }
        }

        [Fact]
        public void AlreadyOpened_WhenOpen_InvokesLog()
        {
            FluentConfigWindowManager.SetOpened(true);
            string loggedMessage = null;
            try
            {
                FluentConfigWindowManager.AlreadyOpened("MyPlugin", "2.0", msg => loggedMessage = msg);
                Assert.NotNull(loggedMessage);
                Assert.Contains("MyPlugin", loggedMessage);
                Assert.Contains("2.0", loggedMessage);
            }
            finally
            {
                FluentConfigWindowManager.SetOpened(false);
            }
        }

        [Fact]
        public void IsOpen_ReflectsSetOpened()
        {
            FluentConfigWindowManager.SetOpened(false);
            Assert.False(FluentConfigWindowManager.IsOpen);

            FluentConfigWindowManager.SetOpened(true);
            try
            {
                Assert.True(FluentConfigWindowManager.IsOpen);
            }
            finally
            {
                FluentConfigWindowManager.SetOpened(false);
            }
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
                FluentConfigWindowManager.SetOpened(false);
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
                FluentConfigWindowManager.SetOpened(false);
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
        public void ConcurrentSetOpened_DoesNotThrowOrCorrupt()
        {
            FluentConfigWindowManager.SetOpened(false);
            try
            {
                Parallel.For(0, 100, i =>
                {
                    FluentConfigWindowManager.SetOpened(i % 2 == 0);
                    _ = FluentConfigWindowManager.IsOpen;
                    FluentConfigWindowManager.AlreadyOpened("Concurrent", "1.0", null);
                });
            }
            finally
            {
                FluentConfigWindowManager.SetOpened(false);
            }
        }
    }
}
