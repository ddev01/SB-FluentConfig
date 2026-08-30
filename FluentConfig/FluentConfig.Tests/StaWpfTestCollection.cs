using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// STA WPF tests and every consumer of the process-wide <c>FluentConfigWindowManager</c>
    /// registry. W1 host-exit <c>TakeAllWindows</c> and W3 reservation tests otherwise race
    /// <c>ConcurrentIsOpen</c> when xUnit runs classes in parallel.
    /// </summary>
    [CollectionDefinition("StaWpf")]
    public sealed class StaWpfTestCollection
    {
    }
}
