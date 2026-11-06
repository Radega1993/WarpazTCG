using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Warpaz.Domain;

namespace Warpaz.Content
{
    /// <summary>Separate alpha format: only vanilla-playable Characters, never the full WZ1 promise.</summary>
    public static class StarterDeckCatalog
    {
        public const string FileName = "starters_wz1_alpha.json";

        public static IReadOnlyList<DeckList> Load(string path, ICardCatalog catalog, RuleConfig rules)
        {
            return Parse(File.ReadAllText(path), catalog, rules);
        }

        public static IReadOnlyList<DeckList> Parse(string json, ICardCatalog catalog, RuleConfig rules)
        {
            var root = JObject.Parse(json);
            if ((string)root["contentVersion"] != catalog.ContentVersion)
                throw new InvalidDataException("Starter contentVersion does not match catalog.");
            var result = new List<DeckList>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in (JArray)root["decks"])
            {
                var id = (string)item["id"];
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new InvalidDataException("Duplicate/missing starter id.");
                var main = new List<CardId>();
                foreach (var entry in ((JObject)item["cards"]).Properties())
                {
                    var count = (int)entry.Value;
                    var cardId = new CardId(entry.Name);
                    if (count < 1 || count > rules.MaxCopiesPerCard || !catalog.TryGet(cardId, out var card) || card.Type != CardType.Character)
                        throw new InvalidDataException("Invalid vanilla starter entry: " + entry.Name);
                    for (var i = 0; i < count; i++) main.Add(cardId);
                }
                var deck = new DeckList(id, (string)item["name"], new CardId((string)item["hero"]), new CardId((string)item["class"]), main);
                var validation = DeckValidator.Validate(deck, catalog, rules);
                if (!validation.IsValid) throw new InvalidDataException(id + ": " + validation.Reason);
                result.Add(deck);
            }
            if (result.Count != 3) throw new InvalidDataException("Expected three alpha starters.");
            return result.AsReadOnly();
        }
    }
}
