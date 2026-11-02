using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Application;
using Warpaz.Domain;

namespace Warpaz.Presentation
{
    public sealed class PlayerAreaView : MonoBehaviour
    {
        [SerializeField] private Sprite _panelFrameSprite;
        [SerializeField] private Sprite _paIconSprite;
        [SerializeField] private Sprite _resolutionIconSprite;

        private TMP_Text _priorityName;
        private TMP_Text _priorityHero;
        private TMP_Text _priorityResolution;
        private TMP_Text _priorityPa;

        private TMP_Text _rivalName;
        private TMP_Text _rivalHero;
        private TMP_Text _rivalResolution;
        private TMP_Text _rivalPa;

        private Image _priorityFormPanel;
        private Image _rivalPanel;
        private Image _priorityPaIcon;
        private Image _priorityResIcon;
        private Image _rivalPaIcon;
        private Image _rivalResIcon;

        private Button _rivalHeroButton;
        private System.Action _onRivalHeroClicked;
        private CardPlaceholderView _localHeroCard;
        private CardPlaceholderView _rivalHeroCard;

        private RectTransform _opponentHudZone;
        private RectTransform _localHudZone;

        private void Awake()
        {
            CacheTexts();
            EnsureChrome();
            LayoutHudTexts();
        }

        public void AssignHudZones(RectTransform opponentHud, RectTransform localHud)
        {
            _opponentHudZone = opponentHud;
            _localHudZone = localHud;
            CacheTexts();
            EnsureChrome();
            LayoutHudTexts();
        }

        public void ApplyArt(Sprite panelFrame, Sprite paIcon, Sprite resolutionIcon)
        {
            _panelFrameSprite = panelFrame;
            _paIconSprite = paIcon;
            _resolutionIconSprite = resolutionIcon;
            EnsureChrome();
        }

        public void BindRivalHero(System.Action onRivalHeroClicked)
        {
            _onRivalHeroClicked = onRivalHeroClicked;
        }

        public void Render(
            MatchView view,
            PlayerId activePlayer,
            PlayerId localSeat,
            ICardCatalog catalog,
            string player0Name,
            string player1Name)
        {
            if (view == null)
            {
                return;
            }

            CacheTexts();
            EnsureChrome();

            // Bottom HUD = local seat (viewer / learner). Top = opponent.
            var local = localSeat == PlayerId.Player0 ? view.Player0 : view.Player1;
            var rival = localSeat == PlayerId.Player0 ? view.Player1 : view.Player0;

            var localName = localSeat == PlayerId.Player0 ? player0Name : player1Name;
            var rivalName = localSeat == PlayerId.Player0 ? player1Name : player0Name;

            SetText(_priorityName, localName);
            SetText(_priorityResolution, "Res " + local.Resolution);
            SetText(_priorityPa, "PA " + local.PaCurrent + "/" + local.PaMax);
            SetText(_priorityHero, ResolveName(catalog, local.HeroId));

            SetText(_rivalName, rivalName);
            SetText(_rivalResolution, "Res " + rival.Resolution);
            SetText(_rivalPa, "PA " + rival.PaCurrent + "/" + rival.PaMax);
            SetText(_rivalHero, ResolveName(catalog, rival.HeroId));

            RenderHeroCard(_localHeroCard, catalog, local.HeroId, interactable: false);
            RenderHeroCard(_rivalHeroCard, catalog, rival.HeroId, interactable: true);

            HideIfPresent("PriorityClassText");
            HideIfPresent("RivalClassText");
            HideIfPresent("PriorityHandCountText");
            HideIfPresent("PriorityDeckCountText");
            HideIfPresent("PriorityDiscardCountText");
            HideIfPresent("RivalHandCountText");
            HideIfPresent("RivalDeckCountText");
            HideIfPresent("RivalDiscardCountText");
        }

