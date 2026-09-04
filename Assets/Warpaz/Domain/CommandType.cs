namespace Warpaz.Domain
{
    /// <summary>Legal command kinds. Unsupported types must be rejected without mutating state.</summary>
    public enum CommandType
    {
        CreateMatch = 0,
        ConfirmMulligan = 1,
        EndTurn = 2,
        Play = 3,
        Equip = 4,
        Ascend = 5,
        Attack = 6,
        Reorganize = 7,
        Activate = 8,
        React = 9,
        Respond = 10,
        PassPriority = 11,
        ChooseTargets = 12,
        Concede = 13
    }
}
