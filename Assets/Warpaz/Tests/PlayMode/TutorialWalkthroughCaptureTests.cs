using System.Collections;
using System.IO;
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
    /// Drives the guided tutorial end-to-end (goal-based) and writes screenshots.
    /// </summary>
    public sealed class TutorialWalkthroughCaptureTests
    {
        private static string ArtifactsRoot =>
            Path.GetFullPath(Path.Combine(
                UnityEngine.Application.dataPath,
                "..",
                "Artifacts/UIValidation/TUTORIAL_WALKTHROUGH_v1"));

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
        public IEnumerator Tutorial_FullWalkthrough_CapturesEachStep()
        {
            var shots = Path.Combine(ArtifactsRoot, "screenshots");
            Directory.CreateDirectory(shots);
            Directory.CreateDirectory(Path.Combine(ArtifactsRoot, "evidence"));

            yield return BootToTutorialBattle();
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            var root = Object.FindFirstObjectByType<BattleCompositionRoot>();
            Assert.IsNotNull(root);
            var session = root.Session;
            Assert.IsNotNull(session);
            var director = root.GetComponent<TutorialDirector>();
            Assert.IsNotNull(director, "TutorialDirector missing — tutorial payload not applied?");

            yield return Capture(shots, director.CurrentStepIndex, "00_welcome");
            yield return ClickThroughReadsUntil(director, TutorialGoal.MulliganKeep);
            yield return Capture(shots, director.CurrentStepIndex, "01_mulligan");

            var mull = session.ConfirmMulligan(PlayerId.Player0, MulliganDecision.KeepHand);
            Assert.IsTrue(mull.Accepted, mull.RejectionReason);
            root.RefreshViews();
            yield return Settle();
            yield return WaitUntilPhaseNotMulligan(session, 5f);
            root.RefreshViews();
            yield return Settle();
            yield return WaitUntilGoal(director, TutorialGoal.ReadAndContinue, 3f);
            yield return Capture(shots, director.CurrentStepIndex, "02_turn_structure");

            yield return ClickThroughReadsUntil(director, TutorialGoal.PlayExact);
            yield return Capture(shots, director.CurrentStepIndex, "03_play_vanguard_prompt");
            director.DebugClickPrimary();
            yield return Settle();
            Assert.IsTrue(director.IsActionUnlocked);

            var iniciado = FindInHand(session, PlayerId.Player0, "WZ1-018");
            Assert.IsFalse(iniciado.IsNone, "WZ1-018 not in learner hand");
            var play = session.Play(PlayerId.Player0, iniciado, BoardRow.Vanguard, 0);
            Assert.IsTrue(play.Accepted, play.RejectionReason);
            root.RefreshViews();
            yield return Settle();
            yield return WaitUntilGoal(director, TutorialGoal.ReadAndContinue, 2f);
            yield return Capture(shots, director.CurrentStepIndex, "04_recent_concept");

            yield return ClickThroughReadsUntil(director, TutorialGoal.EndTurn);
            yield return Capture(shots, director.CurrentStepIndex, "05_end_turn_prompt");
            director.DebugClickPrimary();
            yield return Settle();
            var end = session.EndTurn(PlayerId.Player0);
            Assert.IsTrue(end.Accepted, end.RejectionReason);
            root.RefreshViews();
            yield return Settle();
            yield return WaitUntilGoal(director, TutorialGoal.ObserveTutor, 3f);
            yield return Capture(shots, director.CurrentStepIndex, "06_tutor_observe");

            yield return WaitUntilLearnerMain(session, 8f);
            root.RefreshViews();
            yield return Settle();
            yield return Capture(shots, director.CurrentStepIndex, "07_tutor_done");
            director.DebugClickPrimary();
            yield return Settle();

            yield return ClickThroughReadsUntil(director, TutorialGoal.PlayExact);
            yield return Capture(shots, director.CurrentStepIndex, "08_play_rearguard_prompt");
            director.DebugClickPrimary();
            yield return Settle();
            var recluta = FindInHand(session, PlayerId.Player0, "WZ1-042");
            Assert.IsFalse(recluta.IsNone, "WZ1-042 not in learner hand");
            var play2 = session.Play(PlayerId.Player0, recluta, BoardRow.Rearguard, 0);
            Assert.IsTrue(play2.Accepted, play2.RejectionReason);
            root.RefreshViews();
            yield return Settle();
            yield return WaitUntilGoal(director, TutorialGoal.ReorganizeExact, 2f);
            yield return Capture(shots, director.CurrentStepIndex, "09_reorganize_prompt");

            director.DebugClickPrimary();
            yield return Settle();
            var from = FindOnBoard(session, PlayerId.Player0, "WZ1-042");
            Assert.IsFalse(from.IsNone);
            var reorg = session.ReorganizeMove(PlayerId.Player0, from, BoardRow.Vanguard, 1);
            Assert.IsTrue(reorg.Accepted, reorg.RejectionReason);
            root.RefreshViews();
            yield return Settle();
            yield return WaitUntilGoal(director, TutorialGoal.ReadAndContinue, 2f);
            yield return Capture(shots, director.CurrentStepIndex, "10_combat_rules");

            yield return ClickThroughReadsUntil(director, TutorialGoal.AttackExact);
            yield return Capture(shots, director.CurrentStepIndex, "11_attack_prompt");
            director.DebugClickPrimary();
            yield return Settle();
            var attacker = FindOnBoard(session, PlayerId.Player0, "WZ1-018");
            var target = FindOnBoard(session, PlayerId.Player1, "WZ1-050");
            Assert.IsFalse(attacker.IsNone);
            Assert.IsFalse(target.IsNone, "Tutor board target WZ1-050 missing");
            var atk = session.Attack(PlayerId.Player0, attacker, target);
            Assert.IsTrue(atk.Accepted, atk.RejectionReason);
            root.RefreshViews();
            yield return Settle();
            yield return WaitUntilGoal(director, TutorialGoal.ReadAndContinue, 2f);
            yield return Capture(shots, director.CurrentStepIndex, "12_resolution");

            // Victory path continues with more EndTurn / Play / AttackHero steps in play;
            // this capture harness stops after the core teaching loop.
            File.WriteAllText(
                Path.Combine(ArtifactsRoot, "evidence", "walkthrough_summary.txt"),
                "Tutorial walkthrough capture OK\nsteps=" + TutorialScript.Steps.Length +
                "\nstoppedAt=" + director.CurrentStepIndex + "\n");
        }

        private static IEnumerator ClickThroughReadsUntil(TutorialDirector director, TutorialGoal goal)
        {
            var timeout = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < timeout)
            {
                if (director.IsFinished)
                {
                    yield break;
                }

                if (director.CurrentGoalPublic == goal)
                {
                    yield break;
                }

                if (director.CurrentGoalPublic == TutorialGoal.ReadAndContinue ||
                    director.CurrentGoalPublic == TutorialGoal.ObserveTutor)
                {
                    // ObserveTutor needs tutor-done before primary works.
                    if (director.CurrentGoalPublic == TutorialGoal.ObserveTutor)
                    {
                        yield break;
                    }

                    director.DebugClickPrimary();
                    yield return Settle();
                    continue;
                }

                // Unexpected action goal before the one we want.
                Assert.AreEqual(goal, director.CurrentGoalPublic,
                    "Expected to reach " + goal + " but hit " + director.CurrentGoalPublic +
                    " at step " + director.CurrentStepIndex);
                yield break;
            }

            Assert.AreEqual(goal, director.CurrentGoalPublic, "Timeout waiting for goal " + goal);
        }

        private static IEnumerator WaitUntilGoal(TutorialDirector director, TutorialGoal goal, float seconds)
        {
            var timeout = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < timeout)
            {
                if (director.CurrentGoalPublic == goal || director.IsFinished)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private static IEnumerator WaitUntilPhaseNotMulligan(GameSession session, float seconds)
        {
            var timeout = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < timeout)
            {
                if (session.State.Phase != MatchPhase.MulliganP0 &&
                    session.State.Phase != MatchPhase.MulliganP1)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private static IEnumerator WaitUntilLearnerMain(GameSession session, float seconds)
        {
            var timeout = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < timeout)
            {
                if (session.State.PriorityPlayerId == PlayerId.Player0 &&
                    session.State.Phase == MatchPhase.Main)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private static IEnumerator BootToTutorialBattle()
        {
            yield return SceneManager.LoadSceneAsync(SceneIds.Bootstrap);
            var timeout = Time.realtimeSinceStartup + 20f;
            while (SceneManager.GetActiveScene().name != SceneIds.MainMenu &&
                   Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(SceneIds.MainMenu, SceneManager.GetActiveScene().name);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu);
            menu.SimulateTutorial();
            timeout = Time.realtimeSinceStartup + 20f;
            while (SceneManager.GetActiveScene().name != SceneIds.Battle &&
                   Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.AreEqual(SceneIds.Battle, SceneManager.GetActiveScene().name);
            yield return null;
            yield return null;
        }

        private static IEnumerator Capture(string dir, int step, string label)
        {
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;
            var path = Path.Combine(dir, step.ToString("00") + "_" + label + ".png");
            Directory.CreateDirectory(dir);

            LogAssert.ignoreFailingMessages = true;
            var tex = CaptureViaCamera();
            LogAssert.ignoreFailingMessages = false;

            Assert.IsNotNull(tex, "Screenshot texture was null for " + label);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
            Assert.IsTrue(File.Exists(path) && new FileInfo(path).Length > 500, "Screenshot not written: " + path);
            yield return null;
        }

        private static Texture2D CaptureViaCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindFirstObjectByType<Camera>();
            }

            if (cam == null)
            {
                return null;
            }

            var w = Mathf.Max(64, Screen.width);
            var h = Mathf.Max(64, Screen.height);
            var rt = new RenderTexture(w, h, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            RenderTexture.active = null;
            rt.Release();
            Object.Destroy(rt);
            return tex;
        }

        private static IEnumerator Settle()
        {
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        private static CardInstanceId FindInHand(GameSession session, PlayerId playerId, string cardId)
        {
            var state = session.State;
            var player = state.GetPlayer(playerId);
            var hand = player.HandInstanceIds;
            for (var i = 0; i < hand.Count; i++)
            {
                var id = hand[i];
                if (state.TryGetCard(id, out var card) && card.DefinitionId.Value == cardId)
                {
                    return id;
                }
            }

            return CardInstanceId.None;
        }

        private static CardInstanceId FindOnBoard(GameSession session, PlayerId playerId, string cardId)
        {
            var state = session.State;
            var player = state.GetPlayer(playerId);
            for (var i = 0; i < player.BoardSlots.Count; i++)
            {
                var slot = player.BoardSlots[i];
                if (slot.Occupant.IsNone)
                {
                    continue;
                }

                if (state.TryGetCard(slot.Occupant, out var card) && card.DefinitionId.Value == cardId)
                {
                    return slot.Occupant;
                }
            }

            return CardInstanceId.None;
        }
    }
}
