namespace Warpaz.Domain
{
    /// <summary>Kind of input the engine is waiting for. Never encoded as a string.</summary>
    public enum PendingDecisionKind
    {
        None = 0,
        ChooseTargets = 1,
        ChooseOption = 2,
        OrderTriggers = 3,
        ChooseInterceptor = 4
    }
}
