using System.Collections.Generic;
using TukTuk.Art;
using UnityEngine;

namespace TukTuk.EditorTools
{
    // Vocabolario delle facciate, ricavato dalle fonti su architettura del Cairo:
    // finestre con persiane e mashrabiya, portali in pietra del periodo khedivale,
    // archi nei palazzi islamici, balconi in ferro o chiusi, condizionatori e panni stesi.
    public enum WindowStyle { Plain, Shutter, Arched, Mashrabiya }
    public enum BalconyStyle { None, Iron, Closed, Oriel, Corbel }
    public enum DoorStyle { Wood, Arched, Portal }

    /// <summary>Stile di facciata di un edificio: scelto per quartiere, con variazioni casuali.</summary>
    public class FacadeLook
    {
        public WindowStyle Window;
        public BalconyStyle Balcony;
        public DoorStyle Door;
        public bool Shop;
        public bool Ac;
        public bool Laundry;
        public Color Shutter = new Color(0.20f, 0.45f, 0.60f);
        public int Bays = 3;
    }

    public class FacadeMaterials
    {
        public Material Frame, Glass, Shutter, Lattice, Steel, Stone, Wood;
    }

    /// <summary>Accumula piccole parti per materiale: un solo mesh per materiale, non centinaia di asset.</summary>
    public class FacadeBatch
    {
        private readonly Dictionary<Material, MeshBuilder> parts = new Dictionary<Material, MeshBuilder>();

        public void Box(Material mat, Vector3 size, float radius, Vector3 pos, Quaternion rot)
        {
            if (mat == null) return;
            if (!parts.TryGetValue(mat, out MeshBuilder dst))
            {
                dst = new MeshBuilder();
                parts[mat] = dst;
            }
            dst.Append(MeshKit.RoundedBox(size, radius, 4), Matrix4x4.TRS(pos, rot, Vector3.one));
        }

        public void Flush(GameObject root, string key, string prefix)
        {
            foreach (var kv in parts)
                ArtGenerator.Part(root, prefix + "_" + kv.Key.name, kv.Value, kv.Key,
                                  Vector3.zero, Quaternion.identity, key);
        }
    }

    public static class FacadeKit
    {
        private static readonly Color[] ShutterColors =
        {
            new Color(0.20f, 0.45f, 0.60f),
            new Color(0.15f, 0.50f, 0.35f),
            new Color(0.55f, 0.25f, 0.18f),
            new Color(0.25f, 0.35f, 0.65f),
        };

        /// <summary>Stile di facciata per quartiere (0 Giza, 1 Residenziale, 2 Downtown, 3 Islamico, 4 Zamalek).</summary>
        public static FacadeLook Pick(int district, System.Random rng)
        {
            var l = new FacadeLook
            {
                Shutter = ShutterColors[rng.Next(ShutterColors.Length)],
                Ac = rng.NextDouble() < 0.5,
                Laundry = rng.NextDouble() < 0.4,
            };
            switch (district)
            {
                case 0: // Giza: case basse, finestre semplici, qualche ringhiera
                    l.Window = Choose(rng, WindowStyle.Plain, WindowStyle.Shutter);
                    l.Balcony = Choose(rng, BalconyStyle.None, BalconyStyle.Iron);
                    l.Door = DoorStyle.Wood;
                    l.Shop = rng.NextDouble() < 0.3;
                    break;
                case 1: // Residenziale: mattone, balconi in ferro o chiusi, negozi al piano terra
                    l.Window = Choose(rng, WindowStyle.Plain, WindowStyle.Shutter);
                    l.Balcony = Choose(rng, BalconyStyle.Iron, BalconyStyle.Closed, BalconyStyle.None);
                    l.Door = DoorStyle.Wood;
                    l.Shop = rng.NextDouble() < 0.35;
                    break;
                case 2: // Downtown: palazzi con persiane, balconi chiusi, portali in pietra
                    l.Window = Choose(rng, WindowStyle.Shutter, WindowStyle.Plain);
                    l.Balcony = Choose(rng, BalconyStyle.Closed, BalconyStyle.Iron);
                    l.Door = DoorStyle.Portal;
                    l.Shop = rng.NextDouble() < 0.5;
                    break;
                case 3: // Islamico: archi, mashrabiya, bovindi in legno
                    l.Window = Choose(rng, WindowStyle.Arched, WindowStyle.Mashrabiya);
                    l.Balcony = Choose(rng, BalconyStyle.Oriel, BalconyStyle.None);
                    l.Door = DoorStyle.Arched;
                    l.Shop = rng.NextDouble() < 0.2;
                    break;
                default: // Zamalek: ville con persiane, mensole in pietra, portali
                    l.Window = WindowStyle.Shutter;
                    l.Balcony = Choose(rng, BalconyStyle.Corbel, BalconyStyle.Iron);
                    l.Door = DoorStyle.Portal;
                    l.Shop = rng.NextDouble() < 0.1;
                    break;
            }
            return l;
        }

