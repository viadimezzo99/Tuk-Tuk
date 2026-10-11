using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Disegno dell'interfaccia: pannelli scuri, testo con ombra nera, moneta dorata.
    /// La scala non scende sotto 1: su una finestra piccola il testo resta leggibile.
    /// </summary>
    public static class Hud
    {
        public static readonly Color Gold = new Color(1f, 0.82f, 0.20f);
        public static readonly Color White = Color.white;
        public static readonly Color Soft = new Color(0.88f, 0.88f, 0.88f);
        private static readonly Color Panel = new Color(0.08f, 0.06f, 0.05f, 0.80f);

        private static GUIStyle text;
        private static GUIStyle shadow;
        private static Texture2D panelTex;
        private static Texture2D coinTex;

        /// <summary>Fattore di scala: 1 a 720 px di altezza, mai sotto 1.</summary>
        public static float K => Mathf.Max(1f, Screen.height / 720f);

        private static void Init()
        {
            if (text != null) return;
            text = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false };
            shadow = new GUIStyle(text);

            panelTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            panelTex.SetPixel(0, 0, Panel);
            panelTex.Apply();

            coinTex = MakeCircle(64, new Color(0.45f, 0.28f, 0.04f), new Color(1f, 0.82f, 0.2f));
        }

        private static Texture2D MakeCircle(int n, Color rim, Color fill)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    Color col = d > 1f ? new Color(0, 0, 0, 0) : (d > 0.82f ? rim : fill);
                    t.SetPixel(x, y, col);
                }
            }
            t.Apply();
            return t;
        }

        /// <summary>Pannello scuro.</summary>
        public static void Box(Rect r)
        {
            Init();
            GUI.DrawTexture(r, panelTex, ScaleMode.StretchToFill);
        }

        /// <summary>Testo con ombra nera, allineato nel rettangolo.</summary>
        public static void Label(Rect r, string s, float size, TextAnchor anchor, Color color)
        {
            Init();
            int fs = Mathf.RoundToInt(size * K);
            text.fontSize = fs;
            text.alignment = anchor;
            text.normal.textColor = color;
            shadow.fontSize = fs;
            shadow.alignment = anchor;
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);

            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s, shadow);
            GUI.Label(r, s, text);
        }

        /// <summary>Icona moneta dorata.</summary>
        public static void Coin(Rect r)
        {
            Init();
            GUI.DrawTexture(r, coinTex, ScaleMode.StretchToFill, true);
        }
    }
}
