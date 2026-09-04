namespace Warpaz.Domain
{
    /// <summary>
    /// Applies one accepted <see cref="CommandType"/> after <see cref="CommandProcessor"/> gates.
    /// Must not mutate <see cref="GameState"/> when returning a rejection.
    /// </summary>
    public interface ICommandHandler
    {
        CommandType Type { get; }

        CommandResult Handle(GameState state, Command command);
    }
}
