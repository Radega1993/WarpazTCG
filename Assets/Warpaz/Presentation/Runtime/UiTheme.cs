using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Warpaz.Presentation
{
    [CreateAssetMenu(fileName = "UiTheme", menuName = "WARPAZ/UI Theme")]
    public sealed class UiTheme : ScriptableObject
    {
        [SerializeField] private Color background = new Color(0.0706f, 0.0784f, 0.1020f, 1f);
        [SerializeField] private Color panel = new Color(0.1176f, 0.1373f, 0.1882f, 1f);
        [SerializeField] private Color text = new Color(0.9490f, 0.9569f, 0.9725f, 1f);
        [SerializeField] private Color accent = new Color(0.8314f, 0.6588f, 0.2941f, 1f);
        [SerializeField] private Color buttonNormal = new Color(0.2196f, 0.2588f, 0.3490f, 1f);
        [SerializeField] private Color buttonHover = new Color(0.3216f, 0.3765f, 0.5020f, 1f);
        [SerializeField] private Color buttonDisabled = new Color(0.1490f, 0.1608f, 0.1961f, 0.55f);

        public Color Background => background;
        public Color Panel => panel;
        public Color Text => text;
        public Color Accent => accent;

        public ColorBlock ButtonColors
        {
            get
            {
                var block = ColorBlock.defaultColorBlock;
                block.normalColor = buttonNormal;
                block.highlightedColor = buttonHover;
                block.pressedColor = accent;
                block.selectedColor = accent;
                block.disabledColor = buttonDisabled;
                block.colorMultiplier = 1f;
                block.fadeDuration = 0.08f;
                return block;
            }
        }

        public void ApplyBackground(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.color = background;
        }

        public void ApplyPanel(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.color = panel;
        }

        public void ApplyText(TMP_Text label)
        {
            if (label == null)
            {
                return;
            }

            label.color = text;
            label.raycastTarget = false;
        }

        public void ApplyButton(Button button, TMP_Text label)
        {
            if (button == null)
            {
                return;
            }

            button.colors = ButtonColors;
            button.transition = Selectable.Transition.ColorTint;
            var image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image != null)
            {
                image.color = Color.white;
            }

            ApplyText(label);
        }
    }
}
