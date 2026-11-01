using System;
using System.Collections.Generic;
using UnityEngine;
using Warpaz.Application;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Composition root de la escena Battle. Puede ser MonoBehaviour.
    /// </summary>
    public sealed class BattleCompositionRoot : MonoBehaviour, IAppRuntimeClient
    {
        private AppRuntime _runtime;
        private ISceneFlow _flow;

        private GameSession _session;
        private BattlePresenter _presenter;
        private bool _layoutReady;

        /// <summary>Read-only seam for PlayMode tests and debug tools. Never replace state from UI.</summary>
        public GameSession Session => _session;

        /// <summary>Re-project session state into attached views (tests / harness).</summary>
        public void RefreshViews()
        {
            _presenter?.Render();
        }

        private void Awake()
        {
            EnsureBattleLayout();
        }

        public void Bind(AppRuntime runtime, ISceneFlow flow)
        {
            EnsureBattleLayout();

            _runtime = runtime;
            _flow = flow;

            var provider = flow as IBattleStartProvider;
            var payload = provider != null ? provider.ConsumePendingBattleStart() : null;
            if (payload == null)
            {
                payload = BuildFallbackPayload(runtime);
            }

            _session = new GameSession(runtime.Catalog);
            var start = _session.Start(payload.MatchSetupValue);
            if (!start.Accepted)
            {
                Debug.LogError("Battle start rejected: " + start.RejectionReason);
                return;
            }

            if (_session.State.Phase == MatchPhase.MulliganP0)
            {
                var mull = _session.ConfirmMulligan(PlayerId.Player0, payload.InitialMulliganForPlayer0);
                if (!mull.Accepted)
                {
                    Debug.LogError("Auto mulligan rejected: " + mull.RejectionReason);
                    return;
                }
            }

            _presenter = new BattlePresenter(
                _session,
                runtime.Catalog,
                payload.Player1Name,
                payload.Player2Name,
                payload.DebugVisibility);

            var playerArea = GetComponentInChildren<PlayerAreaView>(true);
            var handView = GetComponentInChildren<HandView>(true);
            var statusView = GetComponentInChildren<MatchStatusView>(true);
            var mulliganPanel = GetComponentInChildren<MulliganPanel>(true);
            var eventLog = GetComponentInChildren<EventLogView>(true);
            var passOverlay = GetComponentInChildren<PassDeviceOverlay>(true);
            var controls = GetComponentInChildren<BattleControlsView>(true);
            var boardSlots = GetComponentInChildren<BoardSlotsView>(true);
            var opponentHand = GetComponentInChildren<OpponentHandView>(true);
            var piles = GetComponentsInChildren<PileStripView>(true);

            // Layout may host views under Canvas SafeAreaRoot (sibling) when root lacks RectTransform.
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                playerArea = playerArea ?? canvas.GetComponentInChildren<PlayerAreaView>(true);
                handView = handView ?? canvas.GetComponentInChildren<HandView>(true);
                statusView = statusView ?? canvas.GetComponentInChildren<MatchStatusView>(true);
                mulliganPanel = mulliganPanel ?? canvas.GetComponentInChildren<MulliganPanel>(true);
                eventLog = eventLog ?? canvas.GetComponentInChildren<EventLogView>(true);
                passOverlay = passOverlay ?? canvas.GetComponentInChildren<PassDeviceOverlay>(true);
                controls = controls ?? canvas.GetComponentInChildren<BattleControlsView>(true);
                boardSlots = boardSlots ?? canvas.GetComponentInChildren<BoardSlotsView>(true);
                opponentHand = opponentHand ?? canvas.GetComponentInChildren<OpponentHandView>(true);
                if (piles == null || piles.Length == 0)
                {
                    piles = canvas.GetComponentsInChildren<PileStripView>(true);
                }
            }

            PileStripView localPiles = null;
            PileStripView opponentPiles = null;
            for (var i = 0; i < piles.Length; i++)
            {
                if (piles[i].name == BattleLayoutBuilder.LocalPilesName)
                {
                    localPiles = piles[i];
                }
                else if (piles[i].name == BattleLayoutBuilder.OpponentPilesName)
                {
                    opponentPiles = piles[i];
                }
            }

            if (localPiles == null && piles.Length > 0)
            {
                localPiles = piles[0];
            }

            if (opponentPiles == null && piles.Length > 1)
            {
                opponentPiles = piles[1];
            }

            _presenter.AttachViews(
                playerArea,
                handView,
                statusView,
                mulliganPanel,
                eventLog,
                passOverlay,
                controls,
                boardSlots,
                opponentHand,
                localPiles,
                opponentPiles);

            var binder = GetComponent<BattleBoardArtBinder>();
            binder?.Apply();

            _presenter.InitializeFromState();
        }

        private void EnsureBattleLayout()
        {
            if (_layoutReady)
            {
                return;
            }

            BattleLayoutBuilder.EnsureLayout(this);
            _layoutReady = true;
        }

        private static BattleStartPayload BuildFallbackPayload(AppRuntime runtime)
        {
            var options = new Sprint1MatchFixtureOptions(
                seed: 1UL,
                player1Name: "Player 1",
                player2Name: "Player 2",
                mulliganDecision: MulliganDecision.KeepHand,
                debugVisibility: false,
                rules: runtime.Rules);
            var fixture = Sprint1MatchFixtureFactory.Create(runtime.Catalog, options);
            return new BattleStartPayload(
                fixture.ToMatchSetup(),
                options.MulliganDecision,
                options.DebugVisibility,
                options.Player1Name,
                options.Player2Name);
        }
    }
}
