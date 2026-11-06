using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Application;

namespace Warpaz.Presentation
{
    /// <summary>Private, paginated choice UI. Only receives the current viewer's projection.</summary>
    public sealed class EffectDecisionPanel
    {
        private readonly GameObject _root;
        private readonly TMP_Text _title;
        private readonly Button[] _buttons = new Button[6];
        private readonly TMP_Text[] _labels = new TMP_Text[6];
        private readonly Button _previous;
        private readonly Button _next;
        private EffectDecisionView _view;
        private Action<EffectDecisionOption> _choose;
        private int _page;
        private int _eventIndex = -1;
        public EffectDecisionPanel(Transform canvas)
        {
            _root = new GameObject("EffectDecisionPanel", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(canvas, false);
            Rect(_root, .12f, .16f, .88f, .84f);
            _root.GetComponent<Image>().color = new Color(.07f, .09f, .12f, .99f);
            _title = Text(_root.transform, "Title", .04f, .85f, .96f, .98f);
            for (var i = 0; i < 6; i++)
            {
                var captured = i;
                _buttons[i] = Button(_root.transform, "Option" + i, .04f, .72f - .105f * i, .96f, .81f - .105f * i, out _labels[i]);
                _buttons[i].onClick.AddListener(() => { var option = _view.Options[_page * 6 + captured]; _choose?.Invoke(option); });
            }
            _previous = Button(_root.transform, "Previous", .04f, .03f, .45f, .12f, out var previousText); previousText.text = "Anterior";
            _next = Button(_root.transform, "Next", .55f, .03f, .96f, .12f, out var nextText); nextText.text = "Siguiente";
            _previous.onClick.AddListener(() => { _page--; Paint(); });
            _next.onClick.AddListener(() => { _page++; Paint(); });
            _root.SetActive(false);
        }
        public void Render(EffectDecisionView view, int eventIndex, Action<EffectDecisionOption> choose)
        {
            _view = view; _choose = choose;
            if (view == null)
            {
                _root.SetActive(false); _page = 0; _choose = null; _title.text = string.Empty;
                foreach (var label in _labels) label.text = string.Empty;
                return;
            }
            if (_eventIndex != eventIndex) _page = 0;
            _eventIndex = eventIndex; _root.SetActive(true); _root.transform.SetAsLastSibling(); Paint();
        }
        private void Paint()
        {
            _title.text = _view.Title;
            for (var i = 0; i < 6; i++)
            {
                var index = _page * 6 + i; var visible = index < _view.Options.Count;
                _buttons[i].gameObject.SetActive(visible);
                _labels[i].text = visible ? _view.Options[index].Label : string.Empty;
            }
            _previous.interactable = _page > 0; _next.interactable = (_page + 1) * 6 < _view.Options.Count;
        }
        private static Button Button(Transform parent, string name, float x0, float y0, float x1, float y1, out TMP_Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            Rect(go, x0, y0, x1, y1); go.GetComponent<Image>().color = new Color(.20f, .25f, .30f);
            label = Text(go.transform, "Text", .02f, .02f, .98f, .98f); return go.GetComponent<Button>();
        }
        private static TMP_Text Text(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            Rect(go, x0, y0, x1, y1); var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = 22; text.enableAutoSizing = true; text.fontSizeMin = 14; text.fontSizeMax = 24;
            text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
        }
        private static void Rect(GameObject go, float x0, float y0, float x1, float y1)
        {
            var rt = (RectTransform)go.transform; rt.anchorMin = new Vector2(x0,y0); rt.anchorMax = new Vector2(x1,y1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
