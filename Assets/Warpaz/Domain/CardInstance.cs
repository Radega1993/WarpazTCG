using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// A physical card in a match. Occupies exactly one <see cref="CardZone"/>.
    /// Visual data must not be stored here.
    /// </summary>
    public sealed class CardInstance
    {
        private readonly List<StatusType> _statuses;

        public CardInstance(
            CardInstanceId instanceId,
            CardId definitionId,
            PlayerId ownerId,
            PlayerId controllerId,
            CardZone zone)
        {
            if (instanceId.IsNone)
            {
                throw new ArgumentException("CardInstance requires an assigned CardInstanceId.", nameof(instanceId));
            }

            InstanceId = instanceId;
            DefinitionId = definitionId;
            OwnerId = ownerId;
            ControllerId = controllerId;
            Zone = zone;
            ReadyState = ReadyState.Ready;
            Slot = default;
            AttachedEquipmentId = CardInstanceId.None;
            _statuses = new List<StatusType>();
        }

        public CardInstanceId InstanceId { get; }
        public CardId DefinitionId { get; }
        public PlayerId OwnerId { get; }
        public PlayerId ControllerId { get; internal set; }
        public CardZone Zone { get; private set; }
        public ReadyState ReadyState { get; internal set; }
        public BoardSlot Slot { get; private set; }
        public bool HasBoardSlot => Zone == CardZone.Vanguard || Zone == CardZone.Rearguard;
        public CardInstanceId AttachedEquipmentId { get; internal set; }
        public IReadOnlyList<StatusType> Statuses => _statuses;

        internal void SetZone(CardZone zone)
        {
            Zone = zone;
            if (zone != CardZone.Vanguard && zone != CardZone.Rearguard)
            {
                Slot = default;
            }
        }

        internal void SetZone(CardZone zone, BoardSlot slot)
        {
            if (zone == CardZone.Vanguard && slot.Row != BoardRow.Vanguard)
            {
                throw new ArgumentException("Vanguard zone requires a Vanguard BoardSlot.", nameof(slot));
            }

            if (zone == CardZone.Rearguard && slot.Row != BoardRow.Rearguard)
            {
                throw new ArgumentException("Rearguard zone requires a Rearguard BoardSlot.", nameof(slot));
            }

            Zone = zone;
            Slot = slot;
        }

        internal void AddStatus(StatusType status)
        {
            if (!_statuses.Contains(status))
            {
                _statuses.Add(status);
            }
        }

        internal void RemoveStatus(StatusType status)
        {
            _statuses.Remove(status);
        }
    }
}
