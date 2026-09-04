namespace Warpaz.Domain
{
    /// <summary>Observable engine outcome. Presentation must not invent extra event types.</summary>
    public enum GameEventType
    {
        MatchCreated = 0,
        DeckShuffled = 1,
        FirstPlayerChosen = 2,
        CardMoved = 3,
        CardDrawn = 4,
        MulliganKept = 5,
        MulliganReturned = 6,
        TurnStarted = 7,
        PhaseChanged = 8,
        PaMaxIncreased = 9,
        PaRefilled = 10,
        ResolutionRemoved = 11,
        TurnEnded = 12,
        MatchEnded = 13,
        DecisionOpened = 14,
        DecisionClosed = 15,
        InitialHandDrawn = 16,
        PreparationCompleted = 17,
        DrawFailed = 18,
        ActivePlayerChanged = 19
    }
}
