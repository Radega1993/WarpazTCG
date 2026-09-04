using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public sealed class MainMenuController : MonoBehaviour, IAppRuntimeClient
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private TMP_Text comingSoonText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button decksButton;
        [SerializeField] private Button cardsButton;
        [SerializeField] private Button laboratoryButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Dev: Battle payload")]
        [SerializeField] private ulong seed = 1UL;
        [SerializeField] private string player1Name = "Player 1";
        [SerializeField] private string player2Name = "Player 2";
        [SerializeField] private MulliganDecision mulliganDecision = MulliganDecision.KeepHand;
        [SerializeField] private bool debugVisibility = false;

        private AppRuntime _runtime;
        private ISceneFlow _flow;

        public void Bind(AppRuntime runtime, ISceneFlow flow)
        {
            _runtime = runtime;
            _flow = flow;
            ApplyTheme();
            if (titleText != null)
            {
                titleText.text = "WARPAZ TCG";
            }

            if (versionText != null && runtime != null)
            {
                var content = runtime.Manifest != null ? runtime.Manifest.ContentVersion : "—";
                var rules = runtime.Rules != null ? runtime.Rules.RuleVersion : "—";
                versionText.text = "Build " + runtime.BuildVersion + "  ·  Contenido " + content + "  ·  Reglas " + rules;
            }

            if (comingSoonText != null)
            {
                comingSoonText.text = string.Empty;
            }

            Wire(playButton, OnPlay);
            Wire(laboratoryButton, OnLaboratory);
            Wire(quitButton, OnQuit);
            Wire(decksButton, OnComingSoon);
            Wire(cardsButton, OnComingSoon);
            Wire(settingsButton, OnComingSoon);
        }

        public void SimulatePlay()
        {
            OnPlay();
        }

        public void SimulateComingSoon()
        {
            OnComingSoon();
        }

        private void ApplyTheme()
        {
            if (theme == null)
            {
                return;
            }

            theme.ApplyText(titleText);
            theme.ApplyText(versionText);
            theme.ApplyText(comingSoonText);
            if (comingSoonText != null)
            {
                comingSoonText.color = theme.Accent;
            }

            ApplyButton(playButton);
            ApplyButton(decksButton);
            ApplyButton(cardsButton);
            ApplyButton(laboratoryButton);
            ApplyButton(settingsButton);
            ApplyButton(quitButton);
        }

        private void ApplyButton(Button button)
        {
            if (button == null || theme == null)
            {
                return;
            }

            theme.ApplyButton(button, button.GetComponentInChildren<TMP_Text>(true));
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(handler);
        }

        private void OnPlay()
        {
            if (_flow == null || _runtime == null)
            {
                return;
            }

            var provider = _flow as IBattleStartProvider;

            // Fixture Sprint 1 usando catálogo real WZ1 (runtime.Catalog).
            var options = new Sprint1MatchFixtureOptions(
                seed: seed,
                player1Name: player1Name,
                player2Name: player2Name,
                mulliganDecision: mulliganDecision,
                debugVisibility: debugVisibility,
                rules: _runtime.Rules);
            var fixture = Sprint1MatchFixtureFactory.Create(_runtime.Catalog, options);

            var payload = new BattleStartPayload(
                fixture.ToMatchSetup(),
                options.MulliganDecision,
                options.DebugVisibility,
                options.Player1Name,
                options.Player2Name);

            provider?.SetPendingBattleStart(payload);

            _flow.OpenBattle();
        }

        private void OnLaboratory()
        {
            if (_flow != null)
            {
                _flow.OpenLaboratory();
            }
        }

        private void OnComingSoon()
        {
            if (comingSoonText != null)
            {
                comingSoonText.text = "Disponible próximamente";
            }
        }

        private void OnQuit()
        {
            Debug.Log("WARPAZ TCG: Exit requested.");
            UnityEngine.Application.Quit();
        }
    }
}
