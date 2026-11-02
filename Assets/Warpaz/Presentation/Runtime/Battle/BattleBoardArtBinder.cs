using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Applies PRE-SPRINT-2 battle art to existing grey-table views without Domain/Application coupling.
    /// </summary>
    public sealed class BattleBoardArtBinder : MonoBehaviour
    {
        [SerializeField] private Sprite _backgroundSprite;
        [SerializeField] private Sprite _slotFrameSprite;
        [SerializeField] private Sprite _panelFrameSprite;
        [SerializeField] private Sprite _paIconSprite;
        [SerializeField] private Sprite _resolutionIconSprite;
        [SerializeField] private CharacterSlotView _characterSlotPrefab;
        [SerializeField] private Image _backgroundImage;

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            if (_backgroundImage == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var bg = canvas.transform.Find("Background");
                    if (bg != null)
                    {
                        _backgroundImage = bg.GetComponent<Image>();
                    }
                }
            }

            if (_backgroundImage != null && _backgroundSprite != null)
            {
                var rt = _backgroundImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _backgroundImage.sprite = _backgroundSprite;
                _backgroundImage.type = Image.Type.Simple;
                _backgroundImage.preserveAspect = true;
                _backgroundImage.raycastTarget = false;
                _backgroundImage.color = Color.white;
                _backgroundImage.transform.SetAsFirstSibling();

                var fitter = _backgroundImage.GetComponent<AspectRatioFitter>();
                if (fitter == null)
                {
                    fitter = _backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
                }

                if (_backgroundSprite.rect.height > 0.01f)
                {
                    fitter.aspectRatio = _backgroundSprite.rect.width / _backgroundSprite.rect.height;
                }

                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            }

            var board = GetComponentInChildren<BoardSlotsView>(true);
            board?.ApplyArt(_slotFrameSprite, _characterSlotPrefab);

            var playerArea = GetComponentInChildren<PlayerAreaView>(true);
            playerArea?.ApplyArt(_panelFrameSprite, _paIconSprite, _resolutionIconSprite);

            // Thin phase / control bars: flat ink plate — ornate frames stretch into floating gems.
            ApplyFlatPanel(GetComponentInChildren<MatchStatusView>(true), "StatusPanel");
            ApplyFlatPanel(GetComponentInChildren<BattleControlsView>(true), "ControlsPanelFrame");
            ApplySlicedPanel(GetComponentInChildren<MulliganPanel>(true), "MulliganPanelFrame");
            ApplySlicedPanel(GetComponentInChildren<PassDeviceOverlay>(true), "PassOverlayFrame");
            ApplyFlatPanel(GetComponentInChildren<HandView>(true), "HandPanelFrame");
            ApplyFlatPanel(GetComponentInChildren<EventLogView>(true), "EventLogPanelFrame");
        }

        private void ApplyFlatPanel(Component host, string childName)
        {
            if (host == null)
            {
                return;
            }

            var image = EnsurePanelImage(host, childName, inset: 0f);
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = BattleUiColors.HudPanel;
        }

        private void ApplySlicedPanel(Component host, string childName)
        {
            if (host == null || _panelFrameSprite == null)
            {
                return;
            }

            var image = EnsurePanelImage(host, childName, inset: 12f);
            image.sprite = _panelFrameSprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        private static Image EnsurePanelImage(Component host, string childName, float inset)
        {
            var t = host.transform.Find(childName);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(childName, typeof(RectTransform));
                go.transform.SetParent(host.transform, false);
                go.transform.SetAsFirstSibling();
            }
            else
            {
                go = t.gameObject;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-inset, -inset);
            rt.offsetMax = new Vector2(inset, inset);

            var image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }

            image.raycastTarget = false;
            return image;
        }

#if UNITY_EDITOR
        public void EditorAssign(
            Sprite background,
            Sprite slotFrame,
            Sprite panelFrame,
            Sprite paIcon,
            Sprite resolutionIcon,
            CharacterSlotView prefab,
            Image backgroundImage)
        {
            _backgroundSprite = background;
            _slotFrameSprite = slotFrame;
            _panelFrameSprite = panelFrame;
            _paIconSprite = paIcon;
            _resolutionIconSprite = resolutionIcon;
            _characterSlotPrefab = prefab;
            _backgroundImage = backgroundImage;
        }
#endif
    }
}
