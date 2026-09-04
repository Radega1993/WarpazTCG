namespace Warpaz.Domain
{
    /// <summary>Whole-hand mulligan choice. Partial mulligan is out of scope for Sprint 1.</summary>
    public enum MulliganDecision
    {
        KeepHand = 0,
        ReplaceWholeHand = 1
    }
}
