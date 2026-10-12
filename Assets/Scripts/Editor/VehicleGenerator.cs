using System.Collections.Generic;
using TukTuk.Art;
using TukTuk.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TukTuk.EditorTools
{
    /// <summary>
    /// Menu: TukTuk > Genera carretti e moto (asini e rider).
    /// - Carretti (6): cassone di legno trainato da un asino, con una merce diversa per ciascuno
    ///   (vasi di terracotta, pane, lampade d'ottone, meloni e cassette, tappeti, cesti di spezie).
    /// - Moto (6): motocicli con rider e pacco di consegna, come quelli che attraversano il Cairo.
    /// Origine a terra al centro del mezzo, fronte su +Z (verso dove va il traffico).
    /// </summary>
    public static class VehicleGenerator
    {
        private const string Root = "Assets/Art/Generated";

        private static readonly Color Donkey = new Color(0.55f, 0.45f, 0.38f);
        private static readonly Color DonkeyDark = new Color(0.30f, 0.24f, 0.20f);
        private static readonly Color CartWood = new Color(0.62f, 0.42f, 0.25f);
        private static readonly Color CartWoodDark = new Color(0.42f, 0.28f, 0.16f);
        private static readonly Color Tyre = new Color(0.12f, 0.12f, 0.12f);
        private static readonly Color Metal = new Color(0.45f, 0.45f, 0.50f);
        private static readonly Color Terracotta = new Color(0.78f, 0.42f, 0.25f);
        private static readonly Color Brass = new Color(0.85f, 0.65f, 0.25f);
        private static readonly Color Bread = new Color(0.90f, 0.75f, 0.45f);
        private static readonly Color Melon = new Color(0.25f, 0.62f, 0.30f);
        private static readonly Color Orange = new Color(0.98f, 0.55f, 0.15f);
        private static readonly Color Wicker = new Color(0.80f, 0.65f, 0.35f);
        private static readonly Color Cloth = new Color(0.95f, 0.92f, 0.85f);
        private static readonly Color Skin = new Color(0.66f, 0.46f, 0.32f);
        private static readonly Color Cardboard = new Color(0.80f, 0.60f, 0.35f);

        private static readonly Color[] Rugs =
        {
            new Color(0.75f, 0.15f, 0.20f), new Color(0.20f, 0.35f, 0.70f),
            new Color(0.85f, 0.60f, 0.20f), new Color(0.30f, 0.55f, 0.35f),
        };
        private static readonly Color[] Spices =
        {
            new Color(0.95f, 0.75f, 0.15f), new Color(0.80f, 0.20f, 0.10f),
            new Color(0.55f, 0.30f, 0.12f), new Color(0.85f, 0.45f, 0.10f),
        };
        private static readonly Color[] BikeColors =
        {
            new Color(0.85f, 0.20f, 0.20f), new Color(0.15f, 0.45f, 0.85f),
            new Color(0.95f, 0.85f, 0.20f), new Color(0.20f, 0.65f, 0.35f),
        };
        private static readonly Color[] Shirts =
        {
            new Color(0.95f, 0.95f, 0.92f), new Color(0.90f, 0.40f, 0.20f),
            new Color(0.25f, 0.55f, 0.80f), new Color(0.30f, 0.30f, 0.35f),
        };
        private static readonly Color[] Helmets =
        {
            new Color(0.95f, 0.95f, 0.95f), new Color(0.85f, 0.15f, 0.15f), new Color(0.95f, 0.80f, 0.15f),
        };

        [MenuItem("TukTuk/Genera carretti e moto (asini e rider)")]
        public static void Generate()
        {
            PeopleGenerator.EnsureFolder(Root, "Meshes");
            PeopleGenerator.EnsureFolder(Root, "Stalls");
            PeopleGenerator.EnsureFolder(Root, "Vehicles");
            var rng = new System.Random(33);
            for (int i = 0; i < 6; i++) BuildCart(i, rng);
            for (int i = 0; i < 6; i++) BuildMoto(i, rng);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TukTuk] Generati 6 carretti con asino e 6 moto in " + Root + ".");
        }

        // ---------- carretto con asino ----------

        private static void BuildCart(int i, System.Random rng)
        {
            var rig = new Rig("Carretto_" + (i + 1));
            Color wood = CartWood;

            // Cassone e fiancate.
            rig.Add("Cassone", MeshKit.RoundedBox(new Vector3(1.25f, 0.12f, 2.0f), 0.04f, 4), wood,
                    new Vector3(0f, 0.55f, 0f), Vector3.zero);
            rig.Add("FiancataS", Box(new Vector3(0.08f, 0.4f, 2.0f)), CartWoodDark, new Vector3(-0.6f, 0.8f, 0f), Vector3.zero);
            rig.Add("FiancataD", Box(new Vector3(0.08f, 0.4f, 2.0f)), CartWoodDark, new Vector3(0.6f, 0.8f, 0f), Vector3.zero);
            rig.Add("Retro", Box(new Vector3(1.25f, 0.4f, 0.08f)), CartWoodDark, new Vector3(0f, 0.8f, -1.0f), Vector3.zero);

            // Stanghe che collegano il carretto all'asino.
            rig.Add("StangaS", Box(new Vector3(0.06f, 0.06f, 1.4f)), CartWoodDark, new Vector3(-0.35f, 0.6f, 1.6f), Vector3.zero);
            rig.Add("StangaD", Box(new Vector3(0.06f, 0.06f, 1.4f)), CartWoodDark, new Vector3(0.35f, 0.6f, 1.6f), Vector3.zero);

            // Ruote.
            foreach (float x in new[] { -0.78f, 0.78f })
            {
                rig.Add("Ruota", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.45f, 0.1f, 0.04f, 2), 14), Tyre,
                        new Vector3(x, 0.45f, 0f), new Vector3(0f, 0f, 90f));
            }

            // Asino (fronte su +Z).
            rig.Add("Corpo", MeshKit.RoundedBox(new Vector3(0.55f, 0.6f, 1.1f), 0.18f, 6), Donkey,
                    new Vector3(0f, 0.85f, 1.6f), Vector3.zero);
            rig.Add("Collo", MeshKit.RoundedBox(new Vector3(0.26f, 0.45f, 0.3f), 0.1f, 4), Donkey,
                    new Vector3(0f, 1.15f, 2.2f), new Vector3(-30f, 0f, 0f));
            rig.Add("Testa", MeshKit.RoundedBox(new Vector3(0.24f, 0.3f, 0.42f), 0.12f, 4), Donkey,
                    new Vector3(0f, 1.35f, 2.45f), Vector3.zero);
            rig.Add("Muso", MeshKit.RoundedBox(new Vector3(0.16f, 0.14f, 0.2f), 0.06f, 3), Cloth,
                    new Vector3(0f, 1.22f, 2.7f), Vector3.zero);
            rig.Add("OrecchioS", MeshKit.RoundedBox(new Vector3(0.05f, 0.2f, 0.05f), 0.02f, 2), Donkey,
                    new Vector3(-0.07f, 1.6f, 2.35f), Vector3.zero);
            rig.Add("OrecchioD", MeshKit.RoundedBox(new Vector3(0.05f, 0.2f, 0.05f), 0.02f, 2), Donkey,
                    new Vector3(0.07f, 1.6f, 2.35f), Vector3.zero);
            // Zampe: ognuna ruota sull'anca (pivot), così il passo si può animare.
            var legFL = rig.Pivot("Zampe_FL", new Vector3(-0.2f, 0.6f, 2.0f));
            var legFR = rig.Pivot("Zampe_FR", new Vector3(0.2f, 0.6f, 2.0f));
            var legBL = rig.Pivot("Zampe_BL", new Vector3(-0.2f, 0.6f, 1.25f));
            var legBR = rig.Pivot("Zampe_BR", new Vector3(0.2f, 0.6f, 1.25f));
            foreach (Transform hip in new[] { legFL, legFR, legBL, legBR })
                rig.Add("Zampa", MeshKit.RoundedBox(new Vector3(0.1f, 0.6f, 0.1f), 0.03f, 2), Donkey,
                        new Vector3(0f, -0.3f, 0f), Vector3.zero, hip);

            // Cavaliere seduto sull'asino: il pivot "Cavaliere" lo fa ondeggiare mentre cammina.
            var seat = rig.Pivot("Cavaliere", new Vector3(0f, 1.15f, 1.6f));
            Color robe = Shirts[rng.Next(Shirts.Length)];
            rig.Add("Busto", Box(new Vector3(0.42f, 0.5f, 0.3f)), robe, new Vector3(0f, 0.3f, 0f), Vector3.zero, seat);
            rig.Add("Testa", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.12f, 0.13f, 10), 12), Skin,
                    new Vector3(0f, 0.7f, 0f), Vector3.zero, seat);
            rig.Add("Turbante", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.14f, 0.1f, 10, true), 12), Cloth,
                    new Vector3(0f, 0.8f, 0f), Vector3.zero, seat);
            foreach (float x in new[] { -0.22f, 0.22f })
                rig.Add("Gamba", MeshKit.RoundedBox(new Vector3(0.14f, 0.45f, 0.14f), 0.05f, 2), robe,
                        new Vector3(x, 0.05f, 0.12f), new Vector3(-70f, 0f, 0f), seat);
            rig.Add("Coda", MeshKit.RoundedBox(new Vector3(0.05f, 0.35f, 0.05f), 0.02f, 2), DonkeyDark,
                    new Vector3(0f, 0.95f, 1.0f), new Vector3(30f, 0f, 0f));
            rig.Add("Pettorale", MeshKit.RoundedBox(new Vector3(0.66f, 0.07f, 0.08f), 0.02f, 2), DonkeyDark,
                    new Vector3(0f, 0.98f, 1.95f), Vector3.zero);

            // Merce sul cassone (la base è sopra il cassone, y = 0.61).
            BuildGoods(rig, i % 6, rng);

            // Tendone su quattro pali, sui carretti pari.
            if (i % 2 == 0)
            {
                foreach (float px in new[] { -0.55f, 0.55f })
                {
                    foreach (float pz in new[] { -0.9f, 0.9f })
                    {
                        rig.Add("Palo", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.04f, 1.9f, 0.01f, 2), 8),
                                Metal, new Vector3(px, 0.61f, pz), Vector3.zero);
                    }
                }
                rig.Add("Tendone", MeshKit.RoundedBox(new Vector3(1.3f, 0.04f, 2.0f), 0.02f, 3),
                        Rugs[rng.Next(Rugs.Length)], new Vector3(0f, 2.5f, 0f), Vector3.zero);
            }

            rig.Root.AddComponent<DonkeyWalk>();
            rig.Save(Root + "/Stalls/" + rig.Key + ".prefab");
        }

        private static void BuildGoods(Rig rig, int kind, System.Random rng)
        {
            const float top = 0.61f;
            switch (kind)
            {
                case 0: // vasi di terracotta
                    foreach (float x in new[] { -0.3f, 0.3f })
                        foreach (float z in new[] { -0.4f, 0.4f })
                            rig.Add("Vaso", MeshKit.Lathe(new List<Vector2>
                            {
                                new Vector2(0f, 0f), new Vector2(0.22f, 0f), new Vector2(0.3f, 0.25f),
                                new Vector2(0.25f, 0.55f), new Vector2(0.15f, 0.7f), new Vector2(0.12f, 0.8f),
                            }, 14), Terracotta, new Vector3(x, top, z), Vector3.zero);
                    break;

                case 1: // pane in pile
                    foreach (float x in new[] { -0.35f, 0f, 0.35f })
                        foreach (float z in new[] { -0.5f, 0.5f })
                            for (int k = 0; k < 4; k++)
                                rig.Add("Pane", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.22f, 0.07f, 10), 12), Bread,
                                        new Vector3(x, top + k * 0.12f, z), Vector3.zero);
                    break;

                case 2: // lampade d'ottone
                    foreach (float x in new[] { -0.3f, 0.3f })
                        foreach (float z in new[] { -0.4f, 0.4f })
                            rig.Add("Lampada", MeshKit.Lathe(new List<Vector2>
                            {
                                new Vector2(0f, 0f), new Vector2(0.12f, 0f), new Vector2(0.18f, 0.12f),
                                new Vector2(0.1f, 0.3f), new Vector2(0.22f, 0.35f), new Vector2(0.08f, 0.5f),
                                new Vector2(0.03f, 0.62f),
                            }, 14), Brass, new Vector3(x, top, z), Vector3.zero);
                    break;

                case 3: // meloni e cassette d'arance
                    foreach (float x in new[] { -0.35f, 0f, 0.35f })
                        rig.Add("Melone", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.2f, 0.2f, 10), 12), Melon,
                                new Vector3(x, top + 0.2f, -0.5f), Vector3.zero);
                    rig.Add("Cassa", Box(new Vector3(0.45f, 0.25f, 0.45f)), Orange, new Vector3(-0.3f, top + 0.12f, 0.45f), Vector3.zero);
                    rig.Add("Cassa", Box(new Vector3(0.45f, 0.25f, 0.45f)), Orange, new Vector3(0.3f, top + 0.12f, 0.45f), Vector3.zero);
                    break;

                case 4: // tappeti arrotolati
                    foreach (float x in new[] { -0.36f, 0f, 0.36f })
                        rig.Add("Tappeto", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.18f, 1.1f, 0.06f, 3), 12),
                                Rugs[rng.Next(Rugs.Length)], new Vector3(x, top + 0.18f, 0f), new Vector3(90f, 0f, 0f));
                    break;

                default: // cesti di spezie
                    foreach (float x in new[] { -0.4f, 0f, 0.4f })
                    {
                        rig.Add("Cesto", MeshKit.Lathe(new List<Vector2>
                        {
                            new Vector2(0.001f, 0f), new Vector2(0.3f, 0.02f), new Vector2(0.36f, 0.22f),
                            new Vector2(0.3f, 0.3f), new Vector2(0.001f, 0.3f),
                        }, 14), Wicker, new Vector3(x, top, 0f), Vector3.zero);
                        rig.Add("Spezie", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.28f, 0.1f, 10), 12),
                                Spices[rng.Next(Spices.Length)], new Vector3(x, top + 0.3f, 0f), Vector3.zero);
                    }
                    break;
            }
        }

        // ---------- moto con rider ----------

        private static void BuildMoto(int i, System.Random rng)
        {
            var rig = new Rig("Moto_" + (i + 1));
            Color bike = BikeColors[i % BikeColors.Length];
            Color shirt = Shirts[rng.Next(Shirts.Length)];
            Color helmet = Helmets[rng.Next(Helmets.Length)];

            // Ruote.
            foreach (float z in new[] { -0.62f, 0.62f })
            {
                rig.Add("Ruota", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.32f, 0.12f, 0.05f, 2), 14), Tyre,
                        new Vector3(0f, 0.32f, z), new Vector3(0f, 0f, 90f));
            }

            // Telaio, forcella, manubrio, sella.
            rig.Add("Telaio", MeshKit.RoundedBox(new Vector3(0.2f, 0.35f, 1.1f), 0.06f, 4), bike,
                    new Vector3(0f, 0.55f, 0f), Vector3.zero);
            rig.Add("Forcella", MeshKit.RoundedBox(new Vector3(0.08f, 0.6f, 0.08f), 0.02f, 2), Metal,
                    new Vector3(0f, 0.6f, 0.55f), new Vector3(-20f, 0f, 0f));
            rig.Add("Manubrio", MeshKit.RoundedBox(new Vector3(0.7f, 0.05f, 0.05f), 0.02f, 2), Metal,
                    new Vector3(0f, 1.0f, 0.55f), Vector3.zero);
            rig.Add("Sella", MeshKit.RoundedBox(new Vector3(0.28f, 0.1f, 0.55f), 0.04f, 3), DonkeyDark,
                    new Vector3(0f, 0.78f, -0.2f), Vector3.zero);

            // Pacco di consegna sul retro: cartone o scatola di plastica.
            Color box = i % 2 == 0 ? Cardboard : BikeColors[(i + 2) % BikeColors.Length];
            rig.Add("Pacco", MeshKit.RoundedBox(new Vector3(0.6f, 0.5f, 0.5f), 0.03f, 3), box,
                    new Vector3(0f, 0.95f, -0.7f), Vector3.zero);

            // Rider: gambe, busto, braccia, testa, casco.
            foreach (float x in new[] { -0.13f, 0.13f })
                rig.Add("Gamba", MeshKit.RoundedBox(new Vector3(0.14f, 0.5f, 0.14f), 0.05f, 2), shirt,
                        new Vector3(x, 0.75f, 0.1f), new Vector3(-50f, 0f, 0f));
            rig.Add("Busto", MeshKit.RoundedBox(new Vector3(0.42f, 0.5f, 0.26f), 0.1f, 4), shirt,
                    new Vector3(0f, 1.2f, -0.15f), new Vector3(20f, 0f, 0f));
            foreach (float x in new[] { -0.22f, 0.22f })
                rig.Add("Braccio", MeshKit.RoundedBox(new Vector3(0.1f, 0.45f, 0.1f), 0.04f, 2), shirt,
                        new Vector3(x, 1.12f, 0.28f), new Vector3(-60f, 0f, 0f));
            rig.Add("Testa", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.12f, 0.13f, 10), 12), Skin,
                    new Vector3(0f, 1.56f, 0f), Vector3.zero);
            rig.Add("Casco", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.14f, 0.12f, 10, true), 12), helmet,
                    new Vector3(0f, 1.6f, 0f), Vector3.zero);

            rig.Save(Root + "/Vehicles/" + rig.Key + ".prefab");
        }

        // ---------- helper ----------

        private static MeshBuilder Box(Vector3 size)
        {
            return MeshKit.RoundedBox(size, 0.03f, 3);
        }

        /// <summary>Costruisce un prefab pezzo per pezzo; ogni pezzo diventa una mesh salvata come asset.</summary>
        internal class Rig
        {
            public readonly GameObject Root;
            public readonly string Key;
            private int idx;

            public Rig(string key)
            {
                Key = key;
                Root = new GameObject(key);
            }

            public void Add(string name, MeshBuilder b, Color color, Vector3 pos, Vector3 euler, Transform parent = null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent != null ? parent : Root.transform, false);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(euler);

                var mesh = b.ToMesh(Key + "_" + name + "_" + idx);
                string path = $"{MeshFolder}/{Key}_{name}_{idx}.asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
                idx++;

                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = PeopleGenerator.Mat("white", color);
            }

            public Transform Pivot(string name, Vector3 pos)
            {
                var go = new GameObject(name);
                go.transform.SetParent(Root.transform, false);
                go.transform.localPosition = pos;
                return go.transform;
            }

            public void Save(string prefabPath)
            {
                PrefabUtility.SaveAsPrefabAsset(Root, prefabPath);
                Object.DestroyImmediate(Root);
            }
        }

        private const string MeshFolder = Root + "/Meshes";
    }
}
