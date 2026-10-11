using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Distretti del Cairo lungo la strada: Giza, Residenziale, Downtown, Islamico, Zamalek.
    /// Ogni distretto dura DistrictLength metri; nell'ultima parte si mescola con il successivo.
    /// </summary>
    public static class Districts
    {
        public const int Count = 5;
        public const float DistrictLength = 300f;
        public const float BlendFraction = 0.3f;

        private static readonly Color[] Sky =
        {
            new Color(0.98f, 0.85f, 0.65f),
            new Color(0.62f, 0.82f, 1.00f),
            new Color(0.70f, 0.88f, 1.00f),
            new Color(0.98f, 0.80f, 0.60f),
            new Color(0.60f, 0.90f, 0.95f),
        };

        /// <summary>Pesi dei distretti in una certa z.</summary>
        public static float[] Weights(float z)
        {
            var w = new float[Count];
            float pos = Mathf.Max(0f, z) / DistrictLength;
            int i = Mathf.FloorToInt(pos);
            float f = pos - i;
            float start = 1f - BlendFraction;
            if (f <= start)
            {
                w[i % Count] = 1f;
            }
            else
            {
                float t = (f - start) / BlendFraction;
                t = t * t * (3f - 2f * t);
                w[i % Count] = 1f - t;
                w[(i + 1) % Count] = t;
            }
            return w;
        }

        /// <summary>Sceglie un distretto secondo i pesi.</summary>
        public static int Pick(float[] w)
        {
            float r = Random.value, acc = 0f;
            for (int j = 0; j < w.Length; j++)
            {
                acc += w[j];
                if (r <= acc) return j;
            }
            return w.Length - 1;
        }

        /// <summary>Colore del cielo, mescolato con i distretti vicini al tuk-tuk.</summary>
        public static Color SkyAt(float z)
        {
            float[] w = Weights(z);
            Color c = Color.black;
            for (int j = 0; j < Count; j++) c += Sky[j] * w[j];
            return c;
        }
    }
}
