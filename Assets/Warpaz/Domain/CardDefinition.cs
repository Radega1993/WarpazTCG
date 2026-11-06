using System;
using System.Collections.Generic;

namespace Warpaz.Domain
{
    /// <summary>
    /// Immutable catalog entry keyed by a stable WZ id. Effects are data, not
    /// a C# type per card. <see cref="Name"/> is never a lookup key.
    /// </summary>
    public sealed class CardDefinition
    {
        public CardDefinition(
            CardId cardId,
            string setId,
            int number,
            string name,
            CardType type,
            string subtype,
            string race,
            string classPath,
            int? costPa,
            int? power,
            int? response,
            string rarity,
            bool unique,
            string position,
            IReadOnlyList<string> keywords,
            string effectText,
            string flavor,
            string archetype,
            string canon,
            int? complexity,
            string balanceNotes,
            IReadOnlyList<EffectDefinition> effects)
        {
            CardId = cardId;
            SetId = setId;
            Number = number;
            Name = name;
            Type = type;
            Subtype = subtype;
            Race = race;
            ClassPath = classPath;
            CostPa = costPa;
            Power = power;
            Response = response;
            Rarity = rarity;
            Unique = unique;
            Position = position;
            Keywords = keywords ?? Array.Empty<string>();
            EffectText = effectText;
            Flavor = flavor;
            Archetype = archetype;
            Canon = canon;
            Complexity = complexity;
            BalanceNotes = balanceNotes;
            Effects = effects == null ? Array.Empty<EffectDefinition>() : Array.AsReadOnly(System.Linq.Enumerable.ToArray(effects));
        }

        public CardId CardId { get; }
        public string SetId { get; }
        public int Number { get; }
        public string Name { get; }
        public CardType Type { get; }
        public string Subtype { get; }
        public string Race { get; }
        public string ClassPath { get; }
        public int? CostPa { get; }
        public int? Power { get; }
        public int? Response { get; }
        public string Rarity { get; }
        public bool Unique { get; }
        public string Position { get; }
        public IReadOnlyList<string> Keywords { get; }
        public string EffectText { get; }
        public string Flavor { get; }
        public string Archetype { get; }
        public string Canon { get; }
        public int? Complexity { get; }
        public string BalanceNotes { get; }
        public IReadOnlyList<EffectDefinition> Effects { get; }

        public bool IsIdentity => Type == CardType.Hero || Type == CardType.ClassPath;
    }
}
