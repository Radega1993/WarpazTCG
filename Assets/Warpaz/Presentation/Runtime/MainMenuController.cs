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

        public Button PlayButton => playButton;
        public Button DecksButton => decksButton;
        public Button CardsButton => cardsButton;
        public Button TutorialButton => laboratoryButton;
        public Button SettingsButton => settingsButton;
        public Button QuitButton => quitButton;

        public void Bind(AppRuntime runtime, ISceneFlow flow)
        {
            _runtime = runtime;
            _flow = flow;

            MainMenuLayoutBuilder.Ensure(this);

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
            Wire(laboratoryButton, OnTutorial);
            Wire(quitButton, OnQuit);
            Wire(decksButton, OnComingSoon);
            Wire(cardsButton, OnCards);
            Wire(settingsButton, OnComingSoon);

            // Standalone capture harness: jump straight into the tutorial.
            if (TutorialCaptureRunner.IsRequested)
            {
                OnTutorial();
            }
        }

        public void SimulatePlay()
        {
            OnPlay();
        }

        public void SimulateComingSoon()
        {
            OnComingSoon();
        }

        public void SimulateTutorial()
        {
            OnTutorial();
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

        private void OnTutorial()
        {
            if (_flow == null || _runtime == null)
            {
                return;
            }

            var provider = _flow as IBattleStartProvider;
            // Always use TutorialRules (short Resolution) — not free-play AlphaDefaults.
            var fixture = TutorialMatchFixture.Create(
                _runtime.Catalog,
                TutorialMatchFixture.TutorialRules());
            var payload = new BattleStartPayload(
                fixture.ToMatchSetup(),
                MulliganDecision.KeepHand,
                debugVisibility: false,
                player1Name: "Aprendiz",
                player2Name: "Tutor",
                isTutorial: true,
                skipAutoMulligan: true);
            provider?.SetPendingBattleStart(payload);
            _flow.OpenBattle();
        }

        private void OnCards()
        {
            if (_runtime == null || _runtime.Catalog == null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            if (comingSoonText != null)
            {
                comingSoonText.text = string.Empty;
            }

            CardsBrowserOverlay.Show(canvas.transform, _runtime.Catalog, onClose: null);
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
            AppWindowChrome.QuitApp();
        }
    }
}
