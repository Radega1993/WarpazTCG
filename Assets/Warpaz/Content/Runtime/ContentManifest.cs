using System.Collections.Generic;

namespace Warpaz.Content
{
    /// <summary>
    /// Loaded WZ1 content stamp. Expected constants describe the immutable alpha set;
    /// instance values come from <c>content_manifest.v0.1.json</c>.
    /// </summary>
    public sealed class ContentManifest
    {
        public const string CatalogFileName = "cards_wz1.v0.1.json";
        public const string ManifestFileName = "content_manifest.v0.1.json";
        public const int ExpectedCardCount = 120;
        public const int ExpectedIdentityCount = 17;
        public const int ExpectedMainDeckCount = 103;
        public const string FirstCardId = "WZ1-001";
        public const string LastCardId = "WZ1-120";
        public const string ExpectedSetId = "WZ1";
        public const string ExpectedContentVersion = "WZ1-v0.1";
        public const string ExpectedSchemaVersion = "0.1";

        public ContentManifest(
            string schemaVersion,
            string contentVersion,
            string setId,
            int declaredCardCount,
            string firstCardId,
            string lastCardId,
            int identityCount,
            int heroCount,
            int classCount,
            int mainDeckCardCount,
            string catalogFile,
            string sourceWorkbook,
            string sheet,
            string catalogSha256,
            IReadOnlyDictionary<string, int> typeCounts)
        {
            SchemaVersion = schemaVersion;
            ContentVersion = contentVersion;
            SetId = setId;
            DeclaredCardCount = declaredCardCount;
            FileFirstCardId = firstCardId;
            FileLastCardId = lastCardId;
            IdentityCount = identityCount;
            HeroCount = heroCount;
            ClassCount = classCount;
            MainDeckCardCount = mainDeckCardCount;
            CatalogFile = catalogFile;
            SourceWorkbook = sourceWorkbook;
            Sheet = sheet;
            CatalogSha256 = catalogSha256;
            TypeCounts = typeCounts ?? new Dictionary<string, int>();
        }

        public string SchemaVersion { get; }
        public string ContentVersion { get; }
        public string SetId { get; }
        public int DeclaredCardCount { get; }
        public string FileFirstCardId { get; }
        public string FileLastCardId { get; }
        public int IdentityCount { get; }
        public int HeroCount { get; }
        public int ClassCount { get; }
        public int MainDeckCardCount { get; }
        public string CatalogFile { get; }
        public string SourceWorkbook { get; }
        public string Sheet { get; }
        public string CatalogSha256 { get; }
        public IReadOnlyDictionary<string, int> TypeCounts { get; }
    }
}