        private void CacheTexts()
        {
            _priorityName = Find("PriorityNameText");
            _priorityHero = Find("PriorityHeroText");
            _priorityResolution = Find("PriorityResolutionText");
            _priorityPa = Find("PriorityPaText");

            _rivalName = Find("RivalNameText");
            _rivalHero = Find("RivalHeroText");
            _rivalResolution = Find("RivalResolutionText");
            _rivalPa = Find("RivalPaText");

            if (_rivalHero != null)
            {
                _rivalHeroButton = _rivalHero.GetComponent<Button>();
                if (_rivalHeroButton == null)
                {
                    _rivalHeroButton = _rivalHero.gameObject.AddComponent<Button>();
                }

                _rivalHeroButton.onClick.RemoveAllListeners();
                _rivalHeroButton.onClick.AddListener(() => _onRivalHeroClicked?.Invoke());
            }
        }

        private void EnsureChrome()
        {
            var localParent = _localHudZone != null ? _localHudZone : transform as RectTransform;
            var oppParent = _opponentHudZone != null ? _opponentHudZone : transform as RectTransform;

            _priorityFormPanel = EnsurePanel("PriorityPanel", localParent);
            _rivalPanel = EnsurePanel("RivalPanel", oppParent);

            LayoutPanelRegion(_priorityFormPanel, 0.42f, 0f, 1f, 1f);
            LayoutPanelRegion(_rivalPanel, 0.42f, 0f, 1f, 1f);

            ParentTextUnder(_priorityName, _priorityFormPanel);
            ParentTextUnder(_priorityHero, _priorityFormPanel);
            ParentTextUnder(_priorityResolution, _priorityFormPanel);
            ParentTextUnder(_priorityPa, _priorityFormPanel);

            ParentTextUnder(_rivalName, _rivalPanel);
            ParentTextUnder(_rivalHero, _rivalPanel);
            ParentTextUnder(_rivalResolution, _rivalPanel);
            ParentTextUnder(_rivalPa, _rivalPanel);

            _priorityPaIcon = EnsureIcon(
                "PriorityPaIcon",
                _paIconSprite,
                _priorityFormPanel != null ? _priorityFormPanel.transform : transform);
            _priorityResIcon = EnsureIcon(
                "PriorityResIcon",
                _resolutionIconSprite,
                _priorityFormPanel != null ? _priorityFormPanel.transform : transform);
            _rivalPaIcon = EnsureIcon(
                "RivalPaIcon",
                _paIconSprite,
                _rivalPanel != null ? _rivalPanel.transform : transform);
            _rivalResIcon = EnsureIcon(
                "RivalResIcon",
                _resolutionIconSprite,
                _rivalPanel != null ? _rivalPanel.transform : transform);

            _localHeroCard = EnsureHeroCard("LocalHeroCard", localParent, 0f, 0f, 0.40f, 1f);
            _rivalHeroCard = EnsureHeroCard("RivalHeroCard", oppParent, 0f, 0f, 0.40f, 1f);
        }

        private CardPlaceholderView EnsureHeroCard(
            string name,
            Transform parent,
            float x0,
            float y0,
            float x1,
            float y1)
        {
            if (parent == null)
            {
                parent = transform;
            }

            var t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = t.gameObject;
                go.transform.SetParent(parent, false);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);

            var view = go.GetComponent<CardPlaceholderView>();
            if (view == null)
            {
                view = go.AddComponent<CardPlaceholderView>();
            }

            view.EnsureChrome();
            return view;
        }