        private static T Choose<T>(System.Random rng, params T[] options)
        {
            return options[rng.Next(options.Length)];
        }

        /// <summary>Porta del piano terra (a sinistra) e, se c'è, la vetrina del negozio (a destra).</summary>
        public static void Door(FacadeBatch b, float W, float D, FacadeLook look, FacadeMaterials m)
        {
            float zf = D * 0.5f;
            float dx = -W * 0.25f;
            Quaternion none = Quaternion.identity;

            if (look.Door == DoorStyle.Portal)
            {
                b.Box(m.Stone, new Vector3(1.9f, 3.0f, 0.22f), 0.08f, new Vector3(dx, 1.5f, zf + 0.11f), none);
                b.Box(m.Stone, new Vector3(2.2f, 0.28f, 0.34f), 0.10f, new Vector3(dx, 3.12f, zf + 0.17f), none);
            }

            b.Box(m.Wood, new Vector3(1.1f, 2.3f, 0.12f), 0.05f, new Vector3(dx, 1.15f, zf + 0.08f), none);
            if (look.Door == DoorStyle.Arched)
                b.Box(m.Wood, new Vector3(1.1f, 0.35f, 0.12f), 0.16f, new Vector3(dx, 2.45f, zf + 0.08f), none);

            b.Box(m.Stone, new Vector3(1.4f, 0.12f, 0.5f), 0.05f, new Vector3(dx, 0.06f, zf + 0.25f), none);

            if (look.Shop)
            {
                float sx = W * 0.25f;
                b.Box(m.Frame, new Vector3(2.36f, 1.86f, 0.08f), 0.05f, new Vector3(sx, 0.95f, zf + 0.03f), none);
                b.Box(m.Glass, new Vector3(2.2f, 1.7f, 0.1f), 0.04f, new Vector3(sx, 0.95f, zf + 0.07f), none);
            }
        }

        /// <summary>Finestre su ogni piano superiore, una per campata, con lo stile scelto.</summary>
        public static void Windows(FacadeBatch b, float W, float D, int floors, FacadeLook look, FacadeMaterials m)
        {
            float bayW = W / look.Bays;
            float ww = bayW * 0.55f;
            const float wh = 1.4f;
            float zf = D * 0.5f;

            for (int f = 1; f < floors; f++)
            {
                for (int bay = 0; bay < look.Bays; bay++)
                {
                    float x = -W * 0.5f + bayW * (bay + 0.5f);
                    float y = 3.2f * f + 1.5f;

                    b.Box(m.Frame, new Vector3(ww + 0.14f, wh + 0.14f, 0.1f), 0.04f, new Vector3(x, y, zf + 0.03f), Quaternion.identity);
                    if (look.Window == WindowStyle.Mashrabiya)
                        b.Box(m.Lattice, new Vector3(ww, wh, 0.12f), 0.03f, new Vector3(x, y, zf + 0.08f), Quaternion.identity);
                    else
                        b.Box(m.Glass, new Vector3(ww, wh, 0.1f), 0.03f, new Vector3(x, y, zf + 0.07f), Quaternion.identity);

                    // Davanzale in pietra.
                    b.Box(m.Stone, new Vector3(ww + 0.26f, 0.07f, 0.24f), 0.03f,
                          new Vector3(x, y - wh * 0.5f - 0.03f, zf + 0.12f), Quaternion.identity);

                    // Persiane ai lati della finestra.
                    if (look.Window == WindowStyle.Shutter)
                        foreach (float side in new[] { -1f, 1f })
                            b.Box(m.Shutter, new Vector3(0.34f, wh, 0.05f), 0.02f,
                                  new Vector3(x + side * (ww * 0.5f + 0.2f), y, zf + 0.06f), Quaternion.identity);

                    // Arco in cima alla finestra.
                    if (look.Window == WindowStyle.Arched)
                        b.Box(m.Frame, new Vector3(ww + 0.2f, 0.32f, 0.12f), 0.14f,
                              new Vector3(x, y + wh * 0.5f + 0.12f, zf + 0.06f), Quaternion.identity);
                }
            }
        }

