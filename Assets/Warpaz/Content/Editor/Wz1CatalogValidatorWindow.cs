using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Warpaz.Content;

namespace Warpaz.Content.Editor
{
    public sealed class Wz1CatalogValidatorWindow : EditorWindow
    {
        private CardCatalogLoadResult _result;
        private Vector2 _scroll;

        [MenuItem("WARPAZ/Content/Validate WZ1")]
        public static void ValidateWz1()
        {
            var result = LoadOfficialCatalog();
            var window = GetWindow<Wz1CatalogValidatorWindow>("Validate WZ1");
            window._result = result;
            window.Show();
            window.Focus();
            LogSummary(result);
        }

        private void OnGUI()
        {
            if (_result == null)
            {
                if (GUILayout.Button("Validate WZ1"))
                {
                    _result = LoadOfficialCatalog();
                    LogSummary(_result);
                }

                return;
            }

            var report = _result.Report;
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Content version", report != null ? report.ContentVersion : "—");
            EditorGUILayout.LabelField("Hash", report != null ? report.CatalogSha256 : "—");
            EditorGUILayout.LabelField("Total cards", report != null ? report.TotalCards.ToString() : "0");
            EditorGUILayout.LabelField("Identities", report != null ? report.IdentityCount.ToString() : "0");
            EditorGUILayout.LabelField("Main-deck cards", report != null ? report.MainDeckCount.ToString() : "0");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Counts by type", EditorStyles.boldLabel);
            if (report != null)
            {
                foreach (var pair in report.CountsByType)
                {
                    EditorGUILayout.LabelField(pair.Key, pair.Value.ToString());
                }
            }

            EditorGUILayout.Space();
            DrawIssues("Errors", report != null ? report.Errors : null, MessageType.Error);
            DrawIssues("Warnings", report != null ? report.Warnings : null, MessageType.Warning);
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Revalidate"))
            {
                _result = LoadOfficialCatalog();
                LogSummary(_result);
            }
        }

        private static void DrawIssues(
            string title,
            System.Collections.Generic.IReadOnlyList<ContentValidationIssue> issues,
            MessageType messageType)
        {
            var count = issues == null ? 0 : issues.Count;
            EditorGUILayout.LabelField(title + " (" + count + ")", EditorStyles.boldLabel);
            if (count == 0)
            {
                EditorGUILayout.LabelField("None.");
                return;
            }

            for (var i = 0; i < issues.Count; i++)
            {
                EditorGUILayout.HelpBox(issues[i].ToString(), messageType);
            }
        }

        private static CardCatalogLoadResult LoadOfficialCatalog()
        {
            return CardCatalogLoader.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Data"));
        }

        private static void LogSummary(CardCatalogLoadResult result)
        {
            var report = result != null ? result.Report : null;
            var total = report != null ? report.TotalCards : 0;
            var errors = report != null ? report.Errors.Count : 1;
            var builder = new StringBuilder();
            builder.Append(total);
            builder.Append("/");
            builder.Append(ContentManifest.ExpectedCardCount);
            builder.Append(", ");
            builder.Append(errors);
            builder.Append(errors == 1 ? " error" : " errors");
            if (errors == 0)
            {
                Debug.Log(builder.ToString());
                Debug.Log("WZ1 catalog validation: " + builder);
            }
            else
            {
                Debug.LogError("WZ1 catalog validation: " + builder);
                if (report != null)
                {
                    for (var i = 0; i < report.Errors.Count; i++)
                    {
                        Debug.LogError(report.Errors[i].ToString());
                    }
                }
            }
        }
    }
}
