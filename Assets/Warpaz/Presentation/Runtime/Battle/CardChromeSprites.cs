using UnityEngine;

namespace Warpaz.Presentation
{
    /// <summary>Procedural UI sprites for card chrome (circle gems, soft rounded plate).</summary>
    public static class CardChromeSprites
    {
        private static Sprite _circle;
        private static Sprite _roundedRect;

        public static Sprite Circle()
        {
            if (_circle != null)
            {
                return _circle;
            }

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "WarpazCardCircle"
            };

            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f - 1.25f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var a = Mathf.Clamp01(radius + 0.75f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, true);
            _circle = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            _circle.name = "WarpazCardCircle";
            return _circle;
        }

        public static Sprite RoundedRect()
        {
            if (_roundedRect != null)
            {
                return _roundedRect;
            }

            const int size = 64;
            const float radius = 10f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "WarpazCardRounded"
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var a = RoundedAlpha(x, y, size, size, radius);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply(false, true);
            // 9-slice friendly borders so stretched cards keep rounded corners.
            _roundedRect = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            _roundedRect.name = "WarpazCardRounded";
            return _roundedRect;
        }

        private static float RoundedAlpha(int x, int y, int w, int h, float radius)
        {
            var rx = radius;
            var ry = radius;
            float px = x + 0.5f;
            float py = y + 0.5f;

            // Inside the flat region → opaque.
            if (px >= rx && px <= w - rx && py >= ry && py <= h - ry)
            {
                return 1f;
            }

            if (px >= rx && px <= w - rx)
            {
                return 1f;
            }

            if (py >= ry && py <= h - ry)
            {
                return 1f;
            }

            float cx;
            float cy;
            if (px < rx)
            {
                cx = rx;
            }
            else
            {
                cx = w - rx;
            }

            if (py < ry)
            {
                cy = ry;
            }
            else
            {
                cy = h - ry;
            }

            var d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
            return Mathf.Clamp01(radius + 0.6f - d);
        }
    }
}
