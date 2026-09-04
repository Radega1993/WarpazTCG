namespace Warpaz.Domain
{
    internal sealed class CreateMatchHandler : ICommandHandler
    {
        private readonly ICardCatalog _catalog;

        public CreateMatchHandler(ICardCatalog catalog)
        {
            _catalog = catalog;
        }

        public CommandType Type => CommandType.CreateMatch;

        public CommandResult Handle(GameState state, Command command)
        {
            return MatchFactory.TryCreate(_catalog, command.MatchSetup);
        }
    }
}
