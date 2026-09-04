using TMPro;
using UnityEngine;

namespace Warpaz.Presentation
{
    public sealed class CardPlaceholderView : MonoBehaviour
    {
        private TMP_Text _text;

        private void Awake()
        {
            _text = Find("Text");
        }

        public void SetBlocked(string label)
        {
            if (_text != null)
            {
                _text.text = label;
            }
        }

        public void SetCard(string name, string type, string cost)
        {
            if (_text != null)
            {
                _text.text = name + "\n" + type + "\n" + cost;
            }
        }

        private TMP_Text Find(string relativePath)
        {
            var t = transform.Find(relativePath);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }
    }
}
