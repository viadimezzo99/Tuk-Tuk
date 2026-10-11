using System;
using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Texture generate in codice, senza file esterni. Sono piastrellabili (ripetono senza giunture)
    /// e quasi bianche: il colore del materiale le tinge. Ogni texture è una "tile" di circa 4 m.
    /// </summary>
    public static class ProceduralTextures
    {
        private const int N = 256;

        private static Texture2D white, facade, sidewalk, sign, awning, asphalt, plank, lattice, mosaic;

        public static Texture2D White
        {
            get { if (white == null) white = MakeTex((x, y) => Color.white, 1); return white; }
        }

        /// <summary>Muro d'intonaco con finestra a telaio scuro e fascia marcapiano.</summary>
        public static Texture2D Facade
        {
            get { if (facade == null) facade = MakeTex(FacadePixel); return facade; }
        }

        /// <summary>Basoli di pietra, con fuga scura e variazioni di tono.</summary>
        public static Texture2D Sidewalk
        {
            get { if (sidewalk == null) sidewalk = MakeTex(SidewalkPixel); return sidewalk; }
        }

        /// <summary>Asfalto con grana e piccole macchie di usura.</summary>
        public static Texture2D Asphalt
        {
            get { if (asphalt == null) asphalt = MakeTex(AsphaltPixel); return asphalt; }
        }

        /// <summary>Insegna con tratti di calligrafia stilizzata su fondo crema.</summary>
        public static Texture2D Sign
        {
            get { if (sign == null) sign = MakeTex(SignPixel); return sign; }
        }

        /// <summary>Tendone a righe verticali con bordo a festoni.</summary>
        public static Texture2D Awning
        {
            get { if (awning == null) awning = MakeTex(AwningPixel); return awning; }
        }

        /// <summary>Tavole di legno con venature.</summary>
        public static Texture2D Plank
        {
            get { if (plank == null) plank = MakeTex(PlankPixel); return plank; }
        }

        /// <summary>Grata a mashrabiya (rombi traforati), per balconi e finestre.</summary>
        public static Texture2D Lattice
        {
            get { if (lattice == null) lattice = MakeTex(LatticePixel); return lattice; }
        }

        /// <summary>Mosaico a stella islamica, per fasce decorative e cupole.</summary>
        public static Texture2D Mosaic
        {
            get { if (mosaic == null) mosaic = MakeTex(MosaicPixel); return mosaic; }
        }

        // ---------- pixel ----------

        private static Color FacadePixel(int x, int y)
        {
            float u = x / (float)N * 4f, v = y / (float)N * 4f;
            float plaster = 0.90f + 0.10f * Fbm(u, v, 4);
            float fu = Frac(u), fv = Frac(v);

            // Finestra centrata nel riquadro, con telaio chiaro.
            float wu = Mathf.Abs(fu - 0.5f), wv = Mathf.Abs(fv - 0.42f);
            if (wu < 0.16f && wv < 0.24f)
            {
                if (wu > 0.12f || wv > 0.20f) return new Color(0.95f, 0.93f, 0.88f);
                float glass = 0.30f + 0.10f * Hash(Mathf.FloorToInt(u), Mathf.FloorToInt(v));
                return new Color(glass * 0.8f, glass * 0.9f, glass);
            }
            // Marcapiano orizzontale e zoccolo.
            if (fv > 0.82f && fv < 0.86f) return new Color(0.78f, 0.74f, 0.68f);
            if (fv < 0.06f) return new Color(0.70f, 0.66f, 0.62f) * plaster;
            return new Color(plaster, plaster * 0.99f, plaster * 0.97f);
        }

        private static Color SidewalkPixel(int x, int y)
        {
            float u = x / (float)N * 4f, v = y / (float)N * 4f;
            float fu = Frac(u), fv = Frac(v);
            int cu = Mathf.FloorToInt(u), cv = Mathf.FloorToInt(v);
            float tint = 0.92f + 0.08f * Hash(cu, cv);
            float grain = 0.95f + 0.05f * Noise(x * 0.25f, y * 0.25f);
            if (fu < 0.05f || fv < 0.05f) return new Color(0.62f, 0.58f, 0.52f);
            return new Color(tint * grain, tint * grain * 0.97f, tint * grain * 0.90f);
        }

        private static Color AsphaltPixel(int x, int y)
        {
            float u = x / (float)N * 8f, v = y / (float)N * 8f;
            float g = 0.85f + 0.15f * Fbm(u, v, 8);
            float speck = Hash(x, y) > 0.985f ? 0.55f : 1f;
            float c = g * speck;
            return new Color(c, c, c * 1.02f);
        }

        private static Color SignPixel(int x, int y)
        {
            float u = x / (float)N, v = y / (float)N;
            float row = Frac(v * 4f);
            float wave = Mathf.Sin(u * 2f * Mathf.PI * 3f + Hash(0, Mathf.FloorToInt(v * 4f)) * 6f) * 0.5f + 0.5f;
            bool stroke = row > 0.35f && row < 0.55f && wave > 0.45f;
            if (stroke) return new Color(0.22f, 0.16f, 0.12f);
            return new Color(0.98f, 0.95f, 0.86f);
        }

        private static Color AwningPixel(int x, int y)
        {
            float u = x / (float)N * 8f, v = y / (float)N;
            float stripe = Frac(u) < 0.5f ? 1f : 0.82f;
            float scallop = Frac(u) < 0.5f ? 0f : 1f;
            if (v > 0.90f) return new Color(stripe * 0.75f, stripe * 0.75f, stripe * 0.75f);
            return new Color(stripe, stripe, stripe);
        }

        private static Color PlankPixel(int x, int y)
        {
            float u = x / (float)N * 8f, v = y / (float)N * 8f;
            int idx = Mathf.FloorToInt(v);
            float tone = 0.85f + 0.15f * Hash(idx, 7);
            float grain = 0.9f + 0.1f * Fbm(u, v, 8);
            if (Frac(v) < 0.05f) return new Color(0.5f, 0.45f, 0.4f);
            float c = tone * grain;
            return new Color(c, c * 0.92f, c * 0.8f);
        }

        private static Color LatticePixel(int x, int y)
        {
            float fu = Frac(x / (float)N * 2f) - 0.5f, fv = Frac(y / (float)N * 2f) - 0.5f;
            bool hole = Mathf.Abs(fu) + Mathf.Abs(fv) < 0.36f;
            if (hole) return new Color(0.25f, 0.18f, 0.12f);
            float g = 0.95f + 0.05f * Noise(x * 0.2f, y * 0.2f);
            return new Color(g, g * 0.92f, g * 0.8f);
        }

        private static Color MosaicPixel(int x, int y)
        {
            float dx = x / (float)N - 0.5f, dy = y / (float)N - 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float th = Mathf.Atan2(dy, dx);
            float star = 0.22f + 0.13f * Mathf.Abs(Mathf.Cos(4f * th));
            if (r < star) return new Color(0.95f, 0.95f, 0.95f);
            if (r < star + 0.03f) return new Color(0.2f, 0.2f, 0.25f);
            if (r > 0.44f) return new Color(0.2f, 0.2f, 0.25f);
            return new Color(0.80f, 0.80f, 0.88f);
        }

        // ---------- utilità ----------

        private static Texture2D MakeTex(Func<int, int, Color> pixel, int n = N)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 4;
            var cols = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    cols[y * n + x] = pixel(x, y);
            t.SetPixels(cols);
            t.Apply(true);
            return t;
        }

        private static float Frac(float v) => v - Mathf.Floor(v);

        private static int Mod(int a, int m) => ((a % m) + m) % m;

        private static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        /// <summary>Rumore di valore con passo intero: tiling senza giunture.</summary>
        private static float PNoise(float x, float y, int p)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int x0 = Mod(xi, p), x1 = Mod(xi + 1, p), y0 = Mod(yi, p), y1 = Mod(yi + 1, p);
            float a = Hash(x0, y0), b = Hash(x1, y0), c = Hash(x0, y1), d = Hash(x1, y1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float Fbm(float x, float y, int p)
        {
            float v = 0f, amp = 0.5f;
            for (int i = 0; i < 4; i++)
            {
                v += amp * PNoise(x, y, p);
                x *= 2f; y *= 2f; p *= 2; amp *= 0.5f;
            }
            return v;
        }

        private static float Noise(float x, float y) => PNoise(x, y, 64);
    }
}
