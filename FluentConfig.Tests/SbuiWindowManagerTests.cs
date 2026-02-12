using FluentConfig.Core;
using Xunit;

namespace FluentConfig.Tests
{
    public class FluentConfigWindowManagerTests
    {
        // These tests exercise the static state, so they must run sequentially.
        // Each test resets state at the end to avoid leaking to other tests.

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
            // No exception expected
        }
    }
}
