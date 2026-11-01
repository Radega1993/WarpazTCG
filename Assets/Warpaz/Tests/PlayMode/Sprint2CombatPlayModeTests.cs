using System.Collections;
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
    /// <summary>Sprint 2 PlayMode: board projection + session combat after Bootstrap→Battle.</summary>
    public sealed class Sprint2CombatPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var roots = Object.FindObjectsByType<AppCompositionRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < roots.Length; i++)
            {
                Object.Destroy(roots[i].gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Battle_BoardSlots_ProjectFourPerPlayer_AndCombatViaSession()
        {
            LogAssert.NoUnexpectedReceived();
            yield return SceneManager.LoadSceneAsync(SceneIds.Bootstrap);

            var deadline = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().name != SceneIds.MainMenu &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(SceneIds.MainMenu, SceneManager.GetActiveScene().name);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu);
            menu.SimulatePlay();

            deadline = Time.realtimeSinceStartup + 30f;
            while (SceneManager.GetActiveScene().name != SceneIds.Battle &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(SceneIds.Battle, SceneManager.GetActiveScene().name);

            var mulliganPanel = Object.FindFirstObjectByType<MulliganPanel>();
            Assert.IsNotNull(mulliganPanel);
            deadline = Time.realtimeSinceStartup + 15f;
            while (!mulliganPanel.gameObject.activeInHierarchy && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(mulliganPanel.gameObject.activeInHierarchy, "MulliganPanel should be visible");

            var battleRoot = Object.FindFirstObjectByType<BattleCompositionRoot>();
            Assert.IsNotNull(battleRoot);
            var session = battleRoot.Session;
            Assert.IsNotNull(session);

            var keepButton = mulliganPanel.transform.Find("KeepHandButton").GetComponent<Button>();
            Assert.IsNotNull(keepButton);

            var overlay = Object.FindFirstObjectByType<PassDeviceOverlay>(FindObjectsInactive.Include);
            Assert.IsNotNull(overlay);
            var overlayButton = overlay.transform.Find("ConfirmButton").GetComponent<Button>();

            keepButton.onClick.Invoke();
            yield return null;

            deadline = Time.realtimeSinceStartup + 20f;
            while (session.State != null &&
                   session.State.Phase != MatchPhase.Main &&
                   Time.realtimeSinceStartup < deadline)
            {
                if (overlay != null && overlay.gameObject.activeInHierarchy && overlayButton != null)
                {
                    overlayButton.onClick.Invoke();
                }

                yield return null;
            }

            Assert.AreEqual(MatchPhase.Main, session.State.Phase);

            var view = session.Project(session.State.PriorityPlayerId);
            Assert.AreEqual(4, view.Player0.BoardSlots.Count);
            Assert.AreEqual(4, view.Player1.BoardSlots.Count);

            var boardView = Object.FindFirstObjectByType<BoardSlotsView>();
            Assert.IsNotNull(boardView);
            Assert.IsTrue(boardView.gameObject.activeInHierarchy);

            var actor = session.State.ActivePlayerId;
            var catalog = session.Catalog;
            CardInstanceId characterId = CardInstanceId.None;
            var hand = session.State.GetPlayer(actor).HandInstanceIds;
            for (var i = 0; i < hand.Count; i++)
            {
                if (!session.State.TryGetCard(hand[i], out var card))
                {
                    continue;
                }

                if (catalog.TryGet(card.DefinitionId, out var def) &&
                    def.Type == CardType.Character &&
                    def.CostPa.HasValue &&
                    def.CostPa.Value <= session.State.GetPlayer(actor).PaCurrent &&
                    CharacterPositions.AllowsRow(def.Position, BoardRow.Vanguard))
                {
                    characterId = hand[i];
                    break;
                }
            }

            if (!characterId.IsNone)
            {
                var play = session.Play(actor, characterId, BoardRow.Vanguard, 0);
                Assert.IsTrue(play.Accepted, play.RejectionReason);
                yield return null;

                view = session.Project(session.State.PriorityPlayerId);
                var occupied = false;
                for (var i = 0; i < view.Viewer.BoardSlots.Count; i++)
                {
                    if (view.Viewer.BoardSlots[i].IsOccupied)
                    {
                        occupied = true;
                        break;
                    }
                }

                Assert.IsTrue(occupied, "Played character should appear on board projection");
            }

            Assert.AreEqual(0, FindObjectsWithMissingScripts());
        }

        private static int FindObjectsWithMissingScripts()
        {
            var count = 0;
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var components = transforms[i].GetComponents<Component>();
                for (var c = 0; c < components.Length; c++)
                {
                    if (components[c] == null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
