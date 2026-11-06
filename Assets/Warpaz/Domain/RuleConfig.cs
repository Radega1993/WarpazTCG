using System;

namespace Warpaz.Domain
{
    /// <summary>
    /// Playtest knobs from specification v0.1 §2.3. The rules engine reads this
    /// object; Unity must not hard-code these values in MonoBehaviours.
    /// </summary>
    public sealed class RuleConfig
    {
        public const string SchemaVersion = "0.1";
        public const string AlphaRuleVersion = "0.1.0";
        public const string PreviewRuleVersion = "0.2.0-preview";
        public const string EffectsRuleVersion = "0.3.0-effects";
        public bool EffectsEnabled => RuleVersion == EffectsRuleVersion;
        public bool IsPreview => RuleVersion == PreviewRuleVersion || EffectsEnabled;
        public static RuleConfig EffectsDefaults() => PreviewDefaults(EffectsRuleVersion);
        public static RuleConfig PreviewDefaults(string version = PreviewRuleVersion)
        {
            var r = AlphaDefaults();
            return new RuleConfig(version, r.InitialHandSize, r.InitialResolution,
                r.DeckSize, r.MaxCopiesPerCard, r.InitialPaMax, r.PaIncreasePerTurn, r.PaLimit,
                r.AscensionRequiredPaMax, r.AscensionCost, r.MaximumEquipmentPerCharacter,
                r.FreeReorganizationsPerTurn, r.HandLimit, r.FailedDrawResolutionLoss,
                r.MaximumEventsPerTurn, r.MaximumDecisionsPerTurn);
        }
        public const int DefaultMaximumEventsPerTurn = 256;
        public const int DefaultMaximumDecisionsPerTurn = 64;

        public RuleConfig(
            string ruleVersion,
            int initialHandSize,
            int initialResolution,
            int deckSize,
            int maxCopiesPerCard,
            int initialPaMax,
            int paIncreasePerTurn,
            int paLimit,
            int ascensionRequiredPaMax,
            int ascensionCost,
            int maximumEquipmentPerCharacter,
            int freeReorganizationsPerTurn,
            int? handLimit,
            int failedDrawResolutionLoss,
            int maximumEventsPerTurn,
            int maximumDecisionsPerTurn)
        {
            if (string.IsNullOrWhiteSpace(ruleVersion))
            {
                throw new ArgumentException("ruleVersion is required.", nameof(ruleVersion));
            }

            if (initialHandSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialHandSize), "initialHandSize cannot be negative.");
            }

            if (initialResolution < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialResolution), "initialResolution cannot be negative.");
            }

            if (deckSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deckSize), "deckSize must be positive.");
            }

            if (maxCopiesPerCard <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCopiesPerCard), "maxCopiesPerCard must be positive.");
            }

            if (paLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(paLimit), "paLimit cannot be negative.");
            }

            if (initialPaMax < 0 || initialPaMax > paLimit)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialPaMax),
                    "initialPaMax must be between 0 and paLimit inclusive.");
            }

            if (paIncreasePerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(paIncreasePerTurn), "paIncreasePerTurn cannot be negative.");
            }

            if (ascensionRequiredPaMax < 0 || ascensionCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ascensionCost), "Ascension values cannot be negative.");
            }

            if (maximumEquipmentPerCharacter < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumEquipmentPerCharacter),
                    "maximumEquipmentPerCharacter cannot be negative.");
            }

            if (freeReorganizationsPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(freeReorganizationsPerTurn),
                    "freeReorganizationsPerTurn cannot be negative.");
            }

            if (failedDrawResolutionLoss < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(failedDrawResolutionLoss),
                    "failedDrawResolutionLoss cannot be negative.");
            }

            if (maximumEventsPerTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumEventsPerTurn),
                    "maximumEventsPerTurn must be a positive safety limit.");
            }

            if (maximumDecisionsPerTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumDecisionsPerTurn),
                    "maximumDecisionsPerTurn must be a positive safety limit.");
            }

            if (handLimit.HasValue && handLimit.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(handLimit), "handLimit cannot be negative.");
            }

            RuleVersion = ruleVersion;
            InitialHandSize = initialHandSize;
            InitialResolution = initialResolution;
            DeckSize = deckSize;
            MaxCopiesPerCard = maxCopiesPerCard;
            InitialPaMax = initialPaMax;
            PaIncreasePerTurn = paIncreasePerTurn;
            PaLimit = paLimit;
            AscensionRequiredPaMax = ascensionRequiredPaMax;
            AscensionCost = ascensionCost;
            MaximumEquipmentPerCharacter = maximumEquipmentPerCharacter;
            FreeReorganizationsPerTurn = freeReorganizationsPerTurn;
            HandLimit = handLimit;
            FailedDrawResolutionLoss = failedDrawResolutionLoss;
            MaximumEventsPerTurn = maximumEventsPerTurn;
            MaximumDecisionsPerTurn = maximumDecisionsPerTurn;
        }

        public string RuleVersion { get; }
        public int InitialHandSize { get; }
        public int InitialResolution { get; }
        public int DeckSize { get; }
        public int MaxCopiesPerCard { get; }
        public int InitialPaMax { get; }
        public int PaIncreasePerTurn { get; }
        public int PaLimit { get; }
        public int AscensionRequiredPaMax { get; }
        public int AscensionCost { get; }
        public int MaximumEquipmentPerCharacter { get; }
        public int FreeReorganizationsPerTurn { get; }
        public int? HandLimit { get; }
        public int FailedDrawResolutionLoss { get; }

        /// <summary>Safety cap against infinite effect loops (spec risk register).</summary>
        public int MaximumEventsPerTurn { get; }

        /// <summary>Safety cap against infinite decision windows.</summary>
        public int MaximumDecisionsPerTurn { get; }

        public static RuleConfig AlphaDefaults()
        {
            return new RuleConfig(
                ruleVersion: AlphaRuleVersion,
                initialHandSize: 5,
                initialResolution: 5,
                deckSize: 40,
                maxCopiesPerCard: 3,
                initialPaMax: 0,
                paIncreasePerTurn: 1,
                paLimit: 8,
                ascensionRequiredPaMax: 5,
                ascensionCost: 2,
                maximumEquipmentPerCharacter: 1,
                freeReorganizationsPerTurn: 1,
                handLimit: null,
                failedDrawResolutionLoss: 1,
                maximumEventsPerTurn: DefaultMaximumEventsPerTurn,
                maximumDecisionsPerTurn: DefaultMaximumDecisionsPerTurn);
        }
    }
}
