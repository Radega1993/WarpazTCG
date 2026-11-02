using System.Collections;
using System.IO;
using UnityEngine;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Standalone harness: drives the guided tutorial and dumps PNGs (Overlay UI included).
    /// Enable with command line: -tutorialCapture
    /// </summary>
    public sealed class TutorialCaptureRunner : MonoBehaviour
    {
        public static bool IsRequested
        {
            get
            {
                var args = System.Environment.GetCommandLineArgs();
                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-tutorialCapture")
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private string _dir;
        private BattleCompositionRoot _root;
        private TutorialDirector _director;
        private GameSession _session;

        public static void TryStart(BattleCompositionRoot root)
        {
            if (!IsRequested || root == null)
            {
                return;
            }

            var go = new GameObject("TutorialCaptureRunner");
            DontDestroyOnLoad(go);
            var runner = go.AddComponent<TutorialCaptureRunner>();
            runner._root = root;
            runner.StartCoroutine(runner.Run());
        }

        private IEnumerator Run()
        {
            // Player: <project>/Builds/Linux/WarpazTCG_Data → up to repo root.
            _dir = Path.GetFullPath(Path.Combine(
                UnityEngine.Application.dataPath,
                "..",
                "..",
                "..",
                "Artifacts/UIValidation/TUTORIAL_WALKTHROUGH_v1/screenshots"));
            Directory.CreateDirectory(_dir);
            Debug.Log("TutorialCapture → " + _dir);

            // Wait for director bind.
            for (var i = 0; i < 120; i++)
            {
                _director = _root != null ? _root.GetComponent<TutorialDirector>() : null;
                _session = _root != null ? _root.Session : null;
                if (_director != null && _session != null)
                {
                    break;
                }

                yield return null;
            }

            if (_director == null || _session == null)
            {
                Debug.LogError("TutorialCapture: missing director/session");
                Quit(2);
                yield break;
            }

            yield return Shot(0, "00_welcome");
            _director.DebugClickPrimary();
            yield return Wait(0.4f);
            yield return Shot(1, "01_mulligan");

            var mull = _session.ConfirmMulligan(PlayerId.Player0, MulliganDecision.KeepHand);
            if (!mull.Accepted)
            {
                Fail("mulligan", mull.RejectionReason);
                yield break;
            }

            _root.RefreshViews();
            yield return WaitUntilMain(6f);
            _root.RefreshViews();
            yield return Wait(0.3f);
            yield return Shot(_director.CurrentStepIndex, "02_pa_intro");

            _director.DebugClickPrimary();
            yield return Wait(0.3f);
            yield return Shot(_director.CurrentStepIndex, "03_play_iniciado_prompt");

            _director.DebugClickPrimary();
            yield return Wait(0.2f);
            if (!TryPlay("WZ1-018", BoardRow.Vanguard, 0))
            {
                yield break;
            }

            yield return Wait(0.5f);
            yield return Shot(_director.CurrentStepIndex, "04_after_play_board_visible");

            _director.DebugClickPrimary();
            yield return Wait(0.2f);
            var end = _session.EndTurn(PlayerId.Player0);
            if (!end.Accepted)
            {
                Fail("endturn", end.RejectionReason);
                yield break;
            }

            _root.RefreshViews();
            yield return Wait(0.5f);
            yield return Shot(_director.CurrentStepIndex, "05_tutor_turn_observe");

            var t = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < t)
            {
                if (_session.State.PriorityPlayerId == PlayerId.Player0 &&
                    _session.State.Phase == MatchPhase.Main)
                {
                    break;
                }

                yield return null;
            }

            _root.RefreshViews();
            yield return Wait(0.3f);
            yield return Shot(_director.CurrentStepIndex, "06_tutor_done");
            _director.DebugClickPrimary();
            yield return Wait(0.3f);
            yield return Shot(_director.CurrentStepIndex, "07_play_recluta_prompt");

            _director.DebugClickPrimary();
            yield return Wait(0.2f);
            if (!TryPlay("WZ1-042", BoardRow.Rearguard, 0))
            {
                yield break;
            }

            yield return Wait(0.5f);
            yield return Shot(_director.CurrentStepIndex, "08_reorganize_prompt");

            _director.DebugClickPrimary();
            yield return Wait(0.2f);
            var from = FindOnBoard(PlayerId.Player0, "WZ1-042");
            var reorg = _session.ReorganizeMove(PlayerId.Player0, from, BoardRow.Vanguard, 1);
            if (!reorg.Accepted)
            {
                Fail("reorg", reorg.RejectionReason);
                yield break;
            }

            _root.RefreshViews();
            yield return Wait(0.5f);
            yield return Shot(_director.CurrentStepIndex, "09_attack_prompt");

            _director.DebugClickPrimary();
            yield return Wait(0.2f);
            var attacker = FindOnBoard(PlayerId.Player0, "WZ1-018");
            var target = FindOnBoard(PlayerId.Player1, "WZ1-050");
            var atk = _session.Attack(PlayerId.Player0, attacker, target);
            if (!atk.Accepted)
            {
                Fail("attack", atk.RejectionReason);
                yield break;
            }

            _root.RefreshViews();
            yield return Wait(0.5f);
            yield return Shot(_director.CurrentStepIndex, "10_resolution_concept");

            _director.DebugClickPrimary();
            yield return Wait(0.3f);
            yield return Shot(_director.CurrentStepIndex, "11_finish");

            File.WriteAllText(
                Path.Combine(_dir, "..", "evidence", "standalone_capture_ok.txt"),
                "ok\n");
            Debug.Log("TutorialCapture DONE");
            Quit(0);
        }

        private bool TryPlay(string cardId, BoardRow row, int slot)
        {
            var id = FindInHand(PlayerId.Player0, cardId);
            if (id.IsNone)
            {
                Fail("hand", cardId);
                return false;
            }

            var play = _session.Play(PlayerId.Player0, id, row, slot);
            if (!play.Accepted)
            {
                Fail("play " + cardId, play.RejectionReason);
                return false;
            }

            _root.RefreshViews();
            return true;
        }

        private IEnumerator Shot(int step, string label)
        {
            var path = Path.Combine(_dir, step.ToString("00") + "_" + label + ".png");
            ScreenCapture.CaptureScreenshot(path);
            // Give the player time to flush the PNG.
            yield return new WaitForSecondsRealtime(0.35f);
            Debug.Log("TutorialCapture shot " + path);
        }

        private IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private IEnumerator WaitUntilMain(float timeout)
        {
            var t = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < t)
            {
                var phase = _session.State.Phase;
                if (phase != MatchPhase.MulliganP0 && phase != MatchPhase.MulliganP1)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private CardInstanceId FindInHand(PlayerId playerId, string cardId)
        {
            var state = _session.State;
            var hand = state.GetPlayer(playerId).HandInstanceIds;
            for (var i = 0; i < hand.Count; i++)
            {
                if (state.TryGetCard(hand[i], out var card) && card.DefinitionId.Value == cardId)
                {
                    return hand[i];
                }
            }

            return CardInstanceId.None;
        }

        private CardInstanceId FindOnBoard(PlayerId playerId, string cardId)
        {
            var state = _session.State;
            var board = state.GetPlayer(playerId).BoardSlots;
            for (var i = 0; i < board.Count; i++)
            {
                if (board[i].Occupant.IsNone)
                {
                    continue;
                }

                if (state.TryGetCard(board[i].Occupant, out var card) &&
                    card.DefinitionId.Value == cardId)
                {
                    return board[i].Occupant;
                }
            }

            return CardInstanceId.None;
        }

        private void Fail(string where, string reason)
        {
            Debug.LogError("TutorialCapture FAIL @" + where + ": " + reason);
            Quit(3);
        }

        private static void Quit(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit(code);
#endif
        }
    }
}
