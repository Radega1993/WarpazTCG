using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public sealed class BootstrapController : MonoBehaviour
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private GameObject loadingRoot;
        [SerializeField] private GameObject errorRoot;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text errorText;

        private void Start()
        {
            ApplyTheme();
            ShowLoading("Cargando WARPAZ TCG…");

            var dataDirectory = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Data");
            var rules = RuleConfigLoader.LoadFromDirectory(dataDirectory);
            var catalog = CardCatalogLoader.LoadFromDirectory(dataDirectory);

            if (!rules.Success || !catalog.Success || catalog.Catalog == null ||
                catalog.Catalog.Count != ContentManifest.ExpectedCardCount)
            {
                ShowError(BuildErrorMessage(rules, catalog));
                return;
            }

            Debug.Log(catalog.Catalog.Count + "/" + ContentManifest.ExpectedCardCount + ", 0 errors");

            var runtime = new AppRuntime(
                rules.Config,
                catalog.Catalog,
                catalog.Manifest,
                catalog.Report,
                UnityEngine.Application.version);

            var host = new GameObject("AppCompositionRoot");
            var root = host.AddComponent<AppCompositionRoot>();
            root.Initialize(runtime);
            root.OpenMainMenu();
        }

        private void ApplyTheme()
        {
            if (theme == null)
            {
                return;
            }

            theme.ApplyText(statusText);
            theme.ApplyText(errorText);
        }

        private void ShowLoading(string message)
        {
            if (loadingRoot != null)
            {
                loadingRoot.SetActive(true);
            }

            if (errorRoot != null)
            {
                errorRoot.SetActive(false);
            }

            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void ShowError(string message)
        {
            Debug.LogError(message);
            if (loadingRoot != null)
            {
                loadingRoot.SetActive(false);
            }

            if (errorRoot != null)
            {
                errorRoot.SetActive(true);
            }

            if (errorText != null)
            {
                errorText.text = message;
            }
        }

        private static string BuildErrorMessage(RuleConfigLoadResult rules, CardCatalogLoadResult catalog)
        {
            var builder = new StringBuilder();
            builder.AppendLine("No se pudo arrancar WARPAZ TCG.");
            builder.AppendLine();

            if (catalog != null && catalog.Report != null)
            {
                builder.AppendLine("Contenido: " + (catalog.Report.ContentVersion ?? "—"));
                builder.AppendLine("Hash: " + (catalog.Report.CatalogSha256 ?? "—"));
                builder.AppendLine("Cartas: " + catalog.Report.TotalCards + "/" + ContentManifest.ExpectedCardCount);
                builder.AppendLine();
                AppendIssues(builder, catalog.Report.Errors);
            }

            if (rules != null && rules.Errors != null)
            {
                AppendIssues(builder, rules.Errors);
            }

            if (catalog != null && catalog.Catalog != null &&
                catalog.Catalog.Count != ContentManifest.ExpectedCardCount)
            {
                builder.AppendLine("cardCount: Expected " + ContentManifest.ExpectedCardCount +
                    ", found " + catalog.Catalog.Count + ".");
            }

            return builder.ToString();
        }

        private static void AppendIssues(StringBuilder builder, System.Collections.Generic.IReadOnlyList<ContentValidationIssue> issues)
        {
            if (issues == null)
            {
                return;
            }

            for (var i = 0; i < issues.Count; i++)
            {
                builder.AppendLine(issues[i].ToString());
            }
        }
    }
}
