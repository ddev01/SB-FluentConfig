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
                FluentConfigWindowManager.InvokeWindowClosedCallback(800, 600);
                Assert.Equal(800, receivedWidth);
                Assert.Equal(600, receivedHeight);
            }
            finally
            {
                FluentConfigWindowManager.SetWindowClosedCallback(null);
                FluentConfigWindowManager.SetOpened(false);
            }
        }

        [Fact]
        public void InvokeWindowClosedCallback_NullCallback_DoesNotThrow()
        {
            FluentConfigWindowManager.SetWindowClosedCallback(null);
            FluentConfigWindowManager.InvokeWindowClosedCallback(100, 100);
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
