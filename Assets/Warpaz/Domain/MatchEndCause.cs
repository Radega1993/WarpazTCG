namespace Warpaz.Domain
{
    /// <summary>Why a match finished. <see cref="None"/> means the match is still in progress.</summary>
    public enum MatchEndCause
    {
        None = 0,
        ResolutionDepleted = 1,
        Concede = 2
    }
}