        /// <summary>Balconi sotto alcune finestre, scelti con probabilità: stile dal quartiere.</summary>
        public static void Balconies(FacadeBatch b, float W, float D, int floors, FacadeLook look, FacadeMaterials m, System.Random rng)
        {
            if (look.Balcony == BalconyStyle.None) return;
            float bayW = W / look.Bays;
            float zf = D * 0.5f;

            for (int f = 1; f < floors; f++)
            {
                for (int bay = 0; bay < look.Bays; bay++)
                {
                    if (rng.NextDouble() > 0.45) continue;
                    float x = -W * 0.5f + bayW * (bay + 0.5f);
                    float y0 = 3.2f * f;

                    switch (look.Balcony)
                    {
                        case BalconyStyle.Iron:
                            b.Box(m.Frame, new Vector3(bayW * 0.9f, 0.12f, 0.9f), 0.04f, new Vector3(x, y0 + 0.75f, zf + 0.45f), Quaternion.identity);
                            b.Box(m.Steel, new Vector3(bayW * 0.9f, 0.7f, 0.05f), 0.02f, new Vector3(x, y0 + 1.1f, zf + 0.9f), Quaternion.identity);
                            break;
                        case BalconyStyle.Closed:
                            b.Box(m.Frame, new Vector3(bayW * 0.85f, 1.4f, 0.9f), 0.08f, new Vector3(x, y0 + 1.5f, zf + 0.45f), Quaternion.identity);
                            b.Box(m.Glass, new Vector3(bayW * 0.7f, 1.0f, 0.04f), 0.02f, new Vector3(x, y0 + 1.5f, zf + 0.93f), Quaternion.identity);
                            break;
                        case BalconyStyle.Oriel:
                            b.Box(m.Lattice, new Vector3(bayW * 0.9f, 1.3f, 0.9f), 0.05f, new Vector3(x, y0 + 1.5f, zf + 0.45f), Quaternion.identity);
                            b.Box(m.Wood, new Vector3(bayW * 0.98f, 0.12f, 0.98f), 0.04f, new Vector3(x, y0 + 2.2f, zf + 0.5f), Quaternion.identity);
                            break;
                        case BalconyStyle.Corbel:
                            b.Box(m.Stone, new Vector3(bayW * 0.9f, 0.14f, 0.8f), 0.04f, new Vector3(x, y0 + 0.75f, zf + 0.4f), Quaternion.identity);
                            foreach (float side in new[] { -0.3f, 0.3f })
                                b.Box(m.Stone, new Vector3(0.12f, 0.3f, 0.5f), 0.04f,
                                      new Vector3(x + side * bayW, y0 + 0.45f, zf + 0.3f), Quaternion.identity);
                            break;
                    }
                }
            }
        }

        /// <summary>Dettagli sul tetto e sulla facciata: condizionatori e panni stesi.</summary>
        public static void Roof(FacadeBatch b, float W, float H, float D, int floors, FacadeLook look, FacadeMaterials m, System.Random rng)
        {
            float zf = D * 0.5f;
            float bayW = W / look.Bays;

            if (look.Ac && floors > 1)
            {
                int f = rng.Next(1, floors);
                int bay = rng.Next(look.Bays);
                float x = -W * 0.5f + bayW * (bay + 0.5f);
                b.Box(m.Steel, new Vector3(0.8f, 0.6f, 0.6f), 0.05f, new Vector3(x, 3.2f * f + 1.1f, zf + 0.35f), Quaternion.identity);
            }

            if (look.Laundry)
            {
                float zl = zf - 0.6f;
                foreach (float side in new[] { -0.36f, 0.36f })
                    b.Box(m.Steel, new Vector3(0.06f, 1.2f, 0.06f), 0.02f, new Vector3(W * side, H + 0.6f, zl), Quaternion.identity);
                b.Box(m.Steel, new Vector3(W * 0.72f, 0.02f, 0.02f), 0.01f, new Vector3(0f, H + 1.1f, zl), Quaternion.identity);
                for (int i = 0; i < 2; i++)
                {
                    float x = -W * 0.2f + i * W * 0.4f;
                    b.Box(m.Shutter, new Vector3(0.7f, 0.5f, 0.02f), 0.01f, new Vector3(x, H + 0.85f, zl), Quaternion.identity);
                }
            }
        }
    }
}
