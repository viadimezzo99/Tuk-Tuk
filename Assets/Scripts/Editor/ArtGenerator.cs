using System.Collections.Generic;
using System.IO;
using TukTuk.Art;
using TukTuk.Gameplay;
using UnityEditor;
using UnityEngine;

namespace TukTuk.EditorTools
{
    /// <summary>
    /// Menu: TukTuk > Genera modelli 3D.
    /// Crea texture PNG, materiali cartoon e 20 prefab di edifici (5 quartieri x 4 varianti)
    /// in Assets/Art/Generated. Ogni prefab è un pezzo modulare, con il fronte verso +Z.
    /// </summary>
    public static class ArtGenerator
    {
        private const string Root = "Assets/Art/Generated";
        private static readonly string[] DistrictNames = { "Giza", "Residenziale", "Downtown", "Islamico", "Zamalek" };

        private static readonly Color[][] Palettes =
        {
            new[] { new Color(0.90f, 0.78f, 0.55f), new Color(0.85f, 0.70f, 0.48f), new Color(0.95f, 0.88f, 0.70f), new Color(0.80f, 0.62f, 0.42f) },
            new[] { new Color(0.84f, 0.54f, 0.40f), new Color(0.92f, 0.86f, 0.74f), new Color(0.72f, 0.76f, 0.76f), new Color(0.62f, 0.72f, 0.66f) },
            new[] { new Color(0.98f, 0.92f, 0.82f), new Color(0.96f, 0.80f, 0.74f), new Color(0.90f, 0.95f, 0.97f), new Color(0.97f, 0.88f, 0.70f) },
            new[] { new Color(0.88f, 0.72f, 0.50f), new Color(0.80f, 0.62f, 0.42f), new Color(0.94f, 0.86f, 0.66f), new Color(0.72f, 0.52f, 0.34f) },
            new[] { new Color(0.96f, 0.96f, 0.90f), new Color(0.78f, 0.88f, 0.80f), new Color(0.82f, 0.90f, 0.96f), new Color(0.92f, 0.86f, 0.78f) },
        };

        private static readonly Color Cream = new Color(0.98f, 0.95f, 0.88f);
        private static readonly Color Wood = new Color(0.45f, 0.28f, 0.16f);
        private static readonly Color Steel = new Color(0.72f, 0.74f, 0.78f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        private static readonly Color Mosaic = new Color(0.25f, 0.62f, 0.80f);
        private static readonly Color Awning = new Color(0.90f, 0.25f, 0.20f);

        private class Spec
        {
            public string Name;
            public int District;
            public float W, D, H;
            public Color Body;
            public int Floors;
            public bool Dome, Minaret, Tank, Shop, Mashrabiya, Balconies;
            public FacadeLook Look;
        }

        [MenuItem("TukTuk/Genera modelli 3D")]
        public static void Generate()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets/Art", "Generated");
            foreach (string sub in new[] { "Textures", "Materials", "Meshes", "Buildings" })
                EnsureFolder(Root, sub);

            var textures = ExportTextures();
            var specs = MakeSpecs();
            int count = 0;
            foreach (Spec spec in specs)
            {
                BuildBuilding(spec, textures);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TukTuk] Generati {count} edifici in {Root}/Buildings.");
        }

        // ---------- texture e materiali ----------

        private static Dictionary<string, Texture2D> ExportTextures()
        {
            var map = new Dictionary<string, Texture2D>
            {
                { "facade", ProceduralTextures.Facade },
                { "sidewalk", ProceduralTextures.Sidewalk },
                { "awning", ProceduralTextures.Awning },
                { "plank", ProceduralTextures.Plank },
                { "lattice", ProceduralTextures.Lattice },
                { "mosaic", ProceduralTextures.Mosaic },
                { "sign", ProceduralTextures.Sign },
                { "white", ProceduralTextures.White },
            };
            var result = new Dictionary<string, Texture2D>();
            foreach (var kv in map)
                result[kv.Key] = SaveTexture(kv.Value, kv.Key);
            return result;
        }

