namespace Warpaz.Domain
{
    /// <summary>
    /// Compatibility facade over <see cref="CommandProcessor"/>. Tests and
    /// <c>GameSession</c> keep calling <see cref="Submit"/>.
    /// </summary>
    public sealed class RulesEngine
    {
        private readonly CommandProcessor _processor;

        public RulesEngine(ICardCatalog catalog)
        {
            _processor = new CommandProcessor(catalog);
        }

        public CommandResult Submit(GameState current, Command command)
        {
            return _processor.Submit(current, command);
        }

        internal static void MoveDeckToDiscard(GameState state, int playerId)
        {
            MoveDeckToDiscard(state, PlayerId.FromIndex(playerId));
        }

        internal static void MoveDeckToDiscard(GameState state, PlayerId playerId)
        {
            var player = state.GetPlayer(playerId);
            while (player.Deck.Count > 0)
            {
                var id = player.Deck[0];
                player.Deck.RemoveAt(0);
                player.Discard.Add(id);
                state.Cards[id].SetZone(CardZone.Discard);
            }
        }
    }
}
