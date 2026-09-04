namespace Warpaz.Domain
{
    /// <summary>Match statuses from specification v0.1 §2.1 / §3.7.</summary>
    public enum StatusType
    {
        Recent = 0,
        Hidden = 1,
        Detected = 2,
        Immobilized = 3,
        Frozen = 4
    }
}
