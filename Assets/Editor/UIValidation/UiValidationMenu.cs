using System.IO;
using UnityEditor;
using UnityEngine;

namespace Warpaz.Editor.UIValidation
{
    /// <summary>
    /// Editor-only entry points for S2-UI-VALIDATION-01. Does not mutate production assets.
    /// </summary>
    public static class UiValidationMenu
    {
        public const string ArtifactsRootRelative = "Artifacts/UIValidation/S2_UI_VALIDATION_v1";

        [MenuItem("Warpaz/UI Validation/Open Artifacts Folder")]
        public static void OpenArtifactsFolder()
        {
            var path = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ArtifactsRootRelative));
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Warpaz/UI Validation/Write Expected Blocked States")]
        public static void WriteExpectedBlockedStates()
        {
            var root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ArtifactsRootRelative));
            var shot1920 = Path.Combine(root, "screenshots", "1920x1080");
            Directory.CreateDirectory(shot1920);
            Directory.CreateDirectory(Path.Combine(root, "screenshots", "1600x900"));
            Directory.CreateDirectory(Path.Combine(root, "screenshots", "1280x720"));

            WriteBlocked(
                Path.Combine(shot1920, "02_mulligan_selected_cards_BLOCKED.md"),
                "02_mulligan_selected_cards",
                "Per-card mulligan selection",
                "Product mulligan only exposes KeepHand / ReplaceWholeHand (MulliganPanel).",
                "No legal UI path to select individual hand cards during Mulligan.");

            WriteBlocked(
                Path.Combine(shot1920, "10_pending_intercept_BLOCKED.md"),
                "10_pending_intercept",
                "PendingDecision ChooseInterceptor",
                "P-HeroReach: Hero is legal only when defender Vanguard count is 0; interceptors are Ready Vanguard only.",
                "Legal AttackHero never opens PendingChoice; character attacks do not open intercept.");

            WriteBlocked(
                Path.Combine(shot1920, "11_intercept_declined_BLOCKED.md"),
                "11_intercept_declined",
                "DeclineInterception after PendingChoice",
                "Depends on reachable PendingChoice intercept window.",
                "Blocked by the same P-HeroReach policy as capture 10.");

            WriteBlocked(
                Path.Combine(root, "screenshots", "1600x900", "03_pending_intercept_BLOCKED.md"),
                "03_pending_intercept (1600x900)",
                "Pending intercept at 1600x900",
                "Same P-HeroReach unreachable window.",
                "No legal command path.");

            WriteBlocked(
                Path.Combine(root, "screenshots", "1280x720", "03_pending_intercept_BLOCKED.md"),
                "03_pending_intercept (1280x720)",
                "Pending intercept at 1280x720",
                "Same P-HeroReach unreachable window.",
                "No legal command path.");

            UnityEngine.Debug.Log("[UIValidation] Wrote expected BLOCKED markdown under " + root);
        }

        private static void WriteBlocked(
            string path,
            string stateId,
            string attempted,
            string commands,
            string reason)
        {
            var body =
                "# BLOCKED: " + stateId + "\n\n" +
                "- **Estado intentado:** " + attempted + "\n" +
                "- **Comandos / UI:** " + commands + "\n" +
                "- **Resultado:** No se generó PNG (no fabricar captura).\n" +
                "- **Motivo:** " + reason + "\n" +
                "- **Evidencia:** ADR 0004 / CombatPolicies / MulliganPanel; ver UI_VALIDATION_REPORT.md\n";
            File.WriteAllText(path, body);
        }
    }
}
