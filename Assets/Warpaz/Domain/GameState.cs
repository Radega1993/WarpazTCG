using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Deterministic match snapshot. Contains no sprites, transforms, timestamps, or other view data.
    /// Mutate only by applying a <see cref="Command"/>.
    /// </summary>
    public sealed class GameState
    {
        public const string SchemaVersionValue = "0.1";

        internal GameState(
            string contentVersion,
            RuleConfig rules,
            ulong seed,
            IDeterministicRandom rng)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            ContentVersion = contentVersion ?? throw new ArgumentNullException(nameof(contentVersion));
            Rules = rules;
            Seed = seed;
            Rng = rng;
            SchemaVersion = SchemaVersionValue;
            Phase = MatchPhase.Setup;
            Result = MatchResult.InProgress;
            PendingDecision = PendingDecision.None;
            Cards = new Dictionary<CardInstanceId, CardInstance>();
            Players = new PlayerState[2];
            Events = new List<GameEvent>();
            PendingEffects = new List<PendingEffect>();
            AcceptedCommandIds = new List<CommandId>();
            NextInstanceId = 1;
            Turn = 0;
            ActivePlayerId = PlayerId.Player0;
            PriorityPlayerId = PlayerId.Player0;
            FirstPlayerId = PlayerId.Player0;
        }

        public string SchemaVersion { get; }
        public string ContentVersion { get; }
        public string RuleVersion => Rules.RuleVersion;
        public RuleConfig Rules { get; }
        public ulong Seed { get; }
        public int Turn { get; internal set; }
        public PlayerId ActivePlayerId { get; internal set; }
        public PlayerId PriorityPlayerId { get; internal set; }
        public PlayerId FirstPlayerId { get; internal set; }
        public MatchPhase Phase { get; internal set; }
        public MatchResult Result { get; internal set; }
        public PendingDecision PendingDecision { get; internal set; }
        public int EventIndex => Events.Count;
        public CombatWindow Combat { get; internal set; }
        public EffectStepKind? PendingEffectKind => EffectQueue.Count == 0 ? (EffectStepKind?)null : EffectQueue[0].Kind;
        internal List<EffectStep> EffectQueue { get; } = new List<EffectStep>();

        internal IDeterministicRandom Rng { get; }
        internal Dictionary<CardInstanceId, CardInstance> Cards { get; }
        internal PlayerState[] Players { get; }
        internal List<GameEvent> Events { get; }
        internal List<PendingEffect> PendingEffects { get; }
        internal List<CommandId> AcceptedCommandIds { get; }
        internal int NextInstanceId { get; set; }

        public IReadOnlyList<PlayerState> AllPlayers => Players;
        public IReadOnlyList<GameEvent> EventLog => Events;
        public IReadOnlyList<PendingEffect> PendingEffectStack => PendingEffects;

        public PlayerState GetPlayer(PlayerId playerId)
        {
            return Players[playerId.Index];
        }

        /// <summary>
        /// Current RNG cursor. Normal match views must not display this; debug projection is opt-in.
        /// </summary>
        public RandomSnapshot SaveRng()
        {
            return Rng.Save();
        }

        public bool TryGetCard(CardInstanceId instanceId, out CardInstance card)
        {
            return Cards.TryGetValue(instanceId, out card);
        }

        internal GameEvent Emit(
            GameEventType type,
            PlayerId actorId,
            string payload,
            CardInstanceId sourceInstanceId = default,
            int parentEventIndex = -1,
            GameEventVisibility visibility = GameEventVisibility.Public)
        {
            var evt = new GameEvent(
                Events.Count,
                type,
                actorId,
                payload,
                sourceInstanceId,
                parentEventIndex,
                visibility);
            Events.Add(evt);
            return evt;
        }

        internal CardInstanceId AllocateInstanceId()
        {
            var id = new CardInstanceId(NextInstanceId);
            NextInstanceId++;
            return id;
        }

        internal bool HasAcceptedCommand(CommandId commandId)
        {
            for (var i = 0; i < AcceptedCommandIds.Count; i++)
            {
                if (AcceptedCommandIds[i] == commandId)
                {
                    return true;
                }
            }

            return false;
        }

        internal void RecordAcceptedCommand(CommandId commandId)
        {
            if (!HasAcceptedCommand(commandId))
            {
                AcceptedCommandIds.Add(commandId);
            }
        }
    }
}
