using UnityEngine;

namespace Warpaz.Presentation
{
    /// <summary>
    /// Loads the shared face-down card back from Resources.
    /// </summary>
    public static class CardBackCatalog
    {
        private const string ResourcePath = "Warpaz/UI/Cards/card_back";
        private static Sprite _cached;
        private static bool _missing;

        public static Sprite TryGet()
        {
            if (_cached != null)
            {
                return _cached;
            }

            if (_missing)
            {
                return null;
            }

            var sprite = Resources.Load<Sprite>(ResourcePath);
            if (sprite == null)
            {
                var tex = Resources.Load<Texture2D>(ResourcePath);
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
                _missing = true;
                return null;
            }

            _cached = sprite;
            return _cached;
        }
    }
}
