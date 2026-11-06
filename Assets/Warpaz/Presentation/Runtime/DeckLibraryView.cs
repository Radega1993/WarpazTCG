using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Content;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    /// <summary>Local deck library. Never mutates a live match.</summary>
    public sealed class DeckLibraryView : MonoBehaviour
    {
        private ICardCatalog _catalog;
        private RuleConfig _rules;
        private DeckLibraryStore _store;
        private List<DeckDraft> _decks;
        private Action<DeckList, DeckList> _start;
        private DeckDraft _draft;
        private string _query = "", _notice = "", _confirm;
        private CardType? _filter;
        private bool _onlyDeck, _dirty;
        private int _page;
        private string _first, _second;
        private Transform _body;
        private TMP_Text _status;

        public static void Show(Transform parent, ICardCatalog catalog, RuleConfig rules,
            IReadOnlyList<DeckList> starters, Action<DeckList, DeckList> start)
        {
            var existing = parent.GetComponentInChildren<DeckLibraryView>();
            if (existing != null) return;
            var store = new DeckLibraryStore(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "Decks", "library-v1.json"));
            var decks = store.Load(starters); // Fail before creating an unusable modal.
            var go = new GameObject("DeckLibrary", typeof(RectTransform), typeof(Image), typeof(DeckLibraryView));
            go.transform.SetParent(parent, false); Place(go.transform, 0, 0, 1, AppWindowChrome.ContentTop);
            go.GetComponent<Image>().color = new Color(.045f, .055f, .075f, 1);
            var view = go.GetComponent<DeckLibraryView>();
            view._catalog = catalog; view._rules = rules; view._store = store; view._decks = decks; view._start = start;
            view.Draw();
        }
        private void Draw()
        {
            if (_body != null) { _body.gameObject.SetActive(false); Destroy(_body.gameObject); }
            var body = new GameObject("Page", typeof(RectTransform)); body.transform.SetParent(transform, false);
            _body = body.transform; Place(_body, 0, 0, 1, 1);
            Label(_draft == null ? "MIS MAZOS · WZ1" : "EDITOR DE MAZO", .03f, .92f, .7f, .99f, 30);
            Button(_draft == null ? "Cerrar" : "Volver", .82f, .93f, .97f, .99f, Back);
            _status = Label(_notice, .03f, .01f, .97f, .07f, 16);
            if (_draft == null) DrawLibrary(); else DrawEditor();
        }
        private void Back()
        {
            if (_draft != null && _dirty && _confirm != "discard")
            { _confirm = "discard"; Notice("Cambios sin guardar. Pulsa Volver otra vez para descartarlos."); return; }
            if (_draft == null) Destroy(gameObject);
            else { _draft = null; _dirty = false; _confirm = null; _page = 0; _notice = ""; Draw(); }
        }
        private void DrawLibrary()
        {
            Label("v0.5: auras, Poder condicional y evasión. Solo efectos compilados; no todo WZ1 está implementado.", .03f, .84f, .97f, .91f, 18);
            Button("+ Nuevo mazo", .03f, .76f, .32f, .83f, () => {
                _draft = new DeckDraft(new DeckList(Guid.NewGuid().ToString("N"), "Nuevo mazo",
                    _catalog.All.First(c => c.Type == CardType.Hero).CardId,
                    _catalog.All.First(c => c.Type == CardType.ClassPath).CardId, Array.Empty<CardId>()));
                _dirty = true; _page = 0; _notice = ""; Draw();
            });
            _page = Mathf.Clamp(_page, 0, Math.Max(0, (_decks.Count - 1) / 6));
            for (int i = 0; i < 6 && i + _page * 6 < _decks.Count; i++)
            {
                var d = _decks[i + _page * 6]; var y = .68f - i * .075f;
                var valid = DeckValidator.Validate(d.ToDeck(), _catalog, _rules);
                Label(d.Name + "  ·  " + d.Cards.Count + "/40  ·  " + (valid.IsValid ? "Jugable" : "Borrador"), .03f, y, .58f, y + .065f, 20);
                Button("Editar", .59f, y, .7f, y + .06f, () => { _draft = new DeckDraft(d.ToDeck()); _page = 0; _dirty = false; _confirm = null; _notice = ""; Draw(); });
                Button("Copiar", .71f, y, .83f, y + .06f, () => Persist(() => _decks.Add(d.Duplicate())));
                Button("Eliminar", .84f, y, .97f, y + .06f, () => {
                    if (_confirm != d.Id) { _confirm = d.Id; Notice("Pulsa Eliminar otra vez para borrar «" + d.Name + "». Se conserva backup."); return; }
                    Persist(() => _decks.Remove(d)); _confirm = null;
                });
            }
            Pager(_decks.Count, 6, .21f);
            var playable = _decks.Where(d => DeckValidator.Validate(d.ToDeck(), _catalog, _rules).IsValid).ToList();
            if (playable.Count == 0) { Label("Guarda un mazo válido para jugar.", .03f, .08f, .97f, .19f, 20); return; }
            if (!playable.Any(d => d.Id == _first)) _first = playable[0].Id;
            if (!playable.Any(d => d.Id == _second)) _second = playable[Math.Min(1, playable.Count - 1)].Id;
            Button("J1: " + playable.First(d => d.Id == _first).Name, .03f, .09f, .36f, .17f, () => { _first = Next(playable, _first); Draw(); });
            Button("J2: " + playable.First(d => d.Id == _second).Name, .38f, .09f, .71f, .17f, () => { _second = Next(playable, _second); Draw(); });
            Button("Jugar", .74f, .09f, .97f, .17f, () => {
                try { _start(playable.First(d => d.Id == _first).ToDeck(), playable.First(d => d.Id == _second).ToDeck()); Destroy(gameObject); }
                catch (Exception ex) { Notice(ex.Message); }
            });
        }
        private static string Next(List<DeckDraft> decks, string id) => decks[(decks.FindIndex(d => d.Id == id) + 1) % decks.Count].Id;
        private void DrawEditor()
        {
            Input(_draft.Name, .03f, .84f, .48f, .91f, value => {
                if (value == _draft.Name) return;
                try { _draft.Rename(value); _dirty = true; _confirm = null; Notice("Nombre cambiado; pulsa Guardar."); }
                catch (Exception ex) { Notice(ex.Message); }
            });
            Button("Guardar", .51f, .84f, .72f, .91f, () => {
                if (Persist(() => { var index = _decks.FindIndex(d => d.Id == _draft.Id);
                    if (index < 0) _decks.Add(new DeckDraft(_draft.ToDeck())); else _decks[index] = new DeckDraft(_draft.ToDeck()); }))
                { _dirty = false; _confirm = null; Notice("Mazo guardado."); }
            });
            var validation = DeckValidator.Validate(_draft.ToDeck(), _catalog, _rules);
            Label(_draft.Cards.Count + "/40 · " + (validation.IsValid ? "Jugable" : "Borrador"), .74f, .84f, .97f, .91f, 19);
            Button("Héroe: " + Name(_draft.Hero), .03f, .76f, .48f, .82f, () => CycleIdentity(CardType.Hero));
            Button("Clase: " + Name(_draft.Class), .51f, .76f, .97f, .82f, () => CycleIdentity(CardType.ClassPath));
            Input(_query, .03f, .68f, .4f, .74f, value => { if (_query == value) return; _query = value; _page = 0; Draw(); });
            Button(_filter.HasValue ? CardTypeCatalog.ToLabel(_filter.Value) : "Todos los tipos", .42f, .68f, .69f, .74f, () => {
                var types = new CardType?[] { null, CardType.Character, CardType.Action, CardType.Reaction, CardType.Equipment };
                _filter = types[(Array.IndexOf(types, _filter) + 1) % types.Length]; _page = 0; Draw();
            });
            Button(_onlyDeck ? "Ver catálogo" : "Solo mi mazo", .71f, .68f, .97f, .74f, () => { _onlyDeck = !_onlyDeck; _page = 0; Draw(); });
            var cards = _catalog.All.Where(c => !c.IsIdentity && (!_filter.HasValue || c.Type == _filter)
                && (!_onlyDeck || _draft.Count(c.CardId) > 0)
                && (c.Name.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0 || c.CardId.Value.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            _page = Mathf.Clamp(_page, 0, Math.Max(0, (cards.Count - 1) / 6));
            for (int i = 0; i < 6 && i + _page * 6 < cards.Count; i++)
            {
                var c = cards[i + _page * 6]; var x = .03f + i * .158f;
                var frame = new GameObject("CardFrame", typeof(RectTransform)); frame.transform.SetParent(_body, false); Place(frame.transform, x, .30f, x + .145f, .66f);
                var cardGo = new GameObject(c.CardId.Value, typeof(RectTransform), typeof(CardPlaceholderView), typeof(DeckCardFit)); cardGo.transform.SetParent(frame.transform, false);
                var card = cardGo.GetComponent<CardPlaceholderView>(); card.EnsureChrome();
                card.SetFaceUp(c.Name, CardTypeCatalog.ToLabel(c.Type), "PA " + c.CostPa, "P " + c.Power, c.CardId.Value, c.EffectText);
                card.Bind(() => Notice(c.CardId + " · " + c.Name + " · " + c.EffectText + (EffectEngine.Supports(c) || AdvancedEffects.Supports(c) || ExpandedEffects.Supports(c) ? " [Texto compilado en v0.5; revisar keywords pendientes]" : " [Solo Único/Guardián; otros efectos pendientes]"))); card.SetInteractable(true);
                Button("−", x, .23f, x + .043f, .29f, () => { if (_draft.Remove(c.CardId)) Changed(); });
                Label(_draft.Count(c.CardId) + "/3", x + .045f, .23f, x + .10f, .29f, 20);
                Button("+", x + .102f, .23f, x + .145f, .29f, () => { if (_draft.Add(c.CardId, _catalog, _rules)) Changed(); else Notice("Máximo 40 cartas y 3 copias por carta."); });
            }
            Pager(cards.Count, 6, .17f);
            var costs = _draft.Cards.Where(id => _catalog.TryGet(id, out _)).Select(id => { _catalog.TryGet(id, out var c); return c; }).ToList();
            var curve = string.Join(" · ", costs.GroupBy(c => c.CostPa ?? 0).OrderBy(g => g.Key).Select(g => g.Key + "PA:" + g.Count()));
            Label("Curva " + curve + "\n" + (validation.IsValid ? "Mazo válido para pruebas. Personajes sin efectos compilados funcionan como vanilla." : validation.Reason)
                + (costs.Any(c => c.Power >= 5) ? "" : " · Aviso: sin P≥5 no dañas al héroe sin efectos."), .03f, .075f, .97f, .165f, 16);
        }
        private void CycleIdentity(CardType type)
        {
            var cards = _catalog.All.Where(c => c.Type == type).ToList(); var current = type == CardType.Hero ? _draft.Hero : _draft.Class;
            _draft.SetIdentity(cards[(cards.FindIndex(c => c.CardId == current) + 1) % cards.Count].CardId, _catalog); Changed();
        }
        private void Changed() { _dirty = true; _confirm = null; _notice = "Cambios sin guardar"; Draw(); }
        private string Name(CardId id) => _catalog.TryGet(id, out var c) ? c.Name : id.Value;
        private bool Persist(Action mutation)
        {
            var before = _decks.Select(d => new DeckDraft(d.ToDeck())).ToList();
            try { mutation(); _store.Save(_decks); _notice = "Guardado"; Draw(); return true; }
            catch (Exception ex) { _decks = before; Notice("No se guardó: " + ex.Message); return false; }
        }
        private void Notice(string text) { _notice = text; if (_status != null) _status.text = text; }
        private void Pager(int count, int size, float y)
        {
            Button("‹", .38f, y, .44f, y + .05f, () => { _page = Math.Max(0, _page - 1); Draw(); });
            Label((_page + 1) + " / " + Math.Max(1, (count + size - 1) / size), .45f, y, .55f, y + .05f, 18);
            Button("›", .56f, y, .62f, y + .05f, () => { _page = Math.Min(Math.Max(0, (count - 1) / size), _page + 1); Draw(); });
        }
        private TMP_Text Label(string value, float x, float y, float x1, float y1, int size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(_body, false); Place(go.transform, x, y, x1, y1);
            var text = go.GetComponent<TextMeshProUGUI>(); text.text = value; text.color = Color.white; text.raycastTarget = false;
            text.fontSize = size; text.enableAutoSizing = true; text.fontSizeMin = 12; text.fontSizeMax = size; text.alignment = TextAlignmentOptions.Center; return text;
        }
        private void Button(string value, float x, float y, float x1, float y1, Action action)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(_body, false); Place(go.transform, x, y, x1, y1);
            go.GetComponent<Image>().color = new Color(.16f, .23f, .29f); go.GetComponent<Button>().onClick.AddListener(() => action());
            var parent = _body; _body = go.transform; Label(value, .03f, .03f, .97f, .97f, 21); _body = parent;
        }
        private void Input(string value, float x, float y, float x1, float y1, Action<string> changed)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField)); go.transform.SetParent(_body, false); Place(go.transform, x, y, x1, y1);
            go.GetComponent<Image>().color = new Color(.12f, .15f, .19f);
            var parent = _body; _body = go.transform; var text = (TextMeshProUGUI)Label("", .03f, .05f, .97f, .95f, 22); _body = parent;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            var input = go.GetComponent<TMP_InputField>(); input.textViewport = (RectTransform)go.transform; input.textComponent = text;
            input.characterLimit = 80; input.text = value; input.onEndEdit.AddListener(v => changed(v));
        }
        private static void Place(Transform transform, float x, float y, float x1, float y1)
        { var r = (RectTransform)transform; r.anchorMin = new Vector2(x, y); r.anchorMax = new Vector2(x1, y1); r.offsetMin = r.offsetMax = Vector2.zero; }
    }
}
