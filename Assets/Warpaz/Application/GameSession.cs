using System.Collections.Generic;
using Warpaz.Domain;

namespace Warpaz.Application
{
    /// <summary>
    /// Owns the current <see cref="GameState"/>. Presentation reads views and submits commands;
    /// it cannot replace state or decide legality.
    /// </summary>
    public sealed class GameSession
    {
        private readonly CreateMatchUseCase _createMatch = new CreateMatchUseCase();
        private readonly SubmitCommandUseCase _submitCommand = new SubmitCommandUseCase();
        private readonly RulesEngine _engine;
        private readonly List<Command> _accepted = new List<Command>();
        private int _serial;

        public GameSession(ICardCatalog catalog)
        {
            Catalog = catalog;
            _engine = new RulesEngine(catalog);
        }

        public ICardCatalog Catalog { get; }

        public GameState State { get; private set; }

        public CommandResult LastResult { get; private set; }

        public IReadOnlyList<Command> AcceptedCommands => _accepted;

        public string CurrentHash => State == null ? string.Empty : GameStateHasher.Hash(State);

        public CommandResult Start(MatchSetup setup)
        {
            return _createMatch.Execute(this, setup);
        }

        public CommandResult Start(MatchSetupRequest request, RuleConfig rules)
        {
            return _createMatch.Execute(this, request, rules);
        }

        public CommandResult Submit(Command command)
        {
            return _submitCommand.Execute(this, command);
        }

        public CommandResult ConfirmMulligan(int playerId, bool keep)
        {
            return ConfirmMulligan(PlayerId.FromIndex(playerId), keep);
        }

        public CommandResult ConfirmMulligan(PlayerId playerId, bool keep)
        {
            return ConfirmMulligan(
                playerId,
                keep ? MulliganDecision.KeepHand : MulliganDecision.ReplaceWholeHand);
        }

        public CommandResult ConfirmMulligan(PlayerId playerId, MulliganDecision decision)
        {
            return Submit(Command.ConfirmMulligan(AllocateCommandId(), playerId, CurrentEventIndex(), decision));
        }

        public CommandResult EndTurn(int playerId)
        {
            return EndTurn(PlayerId.FromIndex(playerId));
        }

        public CommandResult EndTurn(PlayerId playerId)
        {
            return Submit(Command.EndTurn(AllocateCommandId(), playerId, CurrentEventIndex()));
        }

        public CommandResult Play(PlayerId playerId, CardInstanceId instanceId, BoardRow row, int slotIndex)
        {
            return Submit(Command.Play(AllocateCommandId(), playerId, CurrentEventIndex(), instanceId, row, slotIndex));
        }

        public CommandResult PlayReplace(PlayerId playerId, CardInstanceId instanceId, CardInstanceId replaceInstanceId)
        {
            return Submit(Command.PlayReplace(
                AllocateCommandId(), playerId, CurrentEventIndex(), instanceId, replaceInstanceId));
        }

        public CommandResult ReorganizeMove(PlayerId playerId, CardInstanceId instanceId, BoardRow row, int slotIndex)
        {
            return Submit(Command.ReorganizeMove(
                AllocateCommandId(), playerId, CurrentEventIndex(), instanceId, row, slotIndex));
        }

        public CommandResult ReorganizeSwap(PlayerId playerId, CardInstanceId firstId, CardInstanceId secondId)
        {
            return Submit(Command.ReorganizeSwap(
                AllocateCommandId(), playerId, CurrentEventIndex(), firstId, secondId));
        }

        public CommandResult Attack(PlayerId playerId, CardInstanceId attackerId, CardInstanceId targetId)
        {
            return Submit(Command.Attack(AllocateCommandId(), playerId, CurrentEventIndex(), attackerId, targetId));
        }

        public CommandResult AttackHero(PlayerId playerId, CardInstanceId attackerId)
        {
            return Submit(Command.AttackHero(AllocateCommandId(), playerId, CurrentEventIndex(), attackerId));
        }

        public CommandResult ChooseInterceptor(PlayerId playerId, CardInstanceId interceptorId)
        {
            return Submit(Command.ChooseInterceptor(
                AllocateCommandId(), playerId, CurrentEventIndex(), interceptorId));
        }

        public CommandResult DeclineInterception(PlayerId playerId)
        {
            return Submit(Command.DeclineInterception(AllocateCommandId(), playerId, CurrentEventIndex()));
        }

        public GameStateView ViewFor(int playerId)
        {
            return ViewFor(PlayerId.FromIndex(playerId));
        }

        public GameStateView ViewFor(PlayerId playerId)
        {
            return GameStateView.For(State, playerId);
        }

        public MatchView Project(int playerId, MatchViewMode mode = MatchViewMode.Normal)
        {
            return Project(PlayerId.FromIndex(playerId), mode);
        }

        public MatchView Project(PlayerId playerId, MatchViewMode mode = MatchViewMode.Normal)
        {
            return GameStateViewProjector.Project(State, playerId, mode);
        }

        public IReadOnlyList<LegalAction> LegalActions()
        {
            return LegalActionQuery.For(this);
        }

        internal CommandId AllocateCommandId()
        {
            _serial++;
            return CommandId.Parse("cmd-" + _serial);
        }

        internal void RememberResult(CommandResult result)
        {
            LastResult = result;
        }

        internal CommandResult ApplyCommand(Command command)
        {
            var result = _engine.Submit(State, command);
            LastResult = result;
            if (result.Accepted)
            {
                State = result.State;
                _accepted.Add(command);
            }

            return result;
        }

        private int CurrentEventIndex()
        {
            return State == null ? 0 : State.EventIndex;
        }
    }
}
