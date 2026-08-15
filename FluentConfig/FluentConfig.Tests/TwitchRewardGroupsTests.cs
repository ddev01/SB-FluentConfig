using System.Collections.Generic;
using Moq;
using Streamer.bot.Plugin.Interface;
using Xunit;

namespace FluentConfig.Tests
{
    public class TwitchRewardGroupsTests
    {
        [Fact]
        public void NullOrEmptyRewards_ReturnsEmptyArray()
        {
            var mock = new Mock<IInlineInvokeProxy>(MockBehavior.Loose);
            mock.Setup(c => c.TwitchGetRewards()).Returns((List<TwitchReward>)null);
            Assert.Empty(Fc.TwitchRewardGroups(mock.Object));

            mock.Setup(c => c.TwitchGetRewards()).Returns(new List<TwitchReward>());
            Assert.Empty(Fc.TwitchRewardGroups(mock.Object));
        }

        [Fact]
        public void DistinctTrimmedSorted_SkipsNullAndEmptyGroup()
        {
            var mock = new Mock<IInlineInvokeProxy>(MockBehavior.Loose);
            mock.Setup(c => c.TwitchGetRewards()).Returns(new List<TwitchReward>
            {
                null,
                new TwitchReward { Group = null },
                new TwitchReward { Group = "  " },
                new TwitchReward { Group = "vip" },
                new TwitchReward { Group = "VIP" },
                new TwitchReward { Group = " sub " },
                new TwitchReward { Group = "mod" },
            });

            var groups = Fc.TwitchRewardGroups(mock.Object);
            Assert.Equal(new[] { "mod", "sub", "vip" }, groups);
        }
    }
}
