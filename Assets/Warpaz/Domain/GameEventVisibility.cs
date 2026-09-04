namespace Warpaz.Domain
{
    /// <summary>
    /// Who may observe a <see cref="GameEvent"/>. The full log is for replay/host;
    /// seated players only see <see cref="Public"/> and their own <see cref="OwnerOnly"/>.
    /// </summary>
    public enum GameEventVisibility
    {
        Public = 0,
        OwnerOnly = 1,
        Debug = 2
    }
}
