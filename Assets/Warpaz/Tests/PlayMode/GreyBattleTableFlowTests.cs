using System;
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
    public sealed class GreyBattleTableFlowTests
    {
        [UnityTearDown]
        public System.Collections.IEnumerator TearDown()
        {
            var roots = UnityEngine.Object.FindObjectsByType<AppCompositionRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < roots.Length; i++)
            {
                UnityEngine.Object.Destroy(roots[i].gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public System.Collections.IEnumerator GreyBattleTable_FullFlowToTurn5()
        {
            LogAssert.NoUnexpectedReceived();

            yield return SceneManager.LoadSceneAsync(SceneIds.Bootstrap);

            var timeout = Time.realtimeSinceStartup + 15f;
            while (SceneManager.GetActiveScene().name != SceneIds.MainMenu && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(SceneIds.MainMenu, SceneManager.GetActiveScene().name);

            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu, "MainMenuController not found");

            // App starts match using the real fixture + payload flow.
            Assert.DoesNotThrow(() => menu.SimulatePlay());

            timeout = Time.realtimeSinceStartup + 20f;
            while (SceneManager.GetActiveScene().name != SceneIds.Battle && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(SceneIds.Battle, SceneManager.GetActiveScene().name);

            var mulliganPanel = UnityEngine.Object.FindFirstObjectByType<MulliganPanel>();
            Assert.IsNotNull(mulliganPanel, "MulliganPanel not found");

            // Wait until the auto-mulligan has advanced us to MulliganP1.
            timeout = Time.realtimeSinceStartup + 15f;
            while (!mulliganPanel.gameObject.activeInHierarchy && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.IsTrue(mulliganPanel.gameObject.activeInHierarchy, "MulliganPanel should be visible");

            var keepButton = mulliganPanel.transform.Find("KeepHandButton").GetComponent<Button>();
            Assert.IsNotNull(keepButton, "KeepHandButton not found");
            Assert.IsTrue(keepButton.interactable, "KeepHandButton should be enabled");

            ScreenCapture.CaptureScreenshot("GreyBattleTable_Mulligan.png");
            keepButton.onClick.Invoke();
            yield return null;

            var battleRoot = UnityEngine.Object.FindFirstObjectByType<BattleCompositionRoot>();
            Assert.IsNotNull(battleRoot, "BattleCompositionRoot not found");

            var sessionField = typeof(BattleCompositionRoot).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(sessionField, "BattleCompositionRoot._session field not found");

            var session = (GameSession)sessionField.GetValue(battleRoot);
            Assert.IsNotNull(session, "GameSession is null");

            var overlays = UnityEngine.Object.FindObjectsByType<PassDeviceOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsNotNull(overlays, "PassDeviceOverlay list is null");
            Assert.IsTrue(overlays.Length > 0, "PassDeviceOverlay not found");
            var overlay = overlays[0];

            var overlayButton = overlay.transform.Find("ConfirmButton").GetComponent<Button>();
            Assert.IsNotNull(overlayButton, "ConfirmButton not found");

            // Depending on RNG/priority, overlay may or may not appear. Advance until we reach Main.
            timeout = Time.realtimeSinceStartup + 20f;
            while (session.State != null && session.State.Phase != MatchPhase.Main && Time.realtimeSinceStartup < timeout)
            {
                if (overlay.gameObject.activeInHierarchy)
                {
                    overlayButton.onClick.Invoke();
                }

                yield return null;
            }

            Assert.AreEqual(MatchPhase.Main, session.State.Phase, "Expected to be in Main after mulligan flow v2");

            // Capture once we are back in Main (after pass device reveal).
            ScreenCapture.CaptureScreenshot("GreyBattleTable_Main.png");

            var controls = UnityEngine.Object.FindFirstObjectByType<BattleControlsView>();
            Assert.IsNotNull(controls, "BattleControlsView not found");

            var endTurnButton = controls.transform.Find("EndTurnButton").GetComponent<Button>();
            Assert.IsNotNull(endTurnButton, "EndTurnButton not found");

            for (var i = 0; i < 5; i++)
            {
                // EndTurn (esperar a que no estemos bloqueados por el overlay, si aparece)
                timeout = Time.realtimeSinceStartup + 20f;
                while ((!endTurnButton.interactable || overlay.gameObject.activeInHierarchy) && Time.realtimeSinceStartup < timeout)
                {
                    if (overlay.gameObject.activeInHierarchy)
                    {
                        overlayButton.onClick.Invoke();
                    }

                    yield return null;
                }

                Assert.IsTrue(endTurnButton.interactable, "EndTurnButton should be enabled");

                endTurnButton.onClick.Invoke();
                yield return null;

                // Esperar a que el siguiente EndTurn vuelva a estar habilitado (y que el overlay, si existe, se resuelva).
                timeout = Time.realtimeSinceStartup + 20f;
                while ((!endTurnButton.interactable || overlay.gameObject.activeInHierarchy) && Time.realtimeSinceStartup < timeout)
                {
                    if (overlay.gameObject.activeInHierarchy)
                    {
                        overlayButton.onClick.Invoke();
                    }

                    yield return null;
                }

                Assert.IsTrue(endTurnButton.interactable, "EndTurnButton should be enabled again");
            }

            // Verify engine reached Turn 6 and is in Main.
            Assert.AreEqual(MatchPhase.Main, session.State.Phase);
            Assert.AreEqual(6, session.State.Turn);
        }
    }
}
