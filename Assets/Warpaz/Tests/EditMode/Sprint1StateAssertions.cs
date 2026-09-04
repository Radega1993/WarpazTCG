using System;
using System.Collections.Generic;
using NUnit.Framework;
using Warpaz.Domain;

namespace Warpaz.Tests.EditMode
{
    /// <summary>
    /// Shared Sprint-1 post-command checks for integration and stress runners.
    /// Prefer <see cref="FindViolation"/> in non-asserting runners; use
    /// <see cref="AssertValid"/> in NUnit tests.
    /// </summary>
    internal static class Sprint1StateAssertions
    {
        public static void AssertValid(GameState state, int prevEventIndex)
        {
            var reason = FindViolation(state, prevEventIndex);
            Assert.IsNull(reason, reason);
        }

        /// <summary>
        /// Returns null when all Sprint-1 invariants hold; otherwise a diagnostic message.
        /// Also checks <see cref="GameStateInvariants"/>.
        /// </summary>
        public static string FindViolation(GameState state, int prevEventIndex)
        {
            if (state == null)
            {
                return "GameState is null.";
            }

            var domain = GameStateInvariants.FindViolation(state);
            if (domain != null)
            {
                return domain;
            }

            if (state.EventIndex < prevEventIndex)
            {
                return "EventIndex decreased from " + prevEventIndex + " to " + state.EventIndex + ".";
            }

            var hash = GameStateHasher.Hash(state);
            if (string.IsNullOrEmpty(hash))
            {
                return "Hash is empty.";
            }

            if (hash.Length != 64)
            {
                return "Hash length is " + hash.Length + ", expected 64.";
            }

            foreach (var ps in state.AllPlayers)
            {
                if (ps.PaCurrent < 0 || ps.PaCurrent > ps.PaMax)
                {
                    return "PA out of range for player " + ps.PlayerId +
                           ": " + ps.PaCurrent + "/" + ps.PaMax + ".";
                }

                if (ps.PaMax < 0 || ps.PaMax > 8)
                {
                    return "PaMax out of 0..8 for player " + ps.PlayerId + ": " + ps.PaMax + ".";
                }

                if (ps.Resolution < 0 || ps.Resolution > 5)
                {
                    return "Resolution out of 0..5 for player " + ps.PlayerId + ": " + ps.Resolution + ".";
                }

                var instances = CountMainDeckInstances(state, ps.PlayerId);
                if (instances != 40)
                {
                    return "Expected 40 main-deck instances for player " + ps.PlayerId +
                           " but got " + instances + ".";
                }
            }

            var duplicate = FindDuplicateAcrossZones(state);
            if (duplicate != null)
            {
                return duplicate;
            }

            if (!state.ActivePlayerId.IsSeated)
            {
                return "ActivePlayerId is not seated: " + state.ActivePlayerId + ".";
            }

            if (!Enum.IsDefined(typeof(MatchPhase), state.Phase))
            {
                return "Phase is not a valid MatchPhase: " + (int)state.Phase + ".";
            }

            if (state.Phase == MatchPhase.Main && state.PendingDecision.IsPending)
            {
                return "Pending decision during Main: " + state.PendingDecision.Kind + ".";
            }

            return null;
        }

        public static int CountMainDeckInstances(GameState state, PlayerId playerId)
        {
            var ps = state.GetPlayer(playerId);
            var count = ps.DeckInstanceIds.Count
                + ps.HandInstanceIds.Count
                + ps.DiscardInstanceIds.Count;
            foreach (var slot in ps.BoardSlots)
            {
                if (!slot.Occupant.IsNone)
                {
                    count++;
                }
            }

            return count;
        }

        private static string FindDuplicateAcrossZones(GameState state)
        {
            var seen = new HashSet<CardInstanceId>();
            foreach (var ps in state.AllPlayers)
            {
                foreach (var id in ps.DeckInstanceIds)
                {
                    if (!id.IsNone && !seen.Add(id))
                    {
                        return "Duplicate instance id across zones: " + id.Value;
                    }
                }

                foreach (var id in ps.HandInstanceIds)
                {
                    if (!id.IsNone && !seen.Add(id))
                    {
                        return "Duplicate instance id across zones: " + id.Value;
                    }
                }

                foreach (var id in ps.DiscardInstanceIds)
                {
                    if (!id.IsNone && !seen.Add(id))
                    {
                        return "Duplicate instance id across zones: " + id.Value;
                    }
                }

                foreach (var slot in ps.BoardSlots)
                {
                    if (!slot.Occupant.IsNone && !seen.Add(slot.Occupant))
                    {
                        return "Duplicate instance id across zones: " + slot.Occupant.Value;
                    }
                }
            }

            return null;
        }
    }
}
