using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Warpaz.Editor.UIValidation
{
    /// <summary>S2-UI-LAYOUT-REPAIR-01 artifact helpers. Does not mutate Domain/Application.</summary>
    public static class UiLayoutRepairMenu
    {
        public const string ArtifactsRootRelative = "Artifacts/UIValidation/S2_UI_LAYOUT_REPAIR_v1";
        public const string ZipRelative = "Artifacts/UIValidation/WARPAZ_S2_UI_LAYOUT_REPAIR_v1.zip";

        [MenuItem("Warpaz/UI Layout Repair/Open Artifacts Folder")]
        public static void OpenArtifactsFolder()
        {
            var path = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ArtifactsRootRelative));
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Warpaz/UI Layout Repair/Build ZIP")]
        public static void BuildZip()
        {
            var root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ArtifactsRootRelative));
            var zipPath = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ZipRelative));
            Directory.CreateDirectory(root);
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(root, zipPath, System.IO.Compression.CompressionLevel.Optimal, true);
            var sha = Sha256File(zipPath);
            File.WriteAllText(Path.Combine(root, "ZIP_SHA256.txt"), sha + "  " + Path.GetFileName(zipPath) + "\n");
            Debug.Log("[UILayoutRepair] ZIP written: " + zipPath + " sha256=" + sha);
        }

        [MenuItem("Warpaz/UI Layout Repair/Write Stub Report Template")]
        public static void WriteStubReport()
        {
            var root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", ArtifactsRootRelative));
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(Path.Combine(root, "evidence"));
            File.WriteAllText(
                Path.Combine(root, "README.md"),
                "# S2_UI_LAYOUT_REPAIR_v1\n\nPresentation-only Battle UI layout repair evidence.\n");
            Debug.Log("[UILayoutRepair] Stub README at " + root);
        }

        private static string Sha256File(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}
