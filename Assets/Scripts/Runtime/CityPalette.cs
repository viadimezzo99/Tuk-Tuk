using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>Palette di colori e texture di base del mondo (strada, marciapiedi, arredi, cartelli).</summary>
    public static class CityPalette
    {
        public static readonly Color Asphalt = new Color(0.36f, 0.36f, 0.40f);
        public static readonly Color Curb = new Color(0.95f, 0.85f, 0.60f);
        public static readonly Color Cornice = new Color(0.98f, 0.95f, 0.88f);
        public static readonly Color Dome = new Color(0.25f, 0.62f, 0.78f);
        public static readonly Color Wood = new Color(0.62f, 0.42f, 0.25f);
        public static readonly Color Pole = new Color(0.25f, 0.25f, 0.28f);
        public static readonly Color Paint = Color.white;
        public static readonly Color Warning = new Color(1f, 0.80f, 0.10f);
        public static readonly Color Blood = new Color(0.55f, 0.04f, 0.05f);
        public static readonly Color Tyre = new Color(0.10f, 0.10f, 0.10f);

        public static readonly Color[][] Palettes =
        {
            new[] { new Color(0.93f, 0.82f, 0.60f), new Color(0.85f, 0.62f, 0.38f), new Color(0.90f, 0.72f, 0.45f), new Color(0.95f, 0.88f, 0.70f) },
            new[] { new Color(0.96f, 0.96f, 0.94f), new Color(0.80f, 0.82f, 0.85f), new Color(0.92f, 0.85f, 0.75f), new Color(0.70f, 0.75f, 0.80f) },
            new[] { new Color(0.78f, 0.42f, 0.30f), new Color(0.85f, 0.62f, 0.38f), new Color(0.55f, 0.75f, 0.70f), new Color(0.90f, 0.60f, 0.40f) },
        };
        public static readonly Color[] SkyColors =
        {
            new Color(0.98f, 0.84f, 0.62f),
            new Color(0.60f, 0.85f, 1.00f),
            new Color(0.45f, 0.58f, 0.85f),
        };
        public static readonly Color[] AwningColors =
        {
            new Color(0.90f, 0.20f, 0.20f),
            new Color(0.15f, 0.65f, 0.35f),
            new Color(0.98f, 0.75f, 0.10f),
            new Color(0.20f, 0.45f, 0.95f),
        };
        public static readonly Color[] FruitColors =
        {
            new Color(0.98f, 0.55f, 0.12f),
            new Color(0.85f, 0.15f, 0.15f),
            new Color(0.35f, 0.70f, 0.20f),
            new Color(0.95f, 0.85f, 0.20f),
        };
        public static readonly Color[] SignColors =
        {
            new Color(0.95f, 0.95f, 0.90f),
            new Color(0.85f, 0.95f, 0.85f),
            new Color(0.95f, 0.85f, 0.80f),
        };
    }
}
