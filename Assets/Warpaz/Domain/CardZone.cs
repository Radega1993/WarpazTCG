namespace Warpaz.Domain
{
    /// <summary>Location of a <see cref="CardInstance"/>. A card occupies exactly one zone.</summary>
    public enum CardZone
    {
        Deck = 0,
        Hand = 1,
        Discard = 2,
        Hero = 3,
        ClassPath = 4,
        Vanguard = 5,
        Rearguard = 6,
        Equipment = 7,
        Resolution = 8,
        PendingStack = 9
    }
}
