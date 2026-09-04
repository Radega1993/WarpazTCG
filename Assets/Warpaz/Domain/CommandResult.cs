using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Result of submitting a <see cref="Command"/>.
    /// When <see cref="Accepted"/> is false, <see cref="State"/> is unchanged and <see cref="Events"/> is empty.
    /// </summary>
    public sealed class CommandResult
    {
        private CommandResult(
            bool accepted,
            CommandRejectionCode code,
            string rejectionReason,
            GameState state,
            IReadOnlyList<GameEvent> events,
            string stateHash)
        {
            Accepted = accepted;
            Code = code;
            RejectionReason = rejectionReason;
            State = state;
            Events = events;
            StateHash = stateHash ?? string.Empty;
        }

        public bool Accepted { get; }

        public CommandRejectionCode Code { get; }

        public string RejectionReason { get; }

        public GameState State { get; }

        public IReadOnlyList<GameEvent> Events { get; }

        public string StateHash { get; }

        public static CommandResult Accept(GameState state, IReadOnlyList<GameEvent> events)
        {
            return new CommandResult(
                true,
                CommandRejectionCode.None,
                string.Empty,
                state,
                events ?? Array.Empty<GameEvent>(),
                HashOrEmpty(state));
        }

        public static CommandResult Reject(GameState state, string reason)
        {
            return Reject(state, CommandRejectionCode.InvalidPayload, reason);
        }

        public static CommandResult Reject(GameState state, CommandRejectionCode code, string reason)
        {
            if (code == CommandRejectionCode.None)
            {
                throw new ArgumentException("A rejection requires a code other than None.", nameof(code));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Rejection reason must be a clear, non-empty message.", nameof(reason));
            }

            return new CommandResult(
                false,
                code,
                reason,
                state,
                Array.Empty<GameEvent>(),
                HashOrEmpty(state));
        }

        private static string HashOrEmpty(GameState state)
        {
            return state == null ? string.Empty : GameStateHasher.Hash(state);
        }
    }
}
