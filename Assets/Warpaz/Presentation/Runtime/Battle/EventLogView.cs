using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warpaz.Application;

namespace Warpaz.Presentation
{
    public sealed class EventLogView : MonoBehaviour
    {
        private TMP_Text _toggleText;
        private Button _toggleButton;
        private GameObject _root;
        private TMP_Text _logText;

        private bool _collapsed = true;

        private void Awake()
        {
            _toggleText = FindText("ToggleText");
            _toggleButton = FindButton("ToggleButton");
            _root = transform.Find("LogRoot") != null ? transform.Find("LogRoot").gameObject : null;
            _logText = FindText("LogRoot/LogText");

            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveAllListeners();
                _toggleButton.onClick.AddListener(Toggle);
            }

            if (_root != null)
            {
                _root.SetActive(!_collapsed);
            }
        }

        private void Toggle()
        {
            _collapsed = !_collapsed;
            if (_root != null)
            {
                _root.SetActive(!_collapsed);
            }

            if (_toggleText != null)
            {
                _toggleText.text = _collapsed ? "Mostrar log" : "Ocultar log";
            }
        }

        public void Render(IReadOnlyList<GameEventView> events, string rejectionReason)
        {
            if (_logText == null)
            {
                return;
            }

            if (events == null)
            {
                _logText.text = string.Empty;
                return;
            }

            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < events.Count; i++)
            {
                var e = events[i];
                builder.Append('[');
                builder.Append(e.Type);
                builder.Append("] ");
                builder.Append(e.Payload);
                if (i + 1 < events.Count)
                {
                    builder.Append('\n');
                }
            }

            if (!string.IsNullOrEmpty(rejectionReason))
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }
                builder.Append("UI: ").Append(rejectionReason);
            }

            _logText.text = builder.ToString();
        }

        private Button FindButton(string relativePath)
        {
            var t = transform.Find(relativePath);
            return t != null ? t.GetComponent<Button>() : null;
        }

        private TMP_Text FindText(string relativePath)
        {
            var t = transform.Find(relativePath);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }
    }
}
