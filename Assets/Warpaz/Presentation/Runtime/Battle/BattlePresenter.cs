using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Warpaz.Application;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public sealed class BattlePresenter
    {
        private readonly GameSession _session;
        private readonly ICardCatalog _catalog;
        private readonly bool _debugVisibility;
        private readonly string _player0Name;
        private readonly string _player1Name;

        private int _viewerSeatIndex;
        private PlayerId _priorityBeforeCommand;
        private PlayerId _prioritySeat;
        private bool _handRevealed = true;
        private bool _passOverlayVisible;
        private PlayerId _pendingPrioritySeat;

        private bool _busy;
        private string _rejectionReason;

        private PlayerAreaView _playerArea;
        private HandView _handView;
        private MatchStatusView _statusView;
        private MulliganPanel _mulliganPanel;
        private EventLogView _eventLog;
        private PassDeviceOverlay _passOverlay;
        private BattleControlsView _controls;
        private BoardSlotsView _boardSlots;

        public BattlePresenter(
            GameSession session,
            ICardCatalog catalog,
            string player0Name,
            string player1Name,
            bool debugVisibility)
        {
            _session = session;
            _catalog = catalog;
            _player0Name = player0Name;
            _player1Name = player1Name;
            _debugVisibility = debugVisibility;
        }

        public void AttachViews(
            PlayerAreaView playerArea,
            HandView handView,
            MatchStatusView statusView,
            MulliganPanel mulliganPanel,
            EventLogView eventLog,
            PassDeviceOverlay passOverlay,
            BattleControlsView controls,
            BoardSlotsView boardSlots)
        {
            _playerArea = playerArea;
            _handView = handView;
            _statusView = statusView;
            _mulliganPanel = mulliganPanel;
            _eventLog = eventLog;
            _passOverlay = passOverlay;
            _controls = controls;
            _boardSlots = boardSlots;

            if (_mulliganPanel != null)
            {
                _mulliganPanel.Bind(OnConfirmMulligan);
            }

            if (_controls != null)
            {
                _controls.Bind(OnEndTurn);
            }

            if (_passOverlay != null)
            {
                _passOverlay.Bind(OnPassDeviceConfirmed);
            }
        }

        public void InitializeFromState()
        {
            if (_session.State == null)
            {
                return;
            }

            _prioritySeat = _session.State.PriorityPlayerId;
            _viewerSeatIndex = _prioritySeat.Index;
            _handRevealed = true;
            _passOverlayVisible = false;
            _rejectionReason = string.Empty;

            Render();
        }

        private MatchView BuildView()
        {
            var mode = _debugVisibility ? MatchViewMode.Debug : MatchViewMode.Normal;
            return _session.Project(_viewerSeatIndex, mode);
        }

        public void Render()
        {
            if (_session.State == null)
            {
                return;
            }

            _prioritySeat = _session.State.PriorityPlayerId;

            var view = BuildView();
            var state = _session.State;

            _boardSlots?.Render(_debugVisibility);
            _playerArea?.Render(view, state.ActivePlayerId, _prioritySeat, _catalog, _player0Name, _player1Name);
            _statusView?.Render(view, state.ActivePlayerId, _prioritySeat, _rejectionReason);
            _eventLog?.Render(view.Events, _rejectionReason);

            _passOverlayVisible = _passOverlayVisible && _handRevealed == false;
            _passOverlay?.Render(_passOverlayVisible, _pendingPrioritySeat, _player0Name, _player1Name);

            var showHand = _handRevealed && !_passOverlayVisible;
            _handView?.Render(view, showHand, _catalog);

            RenderMulliganPanel();
            RenderControls();
        }

        private void RenderMulliganPanel()
        {
            if (_mulliganPanel == null)
            {
                return;
            }

            var state = _session.State;
            var show = state.Phase == MatchPhase.MulliganP0 || state.Phase == MatchPhase.MulliganP1;
            if (show)
            {
                var actions = LegalActionQuery.For(state);
                _mulliganPanel.Render(actions, _prioritySeat, _busy && show, _passOverlayVisible);
            }
            else
            {
                _mulliganPanel.Hide();
            }
        }

        private void RenderControls()
        {
            if (_controls == null)
            {
                return;
            }

            if (_session.State.Phase != MatchPhase.Main)
            {
                _controls.Hide();
                return;
            }

            var actions = LegalActionQuery.For(_session.State);
            var canEnd = false;
            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.Type == CommandType.EndTurn && a.ActorId == _prioritySeat)
                {
                    canEnd = true;
                    break;
                }
            }

            _controls.Render(canEnd, _busy, _passOverlayVisible);
        }

        private void OnConfirmMulligan(MulliganDecision decision)
        {
            if (_busy || _session.State == null)
            {
                return;
            }

            _busy = true;
            _rejectionReason = string.Empty;
            _priorityBeforeCommand = _session.State.PriorityPlayerId;

            var result = _session.ConfirmMulligan(_priorityBeforeCommand, decision);
            _busy = false;

            if (!result.Accepted)
            {
                _rejectionReason = result.RejectionReason;
                Render();
                return;
            }

            var afterPriority = _session.State.PriorityPlayerId;
            if (afterPriority != _priorityBeforeCommand)
            {
                _handRevealed = false;
                _pendingPrioritySeat = afterPriority;
                _passOverlayVisible = true;
            }

            Render();
        }

        private void OnEndTurn()
        {
            if (_busy || _session.State == null)
            {
                return;
            }

            _busy = true;
            _rejectionReason = string.Empty;
            _priorityBeforeCommand = _session.State.PriorityPlayerId;

            var result = _session.EndTurn(_priorityBeforeCommand);
            _busy = false;

            if (!result.Accepted)
            {
                _rejectionReason = result.RejectionReason;
                Render();
                return;
            }

            var afterPriority = _session.State.PriorityPlayerId;
            if (afterPriority != _priorityBeforeCommand)
            {
                _handRevealed = false;
                _pendingPrioritySeat = afterPriority;
                _passOverlayVisible = true;
            }

            Render();
        }

        private void OnPassDeviceConfirmed()
        {
            _passOverlayVisible = false;
            _viewerSeatIndex = _pendingPrioritySeat.Index;
            _handRevealed = true;
            _rejectionReason = string.Empty;
            Render();
        }
    }
}
