using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Immutable composition payload. Created by Bootstrap and injected into scenes.
    /// Not a service locator.
    /// </summary>
    public sealed class AppRuntime
    {
        public AppRuntime(
            RuleConfig rules,
            ICardCatalog catalog,
            ContentManifest manifest,
            ContentValidationReport catalogReport,
            string buildVersion)
        {
            Rules = rules;
            Catalog = catalog;
            Manifest = manifest;
            CatalogReport = catalogReport;
            BuildVersion = buildVersion ?? string.Empty;
        }

        public RuleConfig Rules { get; }
        public ICardCatalog Catalog { get; }
        public ContentManifest Manifest { get; }
        public ContentValidationReport CatalogReport { get; }
        public string BuildVersion { get; }
    }
}
