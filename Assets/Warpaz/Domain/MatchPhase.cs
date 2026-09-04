namespace Warpaz.Domain
{
    /// <summary>High-level match step. Values after <see cref="Closing"/> are reserved for later timing slices.</summary>
    public enum MatchPhase
    {
        Setup = 0,
        MulliganP0 = 1,
        MulliganP1 = 2,
        Preparation = 3,
        Pa = 4,
        Draw = 5,
        Main = 6,
        Closing = 7,
        PendingChoice = 8,
        ReactionWindow = 9,
        ResolveStack = 10,
        GameOver = 11
    }
}
