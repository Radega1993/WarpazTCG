namespace Warpaz.Domain
{
    /// <summary>
    /// Immutable instruction. Natural language is never executed.
    /// </summary>
    public sealed class EffectDefinition
    {
        public EffectDefinition(EffectOpcode opcode, EffectTarget target, int amount = 0)
        { Opcode = opcode; Target = target; Amount = amount; }
        public EffectOpcode Opcode { get; }
        public EffectTarget Target { get; }
        public int Amount { get; }
    }
    public enum EffectOpcode { PowerUntilEndTurn, CannotInterceptUntilEndTurn, CannotReadyUntilEndTurn }
    public enum EffectTarget { AnyCharacter, FriendlyVanguard, EnemyCharacter, EnemyVanguard }
}
