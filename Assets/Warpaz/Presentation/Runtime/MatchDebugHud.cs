using UnityEngine;
using Warpaz.Application;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Isolated OnGUI table for SampleScene only. Disabled by default so Battle grey table stays the product path.
    /// </summary>
    public sealed class MatchDebugHud : MonoBehaviour
    {
        [SerializeField] private bool enableDebugHud;
        [SerializeField] private ulong seed = 1;

        private GameSession _session;
        private int _seatedPlayer;
        private bool _handoff;
        private string _status = string.Empty;

        private void Start()
        {
            if (!enableDebugHud)
            {
                enabled = false;
                return;
            }

            BeginMatch();
        }

        private void BeginMatch()
        {
            _session = new GameSession(MinimalCatalog.Create());
            var setup = new MatchSetup(
                seed,
                RuleConfig.AlphaDefaults(),
                MinimalCatalog.ContentVersionValue,
                new PlayerSetup(PlayerId.Player0, MinimalCatalog.HeroA, MinimalCatalog.ClassA, MinimalCatalog.StandardDeck40()),
                new PlayerSetup(PlayerId.Player1, MinimalCatalog.HeroB, MinimalCatalog.ClassB, MinimalCatalog.StandardDeck40()));
            var result = _session.Start(setup);
            _status = result.Accepted ? "Partida lista. Seed " + seed : result.RejectionReason;
            _seatedPlayer = 0;
            UpdateHandoff();
        }

        private void UpdateHandoff()
        {
            var view = CurrentView();
            if (view == null)
            {
                _handoff = false;
                return;
            }

            _handoff = view.PriorityPlayerId.Value != _seatedPlayer;
        }

        private void OnGUI()
        {
            const int pad = 12;
            GUILayout.BeginArea(new Rect(pad, pad, Screen.width - pad * 2, Screen.height - pad * 2));

            var view = CurrentView();
            if (view == null)
            {
                GUILayout.Label(_status);
                if (GUILayout.Button("Nueva partida"))
                {
                    BeginMatch();
                }

                GUILayout.EndArea();
                return;
            }

            if (_handoff)
            {
                DrawHandoff(view);
                GUILayout.EndArea();
                return;
            }

            DrawMatch(view);
            GUILayout.EndArea();
        }

        private void DrawHandoff(MatchView view)
        {
            var next = view.PriorityPlayerId.Value + 1;
            GUILayout.Label("Pasa el dispositivo");
            GUILayout.Label("Información privada del jugador " + next + ".");
            if (GUILayout.Button("Soy el jugador " + next))
            {
                _seatedPlayer = view.PriorityPlayerId.Value;
                _handoff = false;
            }
        }

        private void DrawMatch(MatchView view)
        {
            GUILayout.Label("WARPAZ TCG — mesa gris Sprint 1");
            GUILayout.Label("Seed " + view.Seed + "  Turno " + view.Turn + "  Fase " + view.Phase);
            GUILayout.Label("Activo P" + (view.ActivePlayerId.Value + 1) + "  Prioridad P" + (view.PriorityPlayerId.Value + 1));
            GUILayout.Label("Hash " + view.Hash);
            GUILayout.Label(_status);

            DrawPlayer("Tú (P" + (_seatedPlayer + 1) + ")", view.Viewer, true);
            DrawPlayer("Rival (P" + (1 - _seatedPlayer + 1) + ")", view.Opponent, false);

            GUILayout.Space(8);
            var seated = PlayerId.FromIndex(_seatedPlayer);
            var actions = LegalActionQuery.For(_session);
            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action.ActorId != seated)
                {
                    continue;
                }

                if (action.Type == CommandType.ConfirmMulligan)
                {
                    var keep = action.MulliganDecision == MulliganDecision.KeepHand;
                    var label = keep ? "Conservar mano" : "Mulligan (devolver toda la mano)";
                    if (GUILayout.Button(label))
                    {
                        Apply(_session.ConfirmMulligan(action.ActorId, action.MulliganDecision));
                    }
                }
                else if (action.Type == CommandType.EndTurn)
                {
                    if (GUILayout.Button("Fin de turno"))
                    {
                        Apply(_session.EndTurn(action.ActorId));
                    }
                }
            }

            if (GUILayout.Button("Nueva partida"))
            {
                BeginMatch();
            }
        }

        private static void DrawPlayer(string title, PlayerMatchView player, bool showHand)
        {
            GUILayout.Space(6);
            GUILayout.Label(title + "  PA " + player.PaCurrent + "/" + player.PaMax + "  Resolución " + player.Resolution);
            GUILayout.Label("Mazo " + player.DeckCount + "  Mano " + player.HandCount + "  Descarte " + player.Discard.Count);
            if (showHand)
            {
                foreach (var card in player.VisibleHand)
                {
                    GUILayout.Label("  " + card.CardId + " #" + card.InstanceId);
                }
            }
            else
            {
                GUILayout.Label("  Mano oculta");
            }
        }

        private MatchView CurrentView()
        {
            if (_session == null || _session.State == null)
            {
                return null;
            }

            return _session.Project(_seatedPlayer);
        }

        private void Apply(CommandResult result)
        {
            _status = result.Accepted ? result.Events.Count + " eventos" : result.RejectionReason;
            UpdateHandoff();
        }
    }
}
