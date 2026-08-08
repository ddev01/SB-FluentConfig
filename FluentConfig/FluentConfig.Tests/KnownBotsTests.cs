using System.Linq;
using Xunit;

namespace FluentConfig.Tests
{
    public class KnownBotsTests
    {
        [Theory]
        [InlineData("nightbot")]
        [InlineData("NightBot")]
        [InlineData("NIGHTBOT")]
        [InlineData(" streamelements ")]
        [InlineData("@moobot")]
        [InlineData(" @StreamLabs ")]
        public void IsKnownBot_TrueForCuratedLogins(string user)
        {
            Assert.True(KnownBots.IsKnownBot(user));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("@")]
        [InlineData("some_viewer")]
        [InlineData("nightbot_fan")]
        public void IsKnownBot_FalseForUnknownOrEmpty(string user)
        {
            Assert.False(KnownBots.IsKnownBot(user));
        }

        [Fact]
        public void GetKnownBots_ReturnsSortedNonEmptyCopy()
        {
            var list = KnownBots.GetKnownBots();
            Assert.NotEmpty(list);
            Assert.Contains("nightbot", list);
            Assert.Contains("commanderroot", list);

            var sorted = list.OrderBy(x => x, System.StringComparer.OrdinalIgnoreCase).ToList();
            Assert.Equal(sorted, list.ToList());
        }

        [Fact]
        public void GetKnownBots_MutationDoesNotAffectLookup()
        {
            var list = (string[])KnownBots.GetKnownBots();
            list[0] = "mutated_should_not_matter";

            Assert.True(KnownBots.IsKnownBot("nightbot"));
            Assert.False(KnownBots.IsKnownBot("mutated_should_not_matter"));
        }
    }
}
