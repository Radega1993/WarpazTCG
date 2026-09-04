namespace Warpaz.Domain
{
    /// <summary>
    /// Outcome of a match. In-progress matches have <see cref="MatchEndCause.None"/> and no winner.
    /// </summary>
    public sealed class MatchResult
    {
        public static readonly MatchResult InProgress = new MatchResult(
            false,
            MatchEndCause.None,
            false,
            PlayerId.Host);

        private MatchResult(bool isFinished, MatchEndCause cause, bool hasWinner, PlayerId winner)
        {
            IsFinished = isFinished;
            Cause = cause;
            HasWinner = hasWinner;
            Winner = winner;
        }

        public bool IsFinished { get; }
        public MatchEndCause Cause { get; }
        public bool HasWinner { get; }
        public PlayerId Winner { get; }

        public static MatchResult Ended(PlayerId winner, MatchEndCause cause)
        {
            if (cause == MatchEndCause.None)
            {
                throw new System.ArgumentException(
                    "A finished match requires a MatchEndCause other than None.",
                    nameof(cause));
            }

            if (!winner.IsSeated)
            {
                throw new System.ArgumentException("Winner must be a seated player.", nameof(winner));
            }

            return new MatchResult(true, cause, true, winner);
        }

        public override string ToString()
        {
            if (!IsFinished)
            {
                return "InProgress";
            }

            return Cause + ":" + Winner;
        }
    }
}
