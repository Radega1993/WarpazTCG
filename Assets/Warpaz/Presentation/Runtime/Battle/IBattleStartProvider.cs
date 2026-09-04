namespace Warpaz.Presentation
{
    /// <summary>
    /// Mecanismo explícito para pasar setup entre escenas sin singletons globales.
    /// </summary>
    public interface IBattleStartProvider
    {
        void SetPendingBattleStart(BattleStartPayload payload);
        BattleStartPayload ConsumePendingBattleStart();
    }
}
