using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Per-player match state. Zone lists expose instance ids without allowing callers to mutate them.
    /// </summary>
    public sealed class PlayerState
    {
        private readonly int _paLimit;
        private readonly int _resolutionCeiling;
        private readonly BoardSlot[] _boardSlots;
        private int _paCurrent;
        private int _paMax;
        private int _resolution;

        public PlayerState(
            PlayerId playerId,
            CardId heroId,
            CardId classPathId,
            CardInstanceId heroInstanceId,
            CardInstanceId classInstanceId,
            RuleConfig rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (!playerId.IsSeated)
            {
                throw new ArgumentException("PlayerState requires a seated PlayerId (0 or 1).", nameof(playerId));
            }

            PlayerId = playerId;
            HeroId = heroId;
            ClassPathId = classPathId;
            HeroInstanceId = heroInstanceId;
            ClassInstanceId = classInstanceId;
            _paLimit = rules.PaLimit;
            _resolutionCeiling = rules.InitialResolution;
            _resolution = rules.InitialResolution;
            _paMax = rules.InitialPaMax;
            _paCurrent = 0;
            EnsurePaInvariant();
            Deck = new List<CardInstanceId>();
            Hand = new List<CardInstanceId>();
            Discard = new List<CardInstanceId>();
            _boardSlots = new[]
            {
                BoardSlot.Vanguard(0),
                BoardSlot.Vanguard(1),
                BoardSlot.Rearguard(0),
                BoardSlot.Rearguard(1)
            };
        }

        public PlayerId PlayerId { get; }
        public CardId HeroId { get; }
        public CardId ClassPathId { get; }
        public CardInstanceId HeroInstanceId { get; }
        public CardInstanceId ClassInstanceId { get; }
        public bool Ascended { get; internal set; }
        public bool FreeReorganizationUsed { get; internal set; }

        public int Resolution
        {
            get { return _resolution; }
            internal set { SetResolution(value); }
        }

        public int PaCurrent
        {
            get { return _paCurrent; }
            internal set { SetPaCurrent(value); }
        }

        public int PaMax
        {
            get { return _paMax; }
            internal set { SetPaMax(value); }
        }

        internal List<CardInstanceId> Deck { get; }
        internal List<CardInstanceId> Hand { get; }
        internal List<CardInstanceId> Discard { get; }

        public IReadOnlyList<CardInstanceId> DeckInstanceIds => Deck;
        public IReadOnlyList<CardInstanceId> HandInstanceIds => Hand;
        public IReadOnlyList<CardInstanceId> DiscardInstanceIds => Discard;
        public IReadOnlyList<BoardSlot> BoardSlots => _boardSlots;

        internal void SetPaMax(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "PA maximum cannot be negative.");
            }

            if (value > _paLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "PA maximum " + value + " exceeds PaLimit " + _paLimit + ".");
            }

            _paMax = value;
            if (_paCurrent > _paMax)
            {
                _paCurrent = _paMax;
            }
        }

        internal void SetPaCurrent(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "PA current cannot be negative.");
            }

            if (value > _paMax)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "PA current " + value + " exceeds paMax " + _paMax + ".");
            }

            _paCurrent = value;
        }

        internal void SetResolution(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Resolution cannot be negative.");
            }

            if (value > _resolutionCeiling)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Resolution " + value + " exceeds initialResolution " + _resolutionCeiling + ".");
            }

            _resolution = value;
        }

        internal void SetBoardSlot(BoardSlot slot)
        {
            var offset = slot.Row == BoardRow.Vanguard ? 0 : BoardSlot.SlotsPerRow;
            _boardSlots[offset + slot.Index] = slot;
        }

        private void EnsurePaInvariant()
        {
            if (_paMax < 0 || _paMax > _paLimit)
            {
                throw new InvalidOperationException(
                    "Initial PA maximum is outside 0.." + _paLimit + ".");
            }

            if (_paCurrent < 0 || _paCurrent > _paMax)
            {
                throw new InvalidOperationException("Initial PA current is outside 0..paMax.");
            }
        }
    }
}
