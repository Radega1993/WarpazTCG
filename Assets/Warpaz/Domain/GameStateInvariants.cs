using System.Collections.Generic;

namespace Warpaz.Domain
{
    public static class GameStateInvariants
    {
        public static void AssertValid(GameState state)
        {
            var reason = FindViolation(state);
            if (reason != null)
            {
                throw new InvariantViolationException(reason);
            }
        }

        public static string FindViolation(GameState state)
        {
            if (state == null)
            {
                return "GameState is null.";
            }

            if (state.EventIndex != state.Events.Count)
            {
                return "eventIndex does not match event log length.";
            }

            var seen = new HashSet<CardInstanceId>();
            foreach (var card in state.Cards.Values)
            {
                if (!seen.Add(card.InstanceId))
                {
                    return "Duplicate instance id " + card.InstanceId + ".";
                }
            }

            for (var p = 0; p < 2; p++)
            {
                var player = state.Players[p];
                if (player == null)
                {
                    return "Missing player " + p + ".";
                }

                if (player.PaCurrent < 0 || player.PaMax < 0)
                {
                    return "PA is negative.";
                }

                if (player.PaCurrent > player.PaMax)
                {
                    return "paCurrent exceeds paMax.";
                }

                if (player.PaMax > state.Rules.PaLimit)
                {
                    return "paMax exceeds rule maximum.";
                }

                if (player.Resolution < 0 || player.Resolution > state.Rules.InitialResolution)
                {
                    return "Resolution out of range.";
                }

                if (OverlappingZoneLists(player))
                {
                    return "A CardInstance appears in more than one zone list for player " + p + ".";
                }

                if (CountZone(state, player, player.Deck, CardZone.Deck) < 0)
                {
                    return "Deck zone invariant failed for player " + p + ".";
                }

                if (CountZone(state, player, player.Hand, CardZone.Hand) < 0)
                {
                    return "Hand zone invariant failed for player " + p + ".";
                }

                if (CountZone(state, player, player.Discard, CardZone.Discard) < 0)
                {
                    return "Discard zone invariant failed for player " + p + ".";
                }

                if (player.Deck.Count + player.Hand.Count + player.Discard.Count != state.Rules.DeckSize)
                {
                    return "Player " + p + " does not have " + state.Rules.DeckSize + " main-deck instances.";
                }

                if (!IdentityInZone(state, player.HeroInstanceId, player.PlayerId, CardZone.Hero) ||
                    !IdentityInZone(state, player.ClassInstanceId, player.PlayerId, CardZone.ClassPath))
                {
                    return "Identity cards missing for player " + p + ".";
                }
            }

            return null;
        }

        private static bool OverlappingZoneLists(PlayerState player)
        {
            var seen = new HashSet<CardInstanceId>();
            return !AddAll(seen, player.Deck) || !AddAll(seen, player.Hand) || !AddAll(seen, player.Discard);
        }

        private static bool AddAll(HashSet<CardInstanceId> seen, List<CardInstanceId> ids)
        {
            foreach (var id in ids)
            {
                if (!seen.Add(id))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountZone(
            GameState state,
            PlayerState player,
            List<CardInstanceId> ids,
            CardZone expected)
        {
            foreach (var id in ids)
            {
                if (!state.Cards.TryGetValue(id, out var card))
                {
                    return -1;
                }

                if (card.OwnerId != player.PlayerId || card.Zone != expected)
                {
                    return -1;
                }
            }

            return ids.Count;
        }

        private static bool IdentityInZone(
            GameState state,
            CardInstanceId instanceId,
            PlayerId playerId,
            CardZone expected)
        {
            return state.Cards.TryGetValue(instanceId, out var card)
                && card.OwnerId == playerId
                && card.Zone == expected;
        }
    }

    public sealed class InvariantViolationException : System.Exception
    {
        public InvariantViolationException(string message)
            : base(message)
        {
        }
    }
}
