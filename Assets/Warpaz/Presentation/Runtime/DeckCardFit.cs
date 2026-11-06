using UnityEngine;
namespace Warpaz.Presentation
{
    public sealed class DeckCardFit : MonoBehaviour
    {
        private void LateUpdate()
        {
            var rect = (RectTransform)transform; var parent = (RectTransform)transform.parent;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = CardLayout.Fit(parent.rect.width, parent.rect.height);
        }
    }
}
