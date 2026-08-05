using Xunit;

namespace FluentConfig.Tests
{
    /// <summary>
    /// GitHubUpdater tests share static HttpClient / ApiBaseUrl overrides — run sequentially.
    /// </summary>
    [CollectionDefinition("GitHubUpdaterMock", DisableParallelization = true)]
    public sealed class GitHubUpdaterTestCollection
    {
    }
}
