using System.Collections.Generic;
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
        private readonly bool _tutorialSolo;
        private readonly PlayerId _learnerSeat;
        private ITutorialInputGate _tutorialGate;

        private int _viewerSeatIndex;
        private PlayerId _priorityBeforeCommand;
        private PlayerId _prioritySeat;
        private bool _handRevealed = true;
        private bool _passOverlayVisible;
        private PlayerId _pendingPrioritySeat;

        private bool _busy;
        private string _rejectionReason;

        private CardInstanceId _selectedHandId = CardInstanceId.None;
        private CardInstanceId _selectedBoardId = CardInstanceId.None;

        private PlayerAreaView _playerArea;
        private HandView _handView;
        private MatchStatusView _statusView;
        private MulliganPanel _mulliganPanel;
        private EventLogView _eventLog;
        private PassDeviceOverlay _passOverlay;
        private BattleControlsView _controls;
        private BoardSlotsView _boardSlots;
        private OpponentHandView _opponentHand;
        private PileStripView _localPiles;
        private PileStripView _opponentPiles;

        public BattlePresenter(
            GameSession session,
            ICardCatalog catalog,
            string player0Name,
            string player1Name,
            bool debugVisibility,
            bool tutorialSolo = false)
        {
            _session = session;
            _catalog = catalog;
            _player0Name = player0Name;
            _player1Name = player1Name;
            _debugVisibility = debugVisibility;
            _tutorialSolo = tutorialSolo;
            _learnerSeat = PlayerId.Player0;
        }

        public void SetTutorialGate(ITutorialInputGate gate)
        {
            _tutorialGate = gate;
        }

        /// <summary>
        /// After a tutor (or other) command submitted outside the click path, refresh pass/viewer rules.
        /// </summary>
        public void NotifyExternalCommand(PlayerId keepViewerSeat)
        {
            if (_session.State == null)
            {
                return;
            }

            ClearSelection();
            if (_tutorialSolo)
            {
                ApplyTutorialViewer(keepViewerSeat);
            }
            else
            {
                var afterPriority = _session.State.PriorityPlayerId;
                if (afterPriority.Index != _viewerSeatIndex)
                {
                    _handRevealed = false;
                    _pendingPrioritySeat = afterPriority;
                    _passOverlayVisible = true;
                }
            }

            Render();
        }

        public void AttachViews(
            PlayerAreaView playerArea,
            HandView handView,
            MatchStatusView statusView,
            MulliganPanel mulliganPanel,
            EventLogView eventLog,
            PassDeviceOverlay passOverlay,
            BattleControlsView controls,
            BoardSlotsView boardSlots,
            OpponentHandView opponentHand = null,
            PileStripView localPiles = null,
            PileStripView opponentPiles = null)
        {
            _playerArea = playerArea;
            _handView = handView;
            _statusView = statusView;
            _mulliganPanel = mulliganPanel;
            _eventLog = eventLog;
            _passOverlay = passOverlay;
            _controls = controls;
            _boardSlots = boardSlots;
            _opponentHand = opponentHand;
            _localPiles = localPiles;
            _opponentPiles = opponentPiles;

            if (_mulliganPanel != null)
            {
                _mulliganPanel.Bind(OnConfirmMulligan);
            }

            if (_controls != null)
            {
                _controls.Bind(OnPrimaryControl, OnDeclineInterception);
            }

            if (_passOverlay != null)
            {
                _passOverlay.Bind(OnPassDeviceConfirmed);
            }

            if (_handView != null)
            {
                _handView.Bind(OnHandCardClicked);
            }

            if (_boardSlots != null)
            {
                _boardSlots.Bind(OnBoardSlotClicked);
            }

            if (_playerArea != null)
            {
                _playerArea.BindRivalHero(OnRivalHeroClicked);
            }
        }

        public void InitializeFromState()
        {
            if (_session.State == null)
            {
                return;
            }

            _prioritySeat = _session.State.PriorityPlayerId;
            if (_tutorialSolo)
            {
                ApplyTutorialViewer(_learnerSeat);
            }
            else
            {
                _viewerSeatIndex = _prioritySeat.Index;
            }

            _handRevealed = true;
            _passOverlayVisible = false;
            _rejectionReason = string.Empty;
            ClearSelection();

            Render();
        }

        /// <summary>Bottom-of-screen seat. Tutorial never flips when the tutor acts.</summary>
        private PlayerId LocalSeat()
        {
            return _tutorialSolo ? _learnerSeat : PlayerId.FromIndex(_viewerSeatIndex);
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
            var localSeat = LocalSeat();

            var view = BuildView();
            var state = _session.State;
            var interactive = CanInteract();

            // Board/HUD orientation follows the local seat — not priority — so hotseat
            // flips only after Pass Device, and tutorial never flips.
            _boardSlots?.Render(
                view,
                localSeat,
                _catalog,
                interactive,
                _selectedBoardId,
                CollectLegalBoardTargets());
            _playerArea?.Render(
                view,
                state.ActivePlayerId,
                localSeat,
                _catalog,
                _player0Name,
                _player1Name);
            _statusView?.Render(view, state.ActivePlayerId, _prioritySeat, StatusMessage());
            _eventLog?.Render(view.Events, _rejectionReason);

            _passOverlayVisible = _passOverlayVisible && _handRevealed == false && !_tutorialSolo;
            _passOverlay?.Render(_passOverlayVisible, _pendingPrioritySeat, _player0Name, _player1Name);

            var showHand = _handRevealed && !_passOverlayVisible;
            _handView?.Render(
                view,
                showHand,
                _catalog,
                interactive && state.Phase == MatchPhase.Main,
                _selectedHandId,
                CollectPlayableHandIds());

            RenderSideChrome(view, localSeat);

            RenderMulliganPanel();
            RenderControls();
        }

        private void RenderSideChrome(MatchView view, PlayerId localSeat)
        {
            var local = localSeat == PlayerId.Player0 ? view.Player0 : view.Player1;
            var rival = localSeat == PlayerId.Player0 ? view.Player1 : view.Player0;

            _opponentHand?.Render(rival.HandCount);
            _localPiles?.Render(local.DeckCount, local.Discard.Count);
            _opponentPiles?.Render(rival.DeckCount, rival.Discard.Count);
        }

        private List<CardInstanceId> CollectPlayableHandIds()
        {
            var result = new List<CardInstanceId>();
            if (_session.State == null || _session.State.Phase != MatchPhase.Main)
            {
                return result;
            }

            var actions = LegalActionQuery.For(_session);
            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.ActorId != _prioritySeat)
                {
                    continue;
                }

                if (a.Type == CommandType.Play && !a.PrimaryInstanceId.IsNone)
                {
                    if (_tutorialGate != null && !_tutorialGate.AllowSelectHand(a.PrimaryInstanceId))
                    {
                        continue;
                    }

                    result.Add(a.PrimaryInstanceId);
                }
            }

            return result;
        }

        private string StatusMessage()
        {
            if (!string.IsNullOrEmpty(_rejectionReason))
            {
                return _rejectionReason;
            }

            if (_session.State != null && _session.State.Phase == MatchPhase.PendingChoice)
            {
                return "Intercepción: elige interceptor o Declinar";
            }

            if (!_selectedHandId.IsNone)
            {
                return "Selecciona casilla (o unidad a sustituir)";
            }

            if (!_selectedBoardId.IsNone)
            {
                return "Selecciona objetivo, casilla vacía u otra unidad (reorg)";
            }

            return string.Empty;
        }

        private List<CardInstanceId> CollectLegalBoardTargets()
        {
            var result = new List<CardInstanceId>();
            if (_session.State == null)
            {
                return result;
            }

            var actions = LegalActionQuery.For(_session);
            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.ActorId != _prioritySeat)
                {
                    continue;
                }

                if (!_selectedBoardId.IsNone)
                {
                    if (a.Type == CommandType.Attack &&
                        a.PrimaryInstanceId == _selectedBoardId &&
                        !a.TargetsHero &&
                        !a.SecondaryInstanceId.IsNone)
                    {
                        result.Add(a.SecondaryInstanceId);
                    }
                    else if (a.Type == CommandType.Reorganize &&
                             a.HasReorganizeMode &&
                             a.ReorganizeMode == ReorganizeMode.SwapRows &&
                             a.PrimaryInstanceId == _selectedBoardId &&
                             !a.SecondaryInstanceId.IsNone)
                    {
                        result.Add(a.SecondaryInstanceId);
                    }
                }

                if (a.Type == CommandType.ChooseTargets && !a.PrimaryInstanceId.IsNone)
                {
                    result.Add(a.PrimaryInstanceId);
                }
            }

            return result;
        }

        private bool CanInteract()
        {
            if (_busy || _passOverlayVisible || !_handRevealed || _session.State == null)
            {
                return false;
            }

            if (_tutorialSolo && _session.State.PriorityPlayerId != _learnerSeat)
            {
                return false;
            }

            return true;
        }

        private void ApplyTutorialViewer(PlayerId keepViewerSeat)
        {
            _viewerSeatIndex = keepViewerSeat.Index;
            _handRevealed = true;
            _passOverlayVisible = false;
            _pendingPrioritySeat = keepViewerSeat;
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
                var actions = LegalActionQuery.For(_session);
                var allowReplace = _tutorialGate == null ||
                                   _tutorialGate.AllowMulligan(MulliganDecision.ReplaceWholeHand);
                _mulliganPanel.Render(
                    actions,
                    _prioritySeat,
                    _busy && show,
                    _passOverlayVisible,
                    allowReplace);
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

            var state = _session.State;
            if (state.Phase == MatchPhase.PendingChoice)
            {
                var canDecline = false;
                var actions = LegalActionQuery.For(_session);
                for (var i = 0; i < actions.Count; i++)
                {
                    var a = actions[i];
                    if (a.Type == CommandType.PassPriority && a.ActorId == _prioritySeat)
                    {
                        canDecline = true;
                        break;
                    }
                }

                _controls.RenderIntercept(canDecline, _busy, _passOverlayVisible);
                return;
            }

            if (state.Phase != MatchPhase.Main)
            {
                _controls.Hide();
                return;
            }

            var mainActions = LegalActionQuery.For(_session);
            var canEnd = false;
            for (var i = 0; i < mainActions.Count; i++)
            {
                var a = mainActions[i];
                if (a.Type == CommandType.EndTurn && a.ActorId == _prioritySeat)
                {
                    canEnd = true;
                    break;
                }
            }

            if (canEnd && _tutorialGate != null && !_tutorialGate.AllowEndTurn())
            {
                canEnd = false;
            }

            _controls.Render(canEnd, _busy, _passOverlayVisible);
        }

        private void OnHandCardClicked(CardInstanceId instanceId)
        {
            if (!CanInteract() || _session.State.Phase != MatchPhase.Main)
            {
                return;
            }

            if (_tutorialGate != null && !_tutorialGate.AllowSelectHand(instanceId))
            {
                _rejectionReason = "Tutorial: elige la carta indicada.";
                Render();
                return;
            }

            if (_selectedHandId == instanceId)
            {
                ClearSelection();
            }
            else
            {
                _selectedHandId = instanceId;
                _selectedBoardId = CardInstanceId.None;
            }

            _rejectionReason = string.Empty;
            Render();
        }

        private void OnBoardSlotClicked(PlayerId ownerId, int domainSlotIndex)
        {
            if (!CanInteract())
            {
                return;
            }

            var state = _session.State;
            if (state.Phase == MatchPhase.PendingChoice)
            {
                TryChooseInterceptor(ownerId, domainSlotIndex);
                return;
            }

            if (state.Phase != MatchPhase.Main)
            {
                return;
            }

            var slot = ResolveSlot(ownerId, domainSlotIndex);
            if (!slot.HasValue)
            {
                return;
            }

            var boardSlot = slot.Value;
            var isOwn = ownerId == _prioritySeat;

            if (!_selectedHandId.IsNone && isOwn)
            {
                if (boardSlot.Occupant.IsNone)
                {
                    if (_tutorialGate != null &&
                        !_tutorialGate.AllowPlay(_selectedHandId, boardSlot.Row, boardSlot.Index))
                    {
                        _rejectionReason = "Tutorial: casilla incorrecta.";
                        Render();
                        return;
                    }

                    TrySubmit(() => _session.Play(
                        _prioritySeat, _selectedHandId, boardSlot.Row, boardSlot.Index));
                }
                else
                {
                    // Replace is never part of the guided tutorial script.
                    if (_tutorialGate != null)
                    {
                        _rejectionReason = "Tutorial: elige una casilla vacía.";
                        Render();
                        return;
                    }

                    TrySubmit(() => _session.PlayReplace(
                        _prioritySeat, _selectedHandId, boardSlot.Occupant));
                }

                return;
            }

            if (!_selectedBoardId.IsNone)
            {
                if (!isOwn)
                {
                    if (!boardSlot.Occupant.IsNone)
                    {
                        if (_tutorialGate != null &&
                            !_tutorialGate.AllowAttack(_selectedBoardId, boardSlot.Occupant))
                        {
                            _rejectionReason = "Tutorial: objetivo de ataque incorrecto.";
                            Render();
                            return;
                        }

                        TrySubmit(() => _session.Attack(
                            _prioritySeat, _selectedBoardId, boardSlot.Occupant));
                    }

                    return;
                }

                if (boardSlot.Occupant.IsNone)
                {
                    if (_tutorialGate != null &&
                        !_tutorialGate.AllowReorganize(_selectedBoardId, boardSlot.Row, boardSlot.Index))
                    {
                        _rejectionReason = "Tutorial: mueve a la casilla indicada.";
                        Render();
                        return;
                    }

                    TrySubmit(() => _session.ReorganizeMove(
                        _prioritySeat, _selectedBoardId, boardSlot.Row, boardSlot.Index));
                    return;
                }

                if (boardSlot.Occupant == _selectedBoardId)
                {
                    ClearSelection();
                    Render();
                    return;
                }

                if (_tutorialGate != null)
                {
                    _rejectionReason = "Tutorial: no uses intercambio en este paso.";
                    Render();
                    return;
                }

                TrySubmit(() => _session.ReorganizeSwap(
                    _prioritySeat, _selectedBoardId, boardSlot.Occupant));
                return;
            }

            if (isOwn && !boardSlot.Occupant.IsNone)
            {
                if (_tutorialGate != null && !_tutorialGate.AllowSelectOwnBoard(boardSlot.Occupant))
                {
                    _rejectionReason = "Tutorial: selecciona la unidad indicada.";
                    Render();
                    return;
                }

                _selectedBoardId = boardSlot.Occupant;
                _selectedHandId = CardInstanceId.None;
                _rejectionReason = string.Empty;
                Render();
            }
        }

        private void OnRivalHeroClicked()
        {
            if (!CanInteract() || _session.State.Phase != MatchPhase.Main)
            {
                return;
            }

            if (_selectedBoardId.IsNone)
            {
                _rejectionReason = "Selecciona atacante primero";
                Render();
                return;
            }

            if (_tutorialGate != null && !_tutorialGate.AllowAttackHero(_selectedBoardId))
            {
                _rejectionReason = "Tutorial: este paso no ataca al Héroe.";
                Render();
                return;
            }

            TrySubmit(() => _session.AttackHero(_prioritySeat, _selectedBoardId));
        }

        private void TryChooseInterceptor(PlayerId ownerId, int domainSlotIndex)
        {
            var slot = ResolveSlot(ownerId, domainSlotIndex);
            if (!slot.HasValue || slot.Value.Occupant.IsNone)
            {
                return;
            }

            var interceptorId = slot.Value.Occupant;
            var actions = LegalActionQuery.For(_session);
            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.Type == CommandType.ChooseTargets &&
                    a.ActorId == _prioritySeat &&
                    a.PrimaryInstanceId == interceptorId)
                {
                    TrySubmit(() => _session.ChooseInterceptor(_prioritySeat, interceptorId));
                    return;
                }
            }
        }

        private BoardSlot? ResolveSlot(PlayerId ownerId, int domainSlotIndex)
        {
            var player = _session.State.GetPlayer(ownerId);
            if (domainSlotIndex < 0 || domainSlotIndex >= player.BoardSlots.Count)
            {
                return null;
            }

            return player.BoardSlots[domainSlotIndex];
        }

        private void OnConfirmMulligan(MulliganDecision decision)
        {
            if (_busy || _session.State == null)
            {
                return;
            }

            if (_tutorialGate != null && !_tutorialGate.AllowMulligan(decision))
            {
                _rejectionReason = "Tutorial: pulsa Conservar.";
                Render();
                return;
            }

            ClearSelection();
            TrySubmit(() => _session.ConfirmMulligan(_session.State.PriorityPlayerId, decision));
        }

        private void OnPrimaryControl()
        {
            if (_busy || _session.State == null)
            {
                return;
            }

            if (_session.State.Phase == MatchPhase.PendingChoice)
            {
                return;
            }

            if (_tutorialGate != null && !_tutorialGate.AllowEndTurn())
            {
                _rejectionReason = "Tutorial: aún no toca finalizar el turno.";
                Render();
                return;
            }

            ClearSelection();
            TrySubmit(() => _session.EndTurn(_session.State.PriorityPlayerId));
        }

        private void OnDeclineInterception()
        {
            if (_busy || _session.State == null || _session.State.Phase != MatchPhase.PendingChoice)
            {
                return;
            }

            TrySubmit(() => _session.DeclineInterception(_session.State.PriorityPlayerId));
        }

        private void TrySubmit(System.Func<CommandResult> submit)
        {
            _busy = true;
            _rejectionReason = string.Empty;
            _priorityBeforeCommand = _session.State.PriorityPlayerId;

            var result = submit();
            _busy = false;

            if (!result.Accepted)
            {
                _rejectionReason = result.RejectionReason;
                Render();
                return;
            }

            ClearSelection();

            var afterPriority = _session.State.PriorityPlayerId;
            if (_tutorialSolo)
            {
                ApplyTutorialViewer(_learnerSeat);
            }
            else if (afterPriority != _priorityBeforeCommand)
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
            ClearSelection();
            Render();
        }

        private void ClearSelection()
        {
            _selectedHandId = CardInstanceId.None;
            _selectedBoardId = CardInstanceId.None;
        }
    }
}
