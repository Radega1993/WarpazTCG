using System.Collections.Generic;
using System.Text;

namespace Warpaz.Tests.EditMode
{
    public sealed class Sprint2StressReport
    {
        public int MatchesStarted { get; set; }

        public int MatchesCompleted { get; set; }

        public int CommandsAccepted { get; set; }

        public int PlaysAccepted { get; set; }

        public int AttacksAccepted { get; set; }

        public int ReorgsAccepted { get; set; }

        public int InterceptsResolved { get; set; }

        public int UnexpectedRejections { get; set; }

        public int InvariantBreaks { get; set; }

        public int Stalls { get; set; }

        public long ElapsedMilliseconds { get; set; }

        public ulong? FirstFailedSeed { get; set; }

        public string FirstFailureReason { get; set; }

        public List<string> FinalHashes { get; } = new List<string>();

        public void AddFinalHash(string hash)
        {
            FinalHashes.Add(hash);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sprint2StressReport");
            sb.AppendLine("started=" + MatchesStarted + " completed=" + MatchesCompleted);
            sb.AppendLine("commands=" + CommandsAccepted +
                          " plays=" + PlaysAccepted +
                          " attacks=" + AttacksAccepted +
                          " reorgs=" + ReorgsAccepted +
                          " intercepts=" + InterceptsResolved);
            sb.AppendLine("rejects=" + UnexpectedRejections +
                          " invariants=" + InvariantBreaks +
                          " stalls=" + Stalls);
            sb.AppendLine("elapsedMs=" + ElapsedMilliseconds);
            if (FirstFailedSeed.HasValue)
            {
                sb.AppendLine("firstFailSeed=" + FirstFailedSeed.Value + " reason=" + FirstFailureReason);
            }

            return sb.ToString();
        }
    }
}
