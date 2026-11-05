using NUnit.Framework;

namespace Warpaz.Tests.EditMode
{
    public sealed class Sprint2StressTests
    {
        [Test]
        public void TurnLimit_IsFailure_NotCompletedMatch()
        {
            var report = new Sprint2StressRunner(new Sprint2StressConfig(matchCount: 1, maxTurns: 0)).Run();
            Assert.AreEqual(0, report.MatchesCompleted);
            Assert.AreEqual(1, report.Stalls);
            StringAssert.Contains("before GameOver", report.FirstFailureReason);
        }

        [Test]
        public void Stress_50Matches_VanillaActions_Deterministic()
        {
            var config = new Sprint2StressConfig(
                matchCount: Sprint2StressConfig.DefaultMatchCount,
                baseSeed: Sprint2StressConfig.DefaultBaseSeed,
                maxTurns: Sprint2StressConfig.DefaultMaxTurns,
                abortOnFirstFailure: true);

            var first = new Sprint2StressRunner(config).Run();
            Assert.AreEqual(0, first.UnexpectedRejections, first.ToString());
            Assert.AreEqual(0, first.InvariantBreaks, first.ToString());
            Assert.AreEqual(0, first.Stalls, first.ToString());
            Assert.AreEqual(config.MatchCount, first.MatchesStarted, first.ToString());
            Assert.AreEqual(config.MatchCount, first.MatchesCompleted, first.ToString());
            Assert.Greater(first.PlaysAccepted + first.AttacksAccepted + first.ReorgsAccepted, 0, first.ToString());

            var second = new Sprint2StressRunner(config).Run();
            Assert.AreEqual(first.FinalHashes.Count, second.FinalHashes.Count);
            for (var i = 0; i < first.FinalHashes.Count; i++)
            {
                Assert.AreEqual(first.FinalHashes[i], second.FinalHashes[i], "hash mismatch at match " + i);
            }
        }
    }
}
