using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Warpaz.Domain;

namespace Warpaz.Content
{
    /// <summary>Versioned local drafts; corruption and concurrent writes fail closed.</summary>
    public sealed class DeckLibraryStore
    {
        private readonly string _path;
        private string _lastRead;
        private bool _loaded;
        public DeckLibraryStore(string path) { _path = path; }
        public List<DeckDraft> Load(IReadOnlyList<DeckList> starters)
        {
            if (!File.Exists(_path))
            {
                if (File.Exists(_path + ".bak")) throw new IOException("Falta la biblioteca principal; restaura la copia .bak antes de continuar.");
                _lastRead = null; _loaded = true;
                var seeded = starters.Select(d => new DeckDraft(d)).ToList(); Save(seeded); return seeded;
            }
            var json = File.ReadAllText(_path);
            var root = JObject.Parse(json);
            if ((int?)root["schema"] != 1 || !(root["decks"] is JArray items) || items.Count > 256)
                throw new InvalidDataException("Formato de biblioteca no compatible.");
            var ids = new HashSet<string>(); var result = new List<DeckDraft>();
            foreach (var item in items)
            {
                var id = (string)item["id"];
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id) || !(item["cards"] is JArray cards) || cards.Count > 500)
                    throw new InvalidDataException("Mazo dañado o duplicado.");
                result.Add(new DeckDraft(new DeckList(id, (string)item["name"], new CardId((string)item["hero"]),
                    new CardId((string)item["class"]), cards.Select(c => new CardId((string)c)).ToArray())));
            }
            _lastRead = json; _loaded = true; return result;
        }
        public void Save(IReadOnlyList<DeckDraft> decks)
        {
            if (!_loaded) throw new InvalidOperationException("Carga la biblioteca antes de guardar.");
            if (decks.Count > 256 || decks.Select(d => d.Id).Distinct().Count() != decks.Count)
                throw new InvalidDataException("Límite de 256 mazos o IDs duplicados.");
            var current = File.Exists(_path) ? File.ReadAllText(_path) : null;
            if (current != _lastRead) throw new IOException("La biblioteca cambió fuera del juego. Reabre el gestor antes de guardar.");
            var items = new JArray();
            foreach (var d in decks) items.Add(new JObject { ["id"] = d.Id, ["name"] = d.Name,
                ["hero"] = d.Hero.Value, ["class"] = d.Class.Value, ["cards"] = new JArray(d.Cards.Select(c => c.Value)) });
            var json = new JObject { ["schema"] = 1, ["decks"] = items }.ToString();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path)));
            var temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream)) { writer.Write(json); writer.Flush(); stream.Flush(true); }
                if (File.Exists(_path)) File.Replace(temp, _path, _path + ".bak"); else File.Move(temp, _path);
                _lastRead = json;
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
