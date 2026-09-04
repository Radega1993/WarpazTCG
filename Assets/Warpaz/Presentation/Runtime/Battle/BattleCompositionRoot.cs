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

        public void Bind(AppRuntime runtime, ISceneFlow flow)
        {
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

            _presenter.AttachViews(
                playerArea,
                handView,
                statusView,
                mulliganPanel,
                eventLog,
                passOverlay,
                controls,
                boardSlots);

            _presenter.InitializeFromState();
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
