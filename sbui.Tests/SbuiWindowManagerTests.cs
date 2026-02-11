using Sbui.Core;
using Xunit;

namespace Sbui.Tests
{
    public class SbuiWindowManagerTests
    {
        // These tests exercise the static state, so they must run sequentially.
        // Each test resets state at the end to avoid leaking to other tests.

        [Fact]
        public void AlreadyOpened_WhenNotOpen_ReturnsFalse()
        {
            SbuiWindowManager.SetOpened(false);
            try
            {
                Assert.False(SbuiWindowManager.AlreadyOpened("Test", "1.0", null));
            }
            finally
            {
                SbuiWindowManager.SetOpened(false);
            }
        }

        [Fact]
        public void AlreadyOpened_WhenOpen_ReturnsTrue()
        {
            SbuiWindowManager.SetOpened(true);
            try
            {
                Assert.True(SbuiWindowManager.AlreadyOpened("Test", "1.0", null));
            }
            finally
            {
                SbuiWindowManager.SetOpened(false);
            }
        }

        [Fact]
        public void AlreadyOpened_WhenOpen_InvokesLog()
        {
            SbuiWindowManager.SetOpened(true);
            string loggedMessage = null;
            try
            {
                SbuiWindowManager.AlreadyOpened("MyPlugin", "2.0", msg => loggedMessage = msg);
                Assert.NotNull(loggedMessage);
                Assert.Contains("MyPlugin", loggedMessage);
                Assert.Contains("2.0", loggedMessage);
            }
            finally
            {
                SbuiWindowManager.SetOpened(false);
            }
        }

        [Fact]
        public void IsOpen_ReflectsSetOpened()
        {
            SbuiWindowManager.SetOpened(false);
            Assert.False(SbuiWindowManager.IsOpen);

            SbuiWindowManager.SetOpened(true);
            try
            {
                Assert.True(SbuiWindowManager.IsOpen);
            }
            finally
            {
                SbuiWindowManager.SetOpened(false);
            }
        }

        [Fact]
        public void SetWindowClosedCallback_InvokesOnClose()
        {
            double receivedWidth = 0, receivedHeight = 0;
            SbuiWindowManager.SetWindowClosedCallback((w, h) =>
            {
                receivedWidth = w;
                receivedHeight = h;
            });
            try
            {
                SbuiWindowManager.InvokeWindowClosedCallback(800, 600);
                Assert.Equal(800, receivedWidth);
                Assert.Equal(600, receivedHeight);
            }
            finally
            {
                SbuiWindowManager.SetWindowClosedCallback(null);
                SbuiWindowManager.SetOpened(false);
            }
        }

        [Fact]
        public void InvokeWindowClosedCallback_NullCallback_DoesNotThrow()
        {
            SbuiWindowManager.SetWindowClosedCallback(null);
            SbuiWindowManager.InvokeWindowClosedCallback(100, 100);
            // No exception expected
        }
    }
}