        private static void LayoutPanelRegion(Image panel, float x0, float y0, float x1, float y1)
        {
            if (panel == null)
            {
                return;
            }

            var rt = panel.rectTransform;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void RenderHeroCard(
            CardPlaceholderView view,
            ICardCatalog catalog,
            CardId heroId,
            bool interactable)
        {
            if (view == null)
            {
                return;
            }

            var name = ResolveName(catalog, heroId);
            var power = "—";
            string rules = null;
            if (catalog != null && catalog.TryGet(heroId, out var def))
            {
                name = def.Name;
                if (def.Power.HasValue)
                {
                    power = "P" + def.Power.Value;
                }

                rules = def.EffectText;
            }

            view.gameObject.SetActive(true);
            // Identity cards: no rules wall — keep the portrait readable.
            view.SetFaceUp(name, CardTypeCatalog.ToLabel(CardType.Hero), "—", power, heroId.Value, string.Empty);
            view.SetInteractable(interactable);
            view.Bind(interactable ? () => _onRivalHeroClicked?.Invoke() : null);
        }

        private void LayoutHudTexts()
        {
            LayoutStack(_priorityName, _priorityHero, _priorityResolution, _priorityPa, _priorityPaIcon, _priorityResIcon);
            LayoutStack(_rivalName, _rivalHero, _rivalResolution, _rivalPa, _rivalPaIcon, _rivalResIcon);
        }

        private static void LayoutStack(
            TMP_Text name,
            TMP_Text hero,
            TMP_Text resolution,
            TMP_Text pa,
            Image paIcon,
            Image resIcon)
        {
            Place(name, 0.08f, 0.74f, 0.94f, 0.96f);
            Place(hero, 0.08f, 0.50f, 0.94f, 0.74f);
            Place(resolution, 0.34f, 0.26f, 0.94f, 0.48f);
            Place(pa, 0.34f, 0.04f, 0.94f, 0.26f);
            PlaceIcon(resIcon, 0.06f, 0.26f, 0.30f, 0.48f);
            PlaceIcon(paIcon, 0.06f, 0.04f, 0.30f, 0.26f);
            ConfigureLabel(name);
            ConfigureLabel(hero);
            ConfigureLabel(resolution);
            ConfigureLabel(pa);
        }

        private static void Place(TMP_Text text, float x0, float y0, float x1, float y1)
        {
            if (text == null)
            {
                return;
            }

            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void PlaceIcon(Image icon, float x0, float y0, float x1, float y1)
        {
            if (icon == null)
            {
                return;
            }

            var rt = icon.rectTransform;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
        }

        private static void ConfigureLabel(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 26f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.color = BattleUiColors.HudText;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Left;
        }

        private Image EnsurePanel(string name, Transform parent)
        {
            if (parent == null)
            {
                parent = transform;
            }

            var t = parent.Find(name);
            if (t == null)
            {
                t = transform.Find(name);
            }

            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = t.gameObject;
                go.transform.SetParent(parent, false);
            }

            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = BattleLayoutBuilder.EnsureRectTransformOnUiObject(go, parent);
                go = rt.gameObject;
            }

            BattleLayoutBuilder.StretchFull(rt);

            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }

            // Flat plate — the ornate table frame sprite stretches into floating gems on small HUDs.
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = BattleUiColors.HudPanel;
            image.raycastTarget = false;
            return image;
        }

        private Image EnsureIcon(string name, Sprite sprite, Transform parent)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                t = transform.Find(name);
            }

            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = t.gameObject;
                go.transform.SetParent(parent, false);
            }

            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }

            if (sprite != null)
            {
                image.sprite = sprite;
            }

            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            // Avoid stretching tiny icon sprites into muddy blobs.
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            return image;
        }

        private static void ParentTextUnder(TMP_Text text, Image panel)
        {
            if (text == null || panel == null)
            {
                return;
            }

            text.transform.SetParent(panel.transform, false);
        }

        private void HideIfPresent(string path)
        {
            var t = transform.Find(path);
            if (t != null)
            {
                t.gameObject.SetActive(false);
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private TMP_Text Find(string path)
        {
            var t = transform.Find(path);
            if (t == null && _localHudZone != null)
            {
                t = _localHudZone.Find(path) ?? FindDeep(_localHudZone, path);
            }

            if (t == null && _opponentHudZone != null)
            {
                t = _opponentHudZone.Find(path) ?? FindDeep(_opponentHudZone, path);
            }

            if (t == null)
            {
                t = FindDeep(transform, path);
            }

            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string ResolveName(ICardCatalog catalog, CardId id)
        {
            if (catalog != null && catalog.TryGet(id, out var def) && def != null)
            {
                return def.Name;
            }

            return id.Value;
        }
    }
}
