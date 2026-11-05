using UnityEngine;

namespace Warpaz.Presentation
{
    /// <summary>One card ratio for hand, board, collection and inspector. Never stretch artwork.</summary>
    public static class CardLayout
    {
        public const float Aspect = 5f / 7f;

        public static Vector2 Fit(float width, float height)
        {
            width = Mathf.Max(0f, width);
            height = Mathf.Max(0f, height);
            var h = Mathf.Min(height, width / Aspect);
            return new Vector2(h * Aspect, h);
        }

        public static void FitInParent(RectTransform card, RectTransform parent)
        {
            if (card == null || parent == null || parent.rect.width <= 0f || parent.rect.height <= 0f) return;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = Fit(parent.rect.width, parent.rect.height);
        }
    }
}
