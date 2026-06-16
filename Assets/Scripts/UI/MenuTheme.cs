using UnityEngine;

namespace MarioBasketball.UI
{
    /// <summary>
    /// Shared bright, Mario-esque look for the IMGUI menus: the palette plus a
    /// few drawing helpers (flat colour fills, rectangle frames, the sky-blue
    /// backdrop with drifting clouds). One source of truth so the main menu, team
    /// select and the rest stay visually in lockstep.
    /// </summary>
    public static class MenuTheme
    {
        public static readonly Color Sky        = new Color(0.42f, 0.70f, 0.98f);
        public static readonly Color Cloud      = new Color(0.98f, 0.99f, 1f);
        public static readonly Color Cream      = new Color(1f, 0.97f, 0.86f);
        public static readonly Color MarioRed   = new Color(0.90f, 0.20f, 0.18f);
        public static readonly Color LuigiGreen = new Color(0.16f, 0.62f, 0.27f);
        public static readonly Color SkyDeep    = new Color(0.18f, 0.46f, 0.88f);
        public static readonly Color Coin       = new Color(1f, 0.80f, 0.10f);
        public static readonly Color Ink        = new Color(0.15f, 0.15f, 0.18f);

        /// <summary>Flat colour rectangle.</summary>
        public static void Fill(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        /// <summary>Coloured border of thickness <paramref name="t"/> around a rect.</summary>
        public static void Frame(Rect r, Color c, float t)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c);
            Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c);
            Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        /// <summary>Fill the whole screen with the sky and drift a few clouds across.</summary>
        public static void DrawBackground()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), Sky);
            float t = Time.unscaledTime * 18f;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.7f);
            for (int i = 0; i < 5; i++)
            {
                float x = Mathf.Repeat(t + i * 360f, Screen.width + 200f) - 160f;
                float y = 40f + i * (Screen.height / 6f);
                GUI.DrawTexture(new Rect(x, y, 130f, 42f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x + 40f, y - 22f, 80f, 40f), Texture2D.whiteTexture);
            }
            GUI.color = prev;
        }
    }
}
