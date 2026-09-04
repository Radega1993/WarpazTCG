using System.Collections.Generic;
using System.Text;

namespace Warpaz.Tests.EditMode
{
    /// <summary>
    /// Aggregate results of a Sprint-1 stress run. Not a balance report.
    /// </summary>
    public sealed class Sprint1StressReport
    {
        private readonly List<string> _finalHashes = new List<string>();

        public int MatchesStarted { get; set; }
        public int MatchesCompleted { get; set; }
        public int CommandsAccepted { get; set; }
        public int UnexpectedRejections { get; set; }
        public int InvariantBreaks { get; set; }
        public int Stalls { get; set; }
        public ulong? FirstFailedSeed { get; set; }
        public string FirstFailureReason { get; set; }
        public long? ElapsedMilliseconds { get; set; }

        public IReadOnlyList<string> FinalHashes => _finalHashes;

        public void AddFinalHash(string hash)
        {
            _finalHashes.Add(hash ?? string.Empty);
        }

        public bool Succeeded =>
            FirstFailedSeed == null
            && UnexpectedRejections == 0
            && InvariantBreaks == 0
            && Stalls == 0
            && MatchesStarted == MatchesCompleted;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sprint1StressReport");
            sb.AppendLine("  MatchesStarted=" + MatchesStarted);
            sb.AppendLine("  MatchesCompleted=" + MatchesCompleted);
            sb.AppendLine("  CommandsAccepted=" + CommandsAccepted);
            sb.AppendLine("  UnexpectedRejections=" + UnexpectedRejections);
            sb.AppendLine("  InvariantBreaks=" + InvariantBreaks);
            sb.AppendLine("  Stalls=" + Stalls);
            sb.AppendLine("  FirstFailedSeed=" + (FirstFailedSeed.HasValue ? FirstFailedSeed.Value.ToString() : "none"));
            if (!string.IsNullOrEmpty(FirstFailureReason))
            {
                sb.AppendLine("  FirstFailureReason=" + FirstFailureReason);
            }

            if (ElapsedMilliseconds.HasValue)
            {
                sb.AppendLine("  ElapsedMilliseconds=" + ElapsedMilliseconds.Value);
            }

            sb.AppendLine("  FinalHashCount=" + _finalHashes.Count);
            return sb.ToString();
        }
    }
}
