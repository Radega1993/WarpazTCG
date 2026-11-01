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

        private Image _priorityPanelPanel;
        private Image _rivalPanel;
        private Image _priorityPaIcon;
        private Image _priorityResIcon;
        private Image _rivalPaIcon;
        private Image _rivalResIcon;

        private Button _rivalHeroButton;
        private System.Action _onRivalHeroClicked;

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
            PlayerId prioritySeat,
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

            var priority = prioritySeat == PlayerId.Player0 ? view.Player0 : view.Player1;
            var rival = prioritySeat == PlayerId.Player0 ? view.Player1 : view.Player0;

            var priorityName = prioritySeat == PlayerId.Player0 ? player0Name : player1Name;
            var rivalName = prioritySeat == PlayerId.Player0 ? player1Name : player0Name;

            SetText(_priorityName, priorityName);
            SetText(_priorityResolution, priority.Resolution.ToString());
            SetText(_priorityPa, priority.PaCurrent + "/" + priority.PaMax);
            SetText(_priorityHero, ResolveName(catalog, priority.HeroId));

            SetText(_rivalName, rivalName);
            SetText(_rivalResolution, rival.Resolution.ToString());
            SetText(_rivalPa, rival.PaCurrent + "/" + rival.PaMax);
            SetText(_rivalHero, ResolveName(catalog, rival.HeroId));

            // Hide legacy verbose fields if still present in scene.
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

            _priorityPanelPanel = EnsurePanel("PriorityPanel", localParent);
            _rivalPanel = EnsurePanel("RivalPanel", oppParent);

            ParentTextUnder(_priorityName, _priorityPanelPanel);
            ParentTextUnder(_priorityHero, _priorityPanelPanel);
            ParentTextUnder(_priorityResolution, _priorityPanelPanel);
            ParentTextUnder(_priorityPa, _priorityPanelPanel);

            ParentTextUnder(_rivalName, _rivalPanel);
            ParentTextUnder(_rivalHero, _rivalPanel);
            ParentTextUnder(_rivalResolution, _rivalPanel);
            ParentTextUnder(_rivalPa, _rivalPanel);

            _priorityPaIcon = EnsureIcon("PriorityPaIcon", _paIconSprite, _priorityPanelPanel != null ? _priorityPanelPanel.transform : transform);
            _priorityResIcon = EnsureIcon("PriorityResIcon", _resolutionIconSprite, _priorityPanelPanel != null ? _priorityPanelPanel.transform : transform);
            _rivalPaIcon = EnsureIcon("RivalPaIcon", _paIconSprite, _rivalPanel != null ? _rivalPanel.transform : transform);
            _rivalResIcon = EnsureIcon("RivalResIcon", _resolutionIconSprite, _rivalPanel != null ? _rivalPanel.transform : transform);
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
            Place(name, 0.06f, 0.72f, 0.94f, 0.94f);
            Place(hero, 0.06f, 0.52f, 0.94f, 0.72f);
            Place(resolution, 0.28f, 0.28f, 0.94f, 0.50f);
            Place(pa, 0.28f, 0.06f, 0.94f, 0.28f);
            PlaceIcon(resIcon, 0.06f, 0.28f, 0.24f, 0.50f);
            PlaceIcon(paIcon, 0.06f, 0.06f, 0.24f, 0.28f);
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
            text.color = BattleUiColors.CardText;
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
                // May still live under this view from the old scene.
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

            if (_panelFrameSprite != null)
            {
                image.sprite = _panelFrameSprite;
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                image.preserveAspect = false;
            }

            image.color = Color.white;
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
