namespace Warpaz.Domain
{
    /// <summary>
    /// Structured reason a <see cref="Command"/> was rejected. <see cref="None"/> means accepted.
    /// Player-facing failures use these codes; they must not throw.
    /// </summary>
    public enum CommandRejectionCode
    {
        None = 0,
        StaleState = 1,
        UnknownPlayer = 2,
        WrongActor = 3,
        InvalidPhase = 4,
        InvalidPayload = 5,
        CommandNotSupported = 6,
        MatchAlreadyFinished = 7,
        InvariantViolation = 8,
        DuplicateCommand = 9
    }
}
