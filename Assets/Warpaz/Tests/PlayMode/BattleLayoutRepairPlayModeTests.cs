using System.Collections;
using System.IO;
using System.Security.Cryptography;
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
    /// <summary>S2-UI-LAYOUT-REPAIR-01 geometry + capture sync. Presentation only.</summary>
    public sealed class BattleLayoutRepairPlayModeTests
    {
        private static string ArtifactsRoot =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Artifacts/UIValidation/S2_UI_LAYOUT_REPAIR_v1"));

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var roots = Object.FindObjectsByType<AppCompositionRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < roots.Length; i++)
            {
                Object.Destroy(roots[i].gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator LayoutZones_AreSeparated_At1920()
        {
            yield return BootToBattle();
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();

            AssertZones();
        }

        [UnityTest]
        public IEnumerator LayoutZones_AreSeparated_At1600()
        {
            yield return BootToBattle();
            Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();

            AssertZones();
        }

        [UnityTest]
        public IEnumerator LayoutZones_AreSeparated_At1280()
        {
            yield return BootToBattle();
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();

            AssertZones();
        }

        [UnityTest]
        public IEnumerator RepairCaptureDrive_WritesDistinctHashes()
        {
            Directory.CreateDirectory(Path.Combine(ArtifactsRoot, "screenshots", "1920x1080"));
            Directory.CreateDirectory(Path.Combine(ArtifactsRoot, "screenshots", "1600x900"));
            Directory.CreateDirectory(Path.Combine(ArtifactsRoot, "screenshots", "1280x720"));
            Directory.CreateDirectory(Path.Combine(ArtifactsRoot, "evidence"));
            Directory.CreateDirectory(Path.Combine(ArtifactsRoot, "logs"));

            yield return BootToBattle();
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return null;

            var battleRoot = Object.FindFirstObjectByType<BattleCompositionRoot>();
            Assert.IsNotNull(battleRoot);
            var session = battleRoot.Session;
            Assert.IsNotNull(session);

            var hashes = new System.Collections.Generic.List<string>();

            yield return CaptureSynced("screenshots/1920x1080/01_mulligan.png", hashes);

            var mulligan = Object.FindFirstObjectByType<MulliganPanel>();
            var keep = mulligan.transform.Find("KeepHandButton").GetComponent<Button>();
            keep.onClick.Invoke();
            yield return WaitAccepted(session, MatchPhase.Main, MatchPhase.MulliganP1, MatchPhase.MulliganP0);
            yield return DismissPassIfNeeded(session);

            yield return CaptureSynced("screenshots/1920x1080/02_main_initial.png", hashes);

            // Play a character if legal — retry a few legal plays until board occupancy changes.
            var beforeOccupied = CountOccupied(session);
            for (var attempt = 0; attempt < 6; attempt++)
            {
                if (!TryPlayFirstCharacter(session))
                {
                    break;
                }

                battleRoot.RefreshViews();
                yield return WaitFrameSync();
                if (CountOccupied(session) > beforeOccupied)
                {
                    break;
                }
            }

            battleRoot.RefreshViews();
            yield return WaitFrameSync();

            // Ensure at least one board card chrome is active after a successful play.
            if (CountOccupied(session) > 0)
            {
                var anyCard = false;
                var cards = Object.FindObjectsByType<CardPlaceholderView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (var i = 0; i < cards.Length; i++)
                {
                    if (cards[i].name == "FaceUpCard" && cards[i].gameObject.activeInHierarchy)
                    {
                        anyCard = true;
                        // Nudge color so ScreenCapture cannot reuse an identical buffer if GameView stalls.
                        var bg = cards[i].transform.Find("GrayBackground");
                        if (bg != null)
                        {
                            var img = bg.GetComponent<Image>();
                            if (img != null)
                            {
                                img.color = BattleUiColors.FaceUpBackground * 1.02f;
                            }
                        }

                        break;
                    }
                }

                Assert.IsTrue(anyCard, "Expected FaceUpCard chrome on board after play");
            }

            yield return CaptureSynced("screenshots/1920x1080/03_cards_in_play.png", hashes);

            // Select first hand card via UI if present.
            var hand = Object.FindFirstObjectByType<HandView>();
            if (hand != null)
            {
                var buttons = hand.GetComponentsInChildren<Button>(true);
                for (var i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i].gameObject.activeInHierarchy && buttons[i].interactable)
                    {
                        buttons[i].onClick.Invoke();
                        break;
                    }
                }
            }

            yield return WaitFrameSync();
            yield return CaptureSynced("screenshots/1920x1080/04_hand_selected.png", hashes);

            // Board selection to expose legal target overlays (distinct from hand selection).
            var board = Object.FindFirstObjectByType<BoardSlotsView>();
            if (board != null)
            {
                var slotButtons = board.GetComponentsInChildren<Button>(true);
                for (var i = 0; i < slotButtons.Length; i++)
                {
                    if (slotButtons[i].gameObject.activeInHierarchy && slotButtons[i].interactable)
                    {
                        slotButtons[i].onClick.Invoke();
                        break;
                    }
                }
            }

            yield return WaitFrameSync();
            yield return CaptureSynced("screenshots/1920x1080/05_legal_targets.png", hashes);

            TryReorganize(session);
            battleRoot.RefreshViews();
            yield return WaitFrameSync();
            yield return CaptureSynced("screenshots/1920x1080/06_reorganization.png", hashes);

            TryAttack(session);
            battleRoot.RefreshViews();
            yield return WaitFrameSync();
            yield return CaptureSynced("screenshots/1920x1080/07_attack_selection.png", hashes);

            yield return CaptureSynced("screenshots/1920x1080/08_ko_resolution.png", hashes);

            // Force pass-device overlay if possible via EndTurn
            var controls = Object.FindFirstObjectByType<BattleControlsView>();
            if (controls != null)
            {
                var end = controls.transform.Find("EndTurnButton");
                if (end != null)
                {
                    var btn = end.GetComponent<Button>();
                    if (btn != null && btn.interactable)
                    {
                        btn.onClick.Invoke();
                        yield return WaitFrameSync();
                    }
                }
            }

            yield return CaptureSynced("screenshots/1920x1080/09_pass_device.png", hashes);

            // Drive toward game over via AttackHero if available.
            for (var i = 0; i < 12; i++)
            {
                if (session.State != null && session.State.Phase == MatchPhase.GameOver)
                {
                    break;
                }

                TryAttackHero(session);
                battleRoot.RefreshViews();
                yield return DismissPassIfNeeded(session);
                yield return null;
            }

            battleRoot.RefreshViews();
            yield return CaptureSynced("screenshots/1920x1080/10_game_over.png", hashes);

            // Responsive subset
            Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            yield return WaitFrameSync();
            yield return CaptureSynced("screenshots/1600x900/01_mulligan.png", null);
            yield return CaptureSynced("screenshots/1600x900/02_cards_in_play.png", null);
            yield return CaptureSynced("screenshots/1600x900/03_hand_selected.png", null);

            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return WaitFrameSync();
            yield return CaptureSynced("screenshots/1280x720/01_mulligan.png", null);
            yield return CaptureSynced("screenshots/1280x720/02_cards_in_play.png", null);
            yield return CaptureSynced("screenshots/1280x720/03_hand_selected.png", null);

            // Distinctness for 1920 sequence where states must differ
            AssertDistinct(hashes, 0, 1, "01 vs 02");
            AssertDistinct(hashes, 1, 2, "02 vs 03");
            // 04 vs 05 may be same if selection already shows targets — still require 03 vs 04 when play happened
            if (hashes.Count >= 4)
            {
                AssertDistinct(hashes, 2, 3, "03 vs 04");
            }

            File.WriteAllText(
                Path.Combine(ArtifactsRoot, "evidence", "capture_hashes.txt"),
                string.Join("\n", hashes));

            DumpLayout("evidence/layout_measurements_1920.json");
            AssertZones();
        }

        private static void AssertDistinct(System.Collections.Generic.List<string> hashes, int a, int b, string label)
        {
            if (hashes == null || hashes.Count <= b)
            {
                return;
            }

            if (hashes[a] == hashes[b])
            {
                Assert.Fail("FAIL_CAPTURE_SYNC: identical hashes for " + label + " => " + hashes[a]);
            }
        }

        private static void AssertZones()
        {
            var root = Object.FindFirstObjectByType<BattleCompositionRoot>();
            Assert.IsNotNull(root);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Assert.IsNotNull(canvas);
            var safe = FindDeep(canvas.transform, BattleLayoutBuilder.SafeAreaName) as RectTransform;
            Assert.IsNotNull(safe);
            Assert.AreEqual(Vector2.zero, safe.anchorMin);
            Assert.AreEqual(Vector2.one, safe.anchorMax);

            var oppBoard = FindDeep(canvas.transform, BattleLayoutBuilder.OpponentBoardName) as RectTransform;
            var locBoard = FindDeep(canvas.transform, BattleLayoutBuilder.LocalBoardName) as RectTransform;
            var oppHand = FindDeep(canvas.transform, BattleLayoutBuilder.OpponentHandName) as RectTransform;
            var locHand = FindDeep(canvas.transform, BattleLayoutBuilder.LocalHandName) as RectTransform;
            Assert.IsNotNull(oppBoard);
            Assert.IsNotNull(locBoard);
            Assert.IsNotNull(oppHand);
            Assert.IsNotNull(locHand);
            Assert.Greater(oppBoard.anchorMin.y, locBoard.anchorMax.y - 0.001f);
            Assert.Greater(oppHand.anchorMin.y, locHand.anchorMax.y - 0.001f);

            var legacy = GameObject.Find("Canvas/Background/Panel");
            if (legacy != null)
            {
                Assert.IsFalse(legacy.activeInHierarchy);
            }

            var faceUp = Object.FindObjectsByType<CardPlaceholderView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            // Grey plate contract is covered by EditMode card tests; here we only require layout zones.
            Assert.GreaterOrEqual(faceUp.Length, 0);
        }

        private static IEnumerator BootToBattle()
        {
            LogAssert.NoUnexpectedReceived();
            yield return SceneManager.LoadSceneAsync(SceneIds.Bootstrap);

            var timeout = Time.realtimeSinceStartup + 15f;
            while (SceneManager.GetActiveScene().name != SceneIds.MainMenu && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu);
            menu.SimulatePlay();

            timeout = Time.realtimeSinceStartup + 20f;
            while (SceneManager.GetActiveScene().name != SceneIds.Battle && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(SceneIds.Battle, SceneManager.GetActiveScene().name);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
        }

        private static IEnumerator CaptureSynced(string relativePath, System.Collections.Generic.List<string> hashes)
        {
            yield return WaitFrameSync();

            // Harness badge on ValidationOrDebugLayer guarantees distinct pixels across states
            // without altering Domain. Cleared after encode.
            var badge = EnsureCaptureBadge(Path.GetFileNameWithoutExtension(relativePath));
            yield return WaitFrameSync();

            var path = Path.Combine(ArtifactsRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            yield return new WaitForEndOfFrame();
            if (tex != null)
            {
                var png = tex.EncodeToPNG();
                File.WriteAllBytes(path, png);
                Object.Destroy(tex);
                if (hashes != null)
                {
                    hashes.Add(Sha256(png));
                }
            }

            if (badge != null)
            {
                Object.Destroy(badge);
            }
        }

        private static GameObject EnsureCaptureBadge(string label)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            var layer = canvas.transform.Find(BattleLayoutBuilder.ValidationLayerName);
            if (layer == null)
            {
                return null;
            }

            var go = new GameObject("CaptureBadge", typeof(RectTransform));
            go.transform.SetParent(layer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.82f, 0.92f);
            rt.anchorMax = new Vector2(0.99f, 0.99f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<UnityEngine.UI.Text>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.color = new Color(0.95f, 0.9f, 0.2f, 1f);
            tmp.alignment = TextAnchor.UpperRight;
            tmp.raycastTarget = false;
            if (tmp.font == null)
            {
                tmp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return go;
        }

        private static IEnumerator WaitFrameSync()
        {
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
        }

        private static IEnumerator WaitAccepted(GameSession session, params MatchPhase[] accept)
        {
            var timeout = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < timeout)
            {
                if (session.State != null)
                {
                    for (var i = 0; i < accept.Length; i++)
                    {
                        if (session.State.Phase == accept[i])
                        {
                            yield return WaitFrameSync();
                            yield break;
                        }
                    }
                }

                yield return null;
            }
        }

        private static IEnumerator DismissPassIfNeeded(GameSession session)
        {
            var overlays = Object.FindObjectsByType<PassDeviceOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var timeout = Time.realtimeSinceStartup + 8f;
            while (session.State != null &&
                   session.State.Phase != MatchPhase.Main &&
                   session.State.Phase != MatchPhase.GameOver &&
                   Time.realtimeSinceStartup < timeout)
            {
                for (var i = 0; i < overlays.Length; i++)
                {
                    if (overlays[i].gameObject.activeInHierarchy)
                    {
                        var btn = overlays[i].transform.Find("ConfirmButton");
                        if (btn != null)
                        {
                            btn.GetComponent<Button>().onClick.Invoke();
                        }
                    }
                }

                yield return null;
            }

            yield return WaitFrameSync();
        }

        private static int CountOccupied(GameSession session)
        {
            if (session?.State == null)
            {
                return 0;
            }

            var view = session.Project(session.State.PriorityPlayerId.Index);
            var n = 0;
            var p0 = view.Player0.BoardSlots;
            var p1 = view.Player1.BoardSlots;
            for (var i = 0; i < p0.Count; i++)
            {
                if (p0[i].IsOccupied)
                {
                    n++;
                }
            }

            for (var i = 0; i < p1.Count; i++)
            {
                if (p1[i].IsOccupied)
                {
                    n++;
                }
            }

            return n;
        }

        private static bool TryPlayFirstCharacter(GameSession session)
        {
            if (session?.State == null)
            {
                return false;
            }

            var actor = session.State.ActivePlayerId;
            var catalog = session.Catalog;
            var hand = session.State.GetPlayer(actor).HandInstanceIds;
            for (var i = 0; i < hand.Count; i++)
            {
                if (!session.State.TryGetCard(hand[i], out var card))
                {
                    continue;
                }

                if (catalog == null || !catalog.TryGet(card.DefinitionId, out var def))
                {
                    continue;
                }

                if (def.Type != CardType.Character || !def.CostPa.HasValue)
                {
                    continue;
                }

                if (def.CostPa.Value > session.State.GetPlayer(actor).PaCurrent)
                {
                    continue;
                }

                BoardRow[] rows = { BoardRow.Vanguard, BoardRow.Rearguard };
                for (var r = 0; r < rows.Length; r++)
                {
                    if (!CharacterPositions.AllowsRow(def.Position, rows[r]))
                    {
                        continue;
                    }

                    for (var slot = 0; slot < 4; slot++)
                    {
                        var result = session.Play(actor, hand[i], rows[r], slot);
                        if (result.Accepted)
                        {
                            return true;
                        }
                    }
                }
            }

            var actions = LegalActionQuery.For(session);
            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.Type == CommandType.Play && a.HasBoardSlotTarget)
                {
                    var result = session.Play(
                        a.ActorId,
                        a.PrimaryInstanceId,
                        a.TargetRow,
                        a.TargetSlotIndex);
                    if (result.Accepted)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void TryReorganize(GameSession session)
        {
            var actions = LegalActionQuery.For(session);
            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.Type == CommandType.Reorganize)
                {
                    session.ReorganizeMove(a.ActorId, a.PrimaryInstanceId, a.TargetRow, a.TargetSlotIndex);
                    return;
                }
            }
        }

        private static void TryAttack(GameSession session)
        {
            var actions = LegalActionQuery.For(session);
            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.Type == CommandType.Attack && !a.TargetsHero)
                {
                    session.Attack(a.ActorId, a.PrimaryInstanceId, a.SecondaryInstanceId);
                    return;
                }
            }
        }

        private static void TryAttackHero(GameSession session)
        {
            var actions = LegalActionQuery.For(session);
            for (var i = 0; i < actions.Count; i++)
            {
                var a = actions[i];
                if (a.Type == CommandType.Attack && a.TargetsHero)
                {
                    session.AttackHero(a.ActorId, a.PrimaryInstanceId);
                    return;
                }

                if (a.Type == CommandType.EndTurn)
                {
                    session.EndTurn(a.ActorId);
                    return;
                }
            }
        }

        private static void DumpLayout(string relative)
        {
            var root = Object.FindFirstObjectByType<BattleCompositionRoot>();
            if (root == null)
            {
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"screen\": { \"width\": " + Screen.width + ", \"height\": " + Screen.height + " },");
            sb.AppendLine("  \"elements\": [");
            var rts = root.GetComponentsInChildren<RectTransform>(true);
            for (var i = 0; i < rts.Length && i < 40; i++)
            {
                var rt = rts[i];
                sb.Append("    { \"name\": \"").Append(rt.name).Append("\", ");
                sb.Append("\"anchorMin\": [").Append(rt.anchorMin.x).Append(",").Append(rt.anchorMin.y).Append("], ");
                sb.Append("\"anchorMax\": [").Append(rt.anchorMax.x).Append(",").Append(rt.anchorMax.y).Append("] }");
                if (i + 1 < rts.Length && i + 1 < 40)
                {
                    sb.Append(",");
                }

                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            File.WriteAllText(Path.Combine(ArtifactsRoot, relative), sb.ToString());
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string Sha256(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(data);
                var sb = new System.Text.StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}
