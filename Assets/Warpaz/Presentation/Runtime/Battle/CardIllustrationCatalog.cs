using System.Collections.Generic;
using UnityEngine;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Loads WZ1 card illustrations from Resources by cardId (e.g. WZ1-001).
    /// Presentation-only; does not use names as keys.
    /// </summary>
    public static class CardIllustrationCatalog
    {
        private const string ResourcesRoot = "Warpaz/Cards/WZ1/Illustrations/";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> Missing = new HashSet<string>();

        public static Sprite TryGet(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return null;
            }

            if (Cache.TryGetValue(cardId, out var cached))
            {
                return cached;
            }

            if (Missing.Contains(cardId))
            {
                return null;
            }

            var sprite = Resources.Load<Sprite>(ResourcesRoot + cardId);
            if (sprite == null)
            {
                // Some importers expose Texture2D only; build a runtime sprite once.
                var tex = Resources.Load<Texture2D>(ResourcesRoot + cardId);
                if (tex != null)
                {
                    sprite = Sprite.Create(
                        tex,
                        new Rect(0f, 0f, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }

            if (sprite == null)
            {
                Missing.Add(cardId);
                return null;
            }

            Cache[cardId] = sprite;
            return sprite;
        }
    }
}
