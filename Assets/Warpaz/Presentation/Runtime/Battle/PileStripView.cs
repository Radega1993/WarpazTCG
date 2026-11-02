using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Warpaz.Presentation
{
    /// <summary>Deck + discard face-down pile visuals with public counts only.</summary>
    public sealed class PileStripView : MonoBehaviour
    {
        private CardPlaceholderView _deck;
        private CardPlaceholderView _discard;
        private TMP_Text _deckCount;
        private TMP_Text _discardCount;

        public void Render(int deckCount, int discardCount)
        {
            EnsureChrome();
            _deck.SetFaceDown();
            _deck.SetInteractable(false);
            ForcePileCardBackAspect(_deck.gameObject);
            _discard.SetFaceDown();
            _discard.SetInteractable(false);
            ForcePileCardBackAspect(_discard.gameObject);
            if (_deckCount != null)
            {
                _deckCount.text = deckCount.ToString();
            }

            if (_discardCount != null)
            {
                _discardCount.text = discardCount.ToString();
            }
        }

        private void EnsureChrome()
        {
            if (_deck == null)
            {
                _deck = EnsurePile("DeckPile", "Mazo", 0f, 0.48f);
            }

            if (_discard == null)
            {
                _discard = EnsurePile("DiscardPile", "Desc", 0.52f, 1f);
            }
        }

        private CardPlaceholderView EnsurePile(string name, string label, float x0, float x1)
        {
            var t = transform.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
                go.transform.SetParent(transform, false);
            }
            else
            {
                go = t.gameObject;
            }

            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = BattleLayoutBuilder.EnsureRectTransformOnUiObject(go, transform);
                go = rt.gameObject;
            }

            rt.anchorMin = new Vector2(x0, 0.05f);
            rt.anchorMax = new Vector2(x1, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var view = go.GetComponent<CardPlaceholderView>();
            if (view == null)
            {
                view = go.AddComponent<CardPlaceholderView>();
            }

            view.EnsureChrome();
            view.SetFaceDown();
            ForcePileCardBackAspect(go);

            var countName = name + "Count";
            var ct = go.transform.Find(countName);
            TMP_Text tmp;
            if (ct == null)
            {
                var cgo = new GameObject(countName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                cgo.transform.SetParent(go.transform, false);
                var crt = cgo.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.18f, 0.02f);
                crt.anchorMax = new Vector2(0.82f, 0.26f);
                crt.offsetMin = Vector2.zero;
                crt.offsetMax = Vector2.zero;
                var badge = cgo.GetComponent<Image>();
                badge.color = new Color(0.08f, 0.07f, 0.06f, 0.88f);
                badge.raycastTarget = false;
                var labelGo = new GameObject("Text", typeof(RectTransform));
                labelGo.transform.SetParent(cgo.transform, false);
                var lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
                tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 20f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = BattleUiColors.HudText;
                tmp.raycastTarget = false;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
            }
            else
            {
                tmp = ct.GetComponent<TMP_Text>() ?? ct.GetComponentInChildren<TMP_Text>(true);
            }

            if (name.StartsWith("Deck"))
            {
                _deckCount = tmp;
            }
            else
            {
                _discardCount = tmp;
            }

            var lt = go.transform.Find(name + "Label");
            if (lt == null)
            {
                var lgo = new GameObject(name + "Label", typeof(RectTransform));
                lgo.transform.SetParent(go.transform, false);
                var lrt = lgo.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0.72f);
                lrt.anchorMax = new Vector2(1f, 0.95f);
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
                var labelTmp = lgo.AddComponent<TextMeshProUGUI>();
                labelTmp.text = label;
                labelTmp.alignment = TextAlignmentOptions.Center;
                labelTmp.fontSize = 14f;
                labelTmp.color = BattleUiColors.HudText;
                labelTmp.raycastTarget = false;
                labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
            }

            return view;
        }

        private static void ForcePileCardBackAspect(GameObject pileRoot)
        {
            if (pileRoot == null)
            {
                return;
            }

            // Prefer letterboxed card-back art over stretched muddy pixels.
            var images = pileRoot.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img == null || img.sprite == null)
                {
                    continue;
                }

                if (img.gameObject.name.IndexOf("Back", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    img.gameObject.name.IndexOf("Illustration", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    img.preserveAspect = true;
                    img.type = Image.Type.Simple;
                }
            }
        }
    }
}
