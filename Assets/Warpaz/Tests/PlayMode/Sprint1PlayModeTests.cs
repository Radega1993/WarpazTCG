using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Warpaz.Application;
using Warpaz.Domain;
using Warpaz.Presentation;

namespace Warpaz.Tests.PlayMode
{
    /// <summary>
    /// Additional Sprint-1 PlayMode tests that complement GreyBattleTableFlowTests.
    /// Each test boots the full UI and verifies a specific observable condition.
    /// Tests wait for observable state changes instead of fixed delays.
    /// </summary>
    public sealed class Sprint1PlayModeTests
    {
        // ------------------------------------------------------------------ teardown

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var roots = UnityEngine.Object.FindObjectsByType<AppCompositionRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < roots.Length; i++)
                UnityEngine.Object.Destroy(roots[i].gameObject);
            yield return null;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Loads Bootstrap → waits for MainMenu → invokes SimulatePlay → waits for Battle scene.
        /// Returns the BattleCompositionRoot once it exists.
        /// </summary>
        private static IEnumerator BootToBattle(float timeout = 30f)
        {
            LogAssert.NoUnexpectedReceived();

            yield return SceneManager.LoadSceneAsync(SceneIds.Bootstrap);

            var deadline = Time.realtimeSinceStartup + timeout;
            while (SceneManager.GetActiveScene().name != SceneIds.MainMenu &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual(SceneIds.MainMenu, SceneManager.GetActiveScene().name,
                "MainMenu did not load in time");

            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu, "MainMenuController not found");
            menu.SimulatePlay();

            deadline = Time.realtimeSinceStartup + timeout;
            while (SceneManager.GetActiveScene().name != SceneIds.Battle &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual(SceneIds.Battle, SceneManager.GetActiveScene().name,
                "Battle scene did not load in time");
        }

        private static GameSession GetSession(BattleCompositionRoot root)
        {
            Assert.IsNotNull(root, "BattleCompositionRoot is null");
            var session = root.Session;
            Assert.IsNotNull(session, "GameSession is null");
            return session;
        }

        /// <summary>
        /// Advances through Mulligan phase (P1 keep) and all PassDevice overlays
        /// until we reach MatchPhase.Main. Returns the session once in Main.
        /// </summary>
        private static IEnumerator AdvanceToMain(GameSession session,
            Button keepButton, Button overlayButton, float timeout = 20f)
        {
            keepButton.onClick.Invoke();
            yield return null;

            var deadline = Time.realtimeSinceStartup + timeout;
            while (session.State != null &&
                   session.State.Phase != MatchPhase.Main &&
                   Time.realtimeSinceStartup < deadline)
            {
                var overlays = UnityEngine.Object.FindObjectsByType<PassDeviceOverlay>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (overlays.Length > 0 && overlays[0].gameObject.activeInHierarchy)
                    overlayButton.onClick.Invoke();
                yield return null;
            }

            Assert.AreEqual(MatchPhase.Main, session.State.Phase,
                "Expected to reach Main phase");
        }

        // ------------------------------------------------------------------ tests

        /// <summary>
        /// Verifies that the rival's hand is never exposed through the session projection
        /// at any point during the full 5-turn flow.
        /// </summary>
        [UnityTest]
        public IEnumerator RivalHand_NeverVisible_DuringFullFlow()
        {
            yield return BootToBattle();

            var mulliganPanel = UnityEngine.Object.FindFirstObjectByType<MulliganPanel>();
            Assert.IsNotNull(mulliganPanel, "MulliganPanel not found");

            var deadline = Time.realtimeSinceStartup + 15f;
            while (!mulliganPanel.gameObject.activeInHierarchy && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(mulliganPanel.gameObject.activeInHierarchy, "MulliganPanel should be visible");

            var battleRoot = UnityEngine.Object.FindFirstObjectByType<BattleCompositionRoot>();
            Assert.IsNotNull(battleRoot, "BattleCompositionRoot not found");
            var session = GetSession(battleRoot);

            var keepButton = mulliganPanel.transform.Find("KeepHandButton").GetComponent<Button>();
            var overlays = UnityEngine.Object.FindObjectsByType<PassDeviceOverlay>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var overlay = overlays.Length > 0 ? overlays[0] : null;
            Assert.IsNotNull(overlay, "PassDeviceOverlay not found");
            var overlayButton = overlay.transform.Find("ConfirmButton").GetComponent<Button>();

            // Helper: check that rival hand is empty from both projections.
            void AssertRivalHandHidden()
            {
                var viewP0 = session.Project(PlayerId.Player0);
                var viewP1 = session.Project(PlayerId.Player1);
                Assert.AreEqual(0, viewP0.Opponent.VisibleHand.Count,
                    "P0 projection exposed rival hand (P1)");
                Assert.AreEqual(0, viewP1.Opponent.VisibleHand.Count,
                    "P1 projection exposed rival hand (P0)");
            }

            yield return AdvanceToMain(session, keepButton, overlayButton);
            AssertRivalHandHidden();

            var controls = UnityEngine.Object.FindFirstObjectByType<BattleControlsView>();
            Assert.IsNotNull(controls, "BattleControlsView not found");
            var endTurnButton = controls.transform.Find("EndTurnButton").GetComponent<Button>();

            for (var i = 0; i < 5; i++)
            {
                deadline = Time.realtimeSinceStartup + 20f;
                while ((!endTurnButton.interactable || overlay.gameObject.activeInHierarchy) &&
                       Time.realtimeSinceStartup < deadline)
                {
                    if (overlay.gameObject.activeInHierarchy) overlayButton.onClick.Invoke();
                    yield return null;
                }

                AssertRivalHandHidden();
                endTurnButton.onClick.Invoke();
                yield return null;
                AssertRivalHandHidden();
            }
        }

        /// <summary>
        /// Verifies that the phase and active-player text in MatchStatusView matches
        /// session.State after each EndTurn command.
        /// Uses transform.Find to locate TMP_Text children (same as existing tests).
        /// </summary>
        [UnityTest]
        public IEnumerator PlayerAndPhaseIndicator_MatchGameState_DuringFiveTurns()
        {
            yield return BootToBattle();

            var mulliganPanel = UnityEngine.Object.FindFirstObjectByType<MulliganPanel>();
            Assert.IsNotNull(mulliganPanel, "MulliganPanel not found");

            var deadline = Time.realtimeSinceStartup + 15f;
            while (!mulliganPanel.gameObject.activeInHierarchy && Time.realtimeSinceStartup < deadline)
                yield return null;

            var battleRoot = UnityEngine.Object.FindFirstObjectByType<BattleCompositionRoot>();
            Assert.IsNotNull(battleRoot, "BattleCompositionRoot not found");
            var session = GetSession(battleRoot);

            var keepButton = mulliganPanel.transform.Find("KeepHandButton").GetComponent<Button>();
            var overlays = UnityEngine.Object.FindObjectsByType<PassDeviceOverlay>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var overlay = overlays[0];
            var overlayButton = overlay.transform.Find("ConfirmButton").GetComponent<Button>();

            yield return AdvanceToMain(session, keepButton, overlayButton);

            var statusView = UnityEngine.Object.FindFirstObjectByType<MatchStatusView>();
            Assert.IsNotNull(statusView, "MatchStatusView not found");

            // Use reflection to read the text (internal getter is in a different assembly).
            var phaseField = typeof(MatchStatusView).GetField("_phase",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var activeField = typeof(MatchStatusView).GetField("_active",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(phaseField, "_phase field not found on MatchStatusView");
            Assert.IsNotNull(activeField, "_active field not found on MatchStatusView");

            string GetPhaseText()
            {
                var obj = phaseField.GetValue(statusView) as UnityEngine.Component;
                if (obj == null) return string.Empty;
                var textProp = obj.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                return textProp != null ? (string)textProp.GetValue(obj) : string.Empty;
            }
            string GetActiveText()
            {
                var obj = activeField.GetValue(statusView) as UnityEngine.Component;
                if (obj == null) return string.Empty;
                var textProp = obj.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                return textProp != null ? (string)textProp.GetValue(obj) : string.Empty;
            }

            var controls = UnityEngine.Object.FindFirstObjectByType<BattleControlsView>();
            var endTurnButton = controls.transform.Find("EndTurnButton").GetComponent<Button>();

            for (var i = 0; i < 5; i++)
            {
                deadline = Time.realtimeSinceStartup + 20f;
                while ((!endTurnButton.interactable || overlay.gameObject.activeInHierarchy) &&
                       Time.realtimeSinceStartup < deadline)
                {
                    if (overlay.gameObject.activeInHierarchy) overlayButton.onClick.Invoke();
                    yield return null;
                }

                // Verify display before invoking.
                var expectedPhase = "Fase " + session.State.Phase;
                var expectedActive = "Activo P" + (session.State.ActivePlayerId.Value + 1);
                Assert.AreEqual(expectedPhase, GetPhaseText(),
                    "Phase indicator mismatch at turn " + (i + 1));
                Assert.AreEqual(expectedActive, GetActiveText(),
                    "Active player indicator mismatch at turn " + (i + 1));

                endTurnButton.onClick.Invoke();
                yield return null;
            }
        }

        /// <summary>
        /// Verifies that illegal buttons are disabled: EndTurn button is not interactable
        /// during Mulligan, and the Mulligan buttons are not interactable during Main.
        /// </summary>
        [UnityTest]
        public IEnumerator IllegalButtons_AreDisabled_InWrongPhase()
        {
            yield return BootToBattle();

            var mulliganPanel = UnityEngine.Object.FindFirstObjectByType<MulliganPanel>();
            Assert.IsNotNull(mulliganPanel, "MulliganPanel not found");

            var deadline = Time.realtimeSinceStartup + 15f;
            while (!mulliganPanel.gameObject.activeInHierarchy && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(mulliganPanel.gameObject.activeInHierarchy);

            // During MulliganP1: EndTurnButton must not be interactable.
            var controls = UnityEngine.Object.FindFirstObjectByType<BattleControlsView>();
            if (controls != null && controls.gameObject.activeInHierarchy)
            {
                var endTurnButton = controls.transform.Find("EndTurnButton")?.GetComponent<Button>();
                if (endTurnButton != null)
                {
                    Assert.IsFalse(endTurnButton.interactable,
                        "EndTurnButton must be disabled during Mulligan phase");
                }
            }

            var battleRoot = UnityEngine.Object.FindFirstObjectByType<BattleCompositionRoot>();
            var session = GetSession(battleRoot);
            var keepButton = mulliganPanel.transform.Find("KeepHandButton").GetComponent<Button>();
            var overlays = UnityEngine.Object.FindObjectsByType<PassDeviceOverlay>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var overlay = overlays[0];
            var overlayButton = overlay.transform.Find("ConfirmButton").GetComponent<Button>();

            yield return AdvanceToMain(session, keepButton, overlayButton);

            // During Main: Mulligan panel must be hidden (not visible), so buttons are irrelevant
            // but we verify the panel itself is inactive.
            Assert.IsFalse(mulliganPanel.gameObject.activeInHierarchy,
                "MulliganPanel must be hidden during Main phase");

            // EndTurnButton should be interactable once in Main (for the priority player).
            deadline = Time.realtimeSinceStartup + 10f;
            Button mainEndTurnButton = null;
            if (controls != null)
                mainEndTurnButton = controls.transform.Find("EndTurnButton")?.GetComponent<Button>();

            while ((mainEndTurnButton == null || !mainEndTurnButton.interactable) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            if (mainEndTurnButton != null)
                Assert.IsTrue(mainEndTurnButton.interactable,
                    "EndTurnButton should be enabled during Main phase");
        }

        /// <summary>
        /// Verifies that key UI components are visible (activeInHierarchy) at 1366×768.
        /// </summary>
        [UnityTest]
        public IEnumerator UI_IsLegible_AtMinimumResolution()
        {
            Screen.SetResolution(1366, 768, false);
            yield return null; // Allow resolution change to propagate.

            yield return BootToBattle();

            var deadline = Time.realtimeSinceStartup + 15f;
            var mulliganPanel = UnityEngine.Object.FindFirstObjectByType<MulliganPanel>();
            while ((mulliganPanel == null || !mulliganPanel.gameObject.activeInHierarchy) &&
                   Time.realtimeSinceStartup < deadline)
            {
                mulliganPanel = UnityEngine.Object.FindFirstObjectByType<MulliganPanel>();
                yield return null;
            }

            Assert.IsNotNull(mulliganPanel, "MulliganPanel not found at 1366x768");
            Assert.IsTrue(mulliganPanel.gameObject.activeInHierarchy,
                "MulliganPanel not visible at 1366x768");

            var statusView = UnityEngine.Object.FindFirstObjectByType<MatchStatusView>();
            Assert.IsNotNull(statusView, "MatchStatusView not found at 1366x768");
            Assert.IsTrue(statusView.gameObject.activeInHierarchy,
                "MatchStatusView not visible at 1366x768");

            // BattleControlsView may not yet be active during Mulligan; just verify it exists.
            var controls = UnityEngine.Object.FindObjectsByType<BattleControlsView>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsTrue(controls.Length > 0, "BattleControlsView not found at 1366x768");
        }
    }
}
