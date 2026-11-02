using System;
using System.Collections.Generic;
using System.Globalization;

namespace Warpaz.Domain
{
    /// <summary>
    /// Builds a deterministic <see cref="GameState"/> from a validated <see cref="MatchSetup"/>.
    /// Rejected requests never produce a partial state.
    /// </summary>
    public static class MatchFactory
    {
        public static CommandResult TryCreate(ICardCatalog catalog, MatchSetup setup)
        {
            if (catalog == null)
            {
                return CommandResult.Reject(null, CommandRejectionCode.InvalidPayload, "Catalog is missing.");
            }

            if (setup == null || setup.Rules == null)
            {
                return CommandResult.Reject(null, CommandRejectionCode.InvalidPayload, "Match setup is missing.");
            }

            if (!string.Equals(setup.ContentVersion, catalog.ContentVersion, StringComparison.Ordinal))
            {
                return CommandResult.Reject(null, CommandRejectionCode.InvalidPayload, "Incompatible content version.");
            }

            var seatReason = ValidateSeats(setup);
            if (seatReason != null)
            {
                return CommandResult.Reject(null, CommandRejectionCode.InvalidPayload, seatReason);
            }

            var p0 = DeckValidator.Validate(setup.Player0, catalog, setup.Rules);
            if (!p0.IsValid)
            {
                return CommandResult.Reject(null, CommandRejectionCode.InvalidPayload, "Player 0: " + p0.Reason);
            }

            var p1 = DeckValidator.Validate(setup.Player1, catalog, setup.Rules);
            if (!p1.IsValid)
            {
                return CommandResult.Reject(null, CommandRejectionCode.InvalidPayload, "Player 1: " + p1.Reason);
            }

            var state = new GameState(setup.ContentVersion, setup.Rules, setup.Seed, new SeededRng(setup.Seed));
            var from = state.EventIndex;
            state.Emit(
                GameEventType.MatchCreated,
                Command.HostActorId,
                setup.Seed.ToString(CultureInfo.InvariantCulture)
                + ":" + setup.ContentVersion
                + ":" + setup.Rules.RuleVersion);

            InstantiatePlayer(catalog, state, PlayerId.Player0, setup.Player0);
            InstantiatePlayer(catalog, state, PlayerId.Player1, setup.Player1);

            if (!setup.PreserveMainDeckOrder)
            {
                ShuffleDeck(state, PlayerId.Player0);
                ShuffleDeck(state, PlayerId.Player1);
            }

            state.FirstPlayerId = setup.ForceFirstPlayer0
                ? PlayerId.Player0
                : PlayerId.FromIndex(state.Rng.Next(0, 2));
            state.Emit(GameEventType.FirstPlayerChosen, Command.HostActorId, state.FirstPlayerId.ToString());

            DrawCards(state, PlayerId.Player0, setup.Rules.InitialHandSize);
            DrawCards(state, PlayerId.Player1, setup.Rules.InitialHandSize);

            state.Phase = MatchPhase.MulliganP0;
            state.PriorityPlayerId = PlayerId.Player0;
            state.ActivePlayerId = state.FirstPlayerId;
            state.Emit(GameEventType.PhaseChanged, Command.HostActorId, MatchPhase.MulliganP0.ToString());

            GameStateInvariants.AssertValid(state);
            return Snapshot(state, from);
        }

        internal static void ShuffleDeck(GameState state, PlayerId playerId)
        {
            var player = state.GetPlayer(playerId);
            DeterministicShuffle.FisherYates(player.Deck, state.Rng);
            state.Emit(GameEventType.DeckShuffled, playerId, playerId.ToString());
        }

        internal static void DrawCards(GameState state, PlayerId playerId, int count)
        {
            var player = state.GetPlayer(playerId);
            for (var i = 0; i < count; i++)
            {
                if (player.Deck.Count == 0)
                {
                    break;
                }

                var id = player.Deck[0];
                player.Deck.RemoveAt(0);
                player.Hand.Add(id);
                state.Cards[id].SetZone(CardZone.Hand);
                state.Emit(
                    GameEventType.CardDrawn,
                    playerId,
                    id.ToString(),
                    id,
                    GameEvent.NoParent,
                    GameEventVisibility.OwnerOnly);
            }
        }

        private static string ValidateSeats(MatchSetup setup)
        {
            if (setup.Player0 == null || setup.Player1 == null)
            {
                return "Player setup is missing.";
            }

            if (!setup.Player0.PlayerId.IsSeated || !setup.Player1.PlayerId.IsSeated)
            {
                return "PlayerId must be a seated player.";
            }

            if (setup.Player0.PlayerId == setup.Player1.PlayerId)
            {
                return "Duplicate PlayerId.";
            }

            if (setup.Player0.PlayerId != PlayerId.Player0)
            {
                return "Player 0 must use PlayerId 0.";
            }

            if (setup.Player1.PlayerId != PlayerId.Player1)
            {
                return "Player 1 must use PlayerId 1.";
            }

            return null;
        }

        private static void InstantiatePlayer(
            ICardCatalog catalog,
            GameState state,
            PlayerId playerId,
            PlayerSetup setup)
        {
            var hero = CreateCard(catalog, state, setup.HeroCardId, playerId, CardZone.Hero);
            state.Emit(
                GameEventType.CardMoved,
                playerId,
                setup.HeroCardId + ":" + CardZone.Hero,
                hero.InstanceId);

            var classCard = CreateCard(catalog, state, setup.ClassCardId, playerId, CardZone.ClassPath);
            state.Emit(
                GameEventType.CardMoved,
                playerId,
                setup.ClassCardId + ":" + CardZone.ClassPath,
                classCard.InstanceId);

            var player = new PlayerState(
                playerId,
                setup.HeroCardId,
                setup.ClassCardId,
                hero.InstanceId,
                classCard.InstanceId,
                state.Rules);
            state.Players[playerId.Index] = player;

            foreach (var cardId in setup.MainDeckCardIds)
            {
                var card = CreateCard(catalog, state, cardId, playerId, CardZone.Deck);
                player.Deck.Add(card.InstanceId);
            }
        }

        private static CardInstance CreateCard(
            ICardCatalog catalog,
            GameState state,
            CardId definitionId,
            PlayerId ownerId,
            CardZone zone)
        {
            if (!catalog.TryGet(definitionId, out _))
            {
                throw new InvariantViolationException("Unknown definition " + definitionId + ".");
            }

            var instance = new CardInstance(state.AllocateInstanceId(), definitionId, ownerId, ownerId, zone);
            state.Cards.Add(instance.InstanceId, instance);
            return instance;
        }

        private static CommandResult Snapshot(GameState state, int fromIndex)
        {
            var events = new List<GameEvent>(state.Events.Count - fromIndex);
            for (var i = fromIndex; i < state.Events.Count; i++)
            {
                events.Add(state.Events[i]);
            }

            return CommandResult.Accept(state, events);
        }
    }
}
