using System;
using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    public sealed class PlayerResourceTests
    {
        [Test]
        public void Pa_NeverExceedsLimit_OrGoesNegative()
        {
            var player = FreshPlayer();
            Assert.AreEqual(0, player.PaMax);
            Assert.AreEqual(0, player.PaCurrent);

            player.SetPaMax(8);
            player.SetPaCurrent(8);
            Assert.AreEqual(8, player.PaCurrent);

            var overMax = Assert.Throws<ArgumentOutOfRangeException>(() => player.SetPaMax(9));
            StringAssert.Contains("PaLimit", overMax.Message);

            var negativeMax = Assert.Throws<ArgumentOutOfRangeException>(() => player.SetPaMax(-1));
            StringAssert.Contains("negative", negativeMax.Message);

            var negativeCurrent = Assert.Throws<ArgumentOutOfRangeException>(() => player.SetPaCurrent(-1));
            StringAssert.Contains("negative", negativeCurrent.Message);

            var overCurrent = Assert.Throws<ArgumentOutOfRangeException>(() => player.SetPaCurrent(9));
            StringAssert.Contains("paMax", overCurrent.Message);
        }

        [Test]
        public void Resolution_StaysWithinZeroToInitial()
        {
            var player = FreshPlayer();
            Assert.AreEqual(5, player.Resolution);

            player.SetResolution(0);
            player.SetResolution(5);

            var over = Assert.Throws<ArgumentOutOfRangeException>(() => player.SetResolution(6));
            StringAssert.Contains("initialResolution", over.Message);

            var negative = Assert.Throws<ArgumentOutOfRangeException>(() => player.SetResolution(-1));
            StringAssert.Contains("negative", negative.Message);
        }

        private static PlayerState FreshPlayer()
        {
            return new PlayerState(
                PlayerId.Player0,
                new CardId("WZ1-001"),
                new CardId("WZ1-002"),
                new CardInstanceId(1),
                new CardInstanceId(2),
                RuleConfig.AlphaDefaults());
        }
    }
}
