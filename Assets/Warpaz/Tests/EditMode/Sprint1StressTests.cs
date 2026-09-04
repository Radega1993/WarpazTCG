using NUnit.Framework;

namespace Warpaz.Tests.EditMode
{
    /// <summary>
    /// Sprint-1 resistance suite: 100 fixture matches, mulligan + EndTurn only.
    /// </summary>
    public sealed class Sprint1StressTests
    {
        [Test]
        public void Stress_100Matches_AllProcessed_NoIllegalNoStallNoInvariantBreak()
        {
            var config = new Sprint1StressConfig(
                matchCount: Sprint1StressConfig.DefaultMatchCount,
                baseSeed: Sprint1StressConfig.DefaultBaseSeed,
                maxTurns: Sprint1StressConfig.DefaultMaxTurns,
                abortOnFirstFailure: true);

            var report = new Sprint1StressRunner(config).Run();

            Assert.IsNull(report.FirstFailedSeed,
                "First failed seed=" + report.FirstFailedSeed +
                " reason=" + report.FirstFailureReason + "\n" + report);
            Assert.AreEqual(config.MatchCount, report.MatchesStarted, report.ToString());
            Assert.AreEqual(config.MatchCount, report.MatchesCompleted, report.ToString());
            Assert.AreEqual(0, report.UnexpectedRejections, report.ToString());
            Assert.AreEqual(0, report.Stalls, report.ToString());
            Assert.AreEqual(0, report.InvariantBreaks, report.ToString());
            Assert.AreEqual(config.MatchCount, report.FinalHashes.Count, report.ToString());
            Assert.IsTrue(report.Succeeded, report.ToString());
            Assert.Greater(report.CommandsAccepted, 0);
            TestContext.WriteLine(report.ToString());
        }

        [Test]
        public void Stress_SameSeeds_ProduceIdenticalFinalHashes()
        {
            var config = new Sprint1StressConfig(
                matchCount: Sprint1StressConfig.DefaultMatchCount,
                baseSeed: Sprint1StressConfig.DefaultBaseSeed,
                maxTurns: Sprint1StressConfig.DefaultMaxTurns,
                abortOnFirstFailure: true);

            var first = new Sprint1StressRunner(config).Run();
            var second = new Sprint1StressRunner(config).Run();

            Assert.IsTrue(first.Succeeded, "First run failed:\n" + first);
            Assert.IsTrue(second.Succeeded, "Second run failed:\n" + second);
            Assert.AreEqual(first.FinalHashes.Count, second.FinalHashes.Count);

            for (var i = 0; i < first.FinalHashes.Count; i++)
            {
                Assert.AreEqual(
                    first.FinalHashes[i],
                    second.FinalHashes[i],
                    "Hash mismatch at match index " + i +
                    " (seed " + (config.BaseSeed + (ulong)i) + ")");
            }
        }
    }
}
