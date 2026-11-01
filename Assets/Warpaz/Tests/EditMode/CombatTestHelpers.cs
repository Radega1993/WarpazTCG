using System;
using System.Collections.Generic;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    /// <summary>Shared setup helpers for Sprint 2 combat EditMode tests.</summary>
    internal static class CombatTestHelpers
    {
        public static GameState PlayToMain(ulong seed = 21UL, ICardCatalog catalog = null)
        {
            catalog = catalog ?? MatchFixtures.Catalog;
            var engine = new RulesEngine(catalog);
            var setup = catalog == MatchFixtures.Catalog
                ? MatchFixtures.Standard(seed)
                : new MatchSetup(
                    seed,
                    RuleConfig.AlphaDefaults(),
                    catalog.ContentVersion,
                    MatchFixtures.Player(PlayerId.Player0, MinimalCatalog.HeroA, MinimalCatalog.ClassA, MinimalCatalog.StandardDeck40()),
                    MatchFixtures.Player(PlayerId.Player1, MinimalCatalog.HeroB, MinimalCatalog.ClassB, MinimalCatalog.StandardDeck40()));

            var state = engine.Submit(null, Command.CreateMatch("c1", setup)).State;
            state = engine.Submit(state, Command.ConfirmMulligan("c2", 0, state.EventIndex, true)).State;
            state = engine.Submit(state, Command.ConfirmMulligan("c3", 1, state.EventIndex, true)).State;
            return state;
        }

        public static GameState EndTurn(RulesEngine engine, GameState state, string commandId)
        {
            var result = engine.Submit(
                state,
                Command.EndTurn(commandId, state.ActivePlayerId, state.EventIndex));
            if (!result.Accepted)
            {
                throw new InvariantViolationException(result.RejectionReason);
            }

            return result.State;
        }

        public static void SetPa(GameState state, PlayerId playerId, int pa)
        {
            var player = state.GetPlayer(playerId);
            if (pa > player.PaMax)
            {
                player.PaMax = Math.Min(pa, state.Rules.PaLimit);
            }

            player.PaCurrent = pa;
        }

        public static CardInstanceId FindInHand(GameState state, PlayerId playerId, ICardCatalog catalog, CardType type)
        {
            var player = state.GetPlayer(playerId);
            for (var i = 0; i < player.Hand.Count; i++)
            {
                var id = player.Hand[i];
                var card = state.Cards[id];
                if (catalog.TryGet(card.DefinitionId, out var def) && def.Type == type)
                {
                    return id;
                }
            }

            throw new InvalidOperationException("No " + type + " in hand for " + playerId + ".");
        }

        public static CardInstanceId EnsureCharacterInHand(GameState state, PlayerId playerId, ICardCatalog catalog)
        {
            var player = state.GetPlayer(playerId);
            for (var i = 0; i < player.Hand.Count; i++)
            {
                var id = player.Hand[i];
                if (catalog.TryGet(state.Cards[id].DefinitionId, out var def) && def.Type == CardType.Character)
                {
                    return id;
                }
            }

            for (var i = 0; i < player.Deck.Count; i++)
            {
                var id = player.Deck[i];
                if (catalog.TryGet(state.Cards[id].DefinitionId, out var def) && def.Type == CardType.Character)
                {
                    player.Deck.RemoveAt(i);
                    player.Hand.Add(id);
                    state.Cards[id].SetZone(CardZone.Hand);
                    return id;
                }
            }

            throw new InvalidOperationException("No Character available for " + playerId + ".");
        }

        public static GameState PlayCharacter(
            RulesEngine engine,
            GameState state,
            CardInstanceId instanceId,
            BoardRow row,
            int slotIndex,
            string commandId)
        {
            var result = engine.Submit(
                state,
                Command.Play(commandId, state.ActivePlayerId, state.EventIndex, instanceId, row, slotIndex));
            if (!result.Accepted)
            {
                throw new InvariantViolationException(result.RejectionReason);
            }

            return result.State;
        }

        public static GameState ClearRecentByPassingTurns(RulesEngine engine, GameState state, PlayerId owner)
        {
            var serial = 0;
            while (true)
            {
                serial++;
                state = EndTurn(engine, state, "pass-" + serial);
                if (state.Phase == MatchPhase.Main && state.ActivePlayerId == owner)
                {
                    return state;
                }

                if (serial > 8)
                {
                    throw new InvalidOperationException("Could not return to " + owner + " Main without Recent clear.");
                }
            }
        }

        public static bool HasEvent(GameState state, GameEventType type)
        {
            for (var i = 0; i < state.EventLog.Count; i++)
            {
                if (state.EventLog[i].Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        public static int CountEvents(GameState state, GameEventType type)
        {
            var count = 0;
            for (var i = 0; i < state.EventLog.Count; i++)
            {
                if (state.EventLog[i].Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        public static CardInstanceId Occupant(GameState state, PlayerId playerId, BoardRow row, int index)
        {
            var offset = row == BoardRow.Vanguard ? 0 : BoardSlot.SlotsPerRow;
            return state.GetPlayer(playerId).BoardSlots[offset + index].Occupant;
        }

        public static void PlaceReadyVanguardWithoutRecent(
            GameState state,
            PlayerId playerId,
            CardInstanceId instanceId,
            int slotIndex)
        {
            var player = state.GetPlayer(playerId);
            var card = state.Cards[instanceId];
            if (card.Zone == CardZone.Hand)
            {
                player.Hand.Remove(instanceId);
            }
            else if (card.Zone == CardZone.Deck)
            {
                player.Deck.Remove(instanceId);
            }
            else if (card.Zone == CardZone.Discard)
            {
                player.Discard.Remove(instanceId);
            }

            var slot = BoardSlot.Vanguard(slotIndex, instanceId);
            card.SetZone(CardZone.Vanguard, slot);
            card.ReadyState = ReadyState.Ready;
            card.RemoveStatus(StatusType.Recent);
            player.SetBoardSlot(slot);
        }
    }
}