        private static Texture2D SaveTexture(Texture2D src, string name)
        {
            string path = $"{Root}/Textures/{name}.png";
            File.WriteAllBytes(path, src.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.filterMode = FilterMode.Bilinear;
                ti.mipmapEnabled = true;
                ti.sRGBTexture = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        /// <summary>Materiale cartoon con texture e colore; creato una volta sola e riutilizzato.</summary>
        private static Material Mat(Dictionary<string, Texture2D> tex, string texKey, Color color)
        {
            string key = texKey + "_" + ColorUtility.ToHtmlStringRGB(color);
            if (materialCache.TryGetValue(key, out Material cached)) return cached;

            string path = $"{Root}/Materials/{key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("TukTuk/Toon");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_MainTex", tex[texKey]);
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            materialCache[key] = mat;
            return mat;
        }

        // ---------- mesh e prefab ----------

        private static Mesh SaveMesh(MeshBuilder b, string name)
        {
            Mesh m = b.ToMesh(name);
            string path = $"{Root}/Meshes/{name}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static int meshCounter;

        internal static void Part(GameObject root, string name, MeshBuilder b, Material mat,
                                 Vector3 pos, Quaternion rot, string meshKey)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = SaveMesh(b, meshKey + "_" + name + "_" + (meshCounter++));
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private static void BuildBuilding(Spec s, Dictionary<string, Texture2D> tex)
        {
            var root = new GameObject(s.Name);
            string key = s.Name;

            Material facade = Mat(tex, "facade", s.Body);
            Material trim = Mat(tex, "white", Cream);
            Material wood = Mat(tex, "plank", Wood);
            Material steel = Mat(tex, "white", Steel);
            Material dome = Mat(tex, "mosaic", Mosaic);
            Material gold = Mat(tex, "white", Gold);
            Material awning = Mat(tex, "awning", Awning);
            Material lattice = Mat(tex, "lattice", Wood);

            // Corpo con spigoli smussati: la parte centrale e la facciata.
            Part(root, "Corpo", MeshKit.RoundedBox(new Vector3(s.W, s.H, s.D), 0.35f, 10, 0.25f),
                 facade, new Vector3(0f, s.H * 0.5f, 0f), Quaternion.identity, key);

            // Cornicione in cima.
            Part(root, "Cornicione", MeshKit.RoundedBox(new Vector3(s.W + 0.5f, 0.5f, s.D + 0.5f), 0.2f, 6),
                 trim, new Vector3(0f, s.H + 0.1f, 0f), Quaternion.identity, key);

            // Negozio: tendone e insegna.
            if (s.Shop)
            {
                Part(root, "Tendone", MeshKit.RoundedBox(new Vector3(s.W * 0.9f, 0.14f, 1.1f), 0.05f, 6),
                     awning, new Vector3(0f, 3.5f, s.D * 0.5f + 0.5f), Quaternion.Euler(-8f, 0f, 0f), key);
                Part(root, "Insegna", MeshKit.RoundedBox(new Vector3(s.W * 0.6f, 0.55f, 0.12f), 0.05f, 4),
                     Mat(tex, "sign", Cream), new Vector3(0f, 3.0f, s.D * 0.5f + 0.1f), Quaternion.identity, key);
            }

            // Facciata modulare: porta, finestre, balconi e dettagli di tetto secondo lo stile del quartiere.
            var batch = new FacadeBatch();
            var fm = new FacadeMaterials
            {
                Frame = trim,
                Glass = Mat(tex, "white", new Color(0.20f, 0.26f, 0.32f)),
                Shutter = Mat(tex, "plank", s.Look.Shutter),
                Lattice = lattice,
                Steel = steel,
                Stone = trim,
                Wood = wood,
            };
            int seed = 0;
            foreach (char c in s.Name) seed = seed * 31 + c;
            var facadeRng = new System.Random(seed);
            FacadeKit.Door(batch, s.W, s.D, s.Look, fm);
            FacadeKit.Windows(batch, s.W, s.D, s.Floors, s.Look, fm);
            FacadeKit.Balconies(batch, s.W, s.D, s.Floors, s.Look, fm, facadeRng);
            FacadeKit.Roof(batch, s.W, s.H, s.D, s.Floors, s.Look, fm, facadeRng);
            batch.Flush(root, key, "Facciata");

            // Cupola a mosaico: solido di rotazione con profilo a semiellisse.
            if (s.Dome)
            {
                float rad = s.W * 0.38f;
                Part(root, "Cupola", MeshKit.Lathe(MeshKit.EllipsoidProfile(rad, rad * 0.85f, 18, true), 28),
                     dome, new Vector3(0f, s.H + 0.5f, 0f), Quaternion.identity, key);
                Part(root, "PuntaCupola", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.25f, 0.25f, 8), 12),
                     gold, new Vector3(0f, s.H + 0.5f + rad * 0.85f, 0f), Quaternion.identity, key);
            }

            // Minareto: torre con smusso e bulbo dorato.
            if (s.Minaret)
            {
                float mh = 9f;
                Part(root, "Minareto", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.7f, mh, 0.25f, 4), 20),
                     trim, new Vector3(s.W * 0.32f, s.H, s.D * 0.32f), Quaternion.identity, key);
                Part(root, "BulboMinareto", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.6f, 0.9f, 12), 16),
                     gold, new Vector3(s.W * 0.32f, s.H + mh, s.D * 0.32f), Quaternion.identity, key);
            }

            // Serbatoio dell'acqua sul tetto.
            if (s.Tank)
            {
                Part(root, "Serbatoio", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.9f, 1.5f, 0.3f, 4), 20),
                     wood, new Vector3(-s.W * 0.22f, s.H + 0.5f, -s.D * 0.18f), Quaternion.identity, key);
            }

            string path = $"{Root}/Buildings/{s.Name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ---------- specifiche dei 20 edifici ----------

        private static List<Spec> MakeSpecs()
        {
            var list = new List<Spec>();
            var rng = new System.Random(7);
            for (int d = 0; d < DistrictNames.Length; d++)
            {
                for (int v = 0; v < 6; v++)
                {
                    var s = new Spec
                    {
                        District = d,
                        Name = $"Edificio_{DistrictNames[d]}_{v + 1}",
                        Body = Palettes[d][v % 4],
                    };
                    switch (d)
                    {
                        case 0: // Giza: case basse, terrazze con serbatoi
                            s.W = 6.5f; s.D = 6f; s.Floors = 2 + v % 2;
                            s.Tank = true; s.Balconies = false; break;
                        case 1: // Residenziale: palazzi densi, balconi, negozi
                            s.W = 6f; s.D = 6f; s.Floors = 5 + rng.Next(0, 4);
                            s.Balconies = true; s.Shop = v % 2 == 0; s.Tank = true; break;
                        case 2: // Downtown: palazzi alti, balconi con ringhiera
                            s.W = 7f; s.D = 7f; s.Floors = 6 + rng.Next(0, 4);
                            s.Balconies = true; s.Shop = v % 2 == 0; break;
                        case 3: // Islamico: grate a mashrabiya, cupole, minareti
                            s.W = 6f; s.D = 6f; s.Floors = 3 + rng.Next(0, 3);
                            s.Balconies = true; s.Mashrabiya = true;
                            s.Dome = v < 3; s.Minaret = v == 0 || v == 2; s.Tank = v == 1; break;
                        default: // Zamalek: pastelli, ville medie
                            s.W = 7f; s.D = 7f; s.Floors = 4 + rng.Next(0, 3);
                            s.Balconies = true; s.Dome = v == 3; break;
                    }
                    s.Look = FacadeKit.Pick(d, rng);
            s.Shop = s.Look.Shop;
            s.Balconies = s.Look.Balcony != BalconyStyle.None;

            // Ogni edificio occupa esattamente un lotto: cambiano altezza e dettagli, non l'impronta.
            s.W = TukTuk.Gameplay.CityLayout.LotWidth;
            s.D = TukTuk.Gameplay.CityLayout.LotDepth;
            s.H = s.Floors * 3.2f + 0.6f;
                    list.Add(s);
                }
            }
            return list;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
