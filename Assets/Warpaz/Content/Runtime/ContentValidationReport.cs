using System.Collections.Generic;

namespace Warpaz.Content
{
    public sealed class ContentValidationIssue
    {
        public ContentValidationIssue(string cardId, string field, string message)
        {
            CardId = cardId;
            Field = field ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string CardId { get; }
        public string Field { get; }
        public string Message { get; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(CardId))
            {
                return Field + ": " + Message;
            }

            return CardId + " [" + Field + "]: " + Message;
        }
    }

    public sealed class ContentValidationReport
    {
        public ContentValidationReport(
            string contentVersion,
            string catalogSha256,
            int totalCards,
            IReadOnlyDictionary<string, int> countsByType,
            int identityCount,
            int mainDeckCount,
            IReadOnlyList<ContentValidationIssue> errors,
            IReadOnlyList<ContentValidationIssue> warnings)
        {
            ContentVersion = contentVersion;
            CatalogSha256 = catalogSha256;
            TotalCards = totalCards;
            CountsByType = countsByType ?? new Dictionary<string, int>();
            IdentityCount = identityCount;
            MainDeckCount = mainDeckCount;
            Errors = errors ?? System.Array.Empty<ContentValidationIssue>();
            Warnings = warnings ?? System.Array.Empty<ContentValidationIssue>();
        }

        public string ContentVersion { get; }
        public string CatalogSha256 { get; }
        public int TotalCards { get; }
        public IReadOnlyDictionary<string, int> CountsByType { get; }
        public int IdentityCount { get; }
        public int MainDeckCount { get; }
        public IReadOnlyList<ContentValidationIssue> Errors { get; }
        public IReadOnlyList<ContentValidationIssue> Warnings { get; }

        public bool IsValid => Errors.Count == 0;
    }
}
