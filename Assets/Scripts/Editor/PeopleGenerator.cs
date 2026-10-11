using System.Collections.Generic;
using TukTuk.Art;
using UnityEditor;
using UnityEngine;

namespace TukTuk.EditorTools
{
    /// <summary>
    /// Menu: TukTuk > Genera persone (30 modelli).
    /// Uomini in galabeya o abiti occidentali, donne in abito con foulard, bambini, ambulanti con turbante o kufi,
    /// anziani con tarbush. Ogni prefab ha i pivot "Hip_L", "Hip_R", "Shoulder_L", "Shoulder_R" per l'animazione.
    /// </summary>
    public static class PeopleGenerator
    {
        private const string Root = "Assets/Art/Generated";

        private static readonly Color[] RobeColors =
        {
            new Color(0.95f, 0.93f, 0.85f), new Color(0.97f, 0.97f, 0.95f), new Color(0.55f, 0.75f, 0.90f),
            new Color(0.85f, 0.72f, 0.50f), new Color(0.40f, 0.52f, 0.30f), new Color(0.50f, 0.15f, 0.20f),
            new Color(0.18f, 0.25f, 0.45f), new Color(0.90f, 0.70f, 0.20f),
        };
        private static readonly Color[] ShirtColors =
        {
            new Color(0.85f, 0.25f, 0.20f), new Color(0.25f, 0.55f, 0.80f), new Color(0.95f, 0.95f, 0.95f),
            new Color(0.30f, 0.65f, 0.40f), new Color(0.98f, 0.80f, 0.25f), new Color(0.60f, 0.40f, 0.70f),
        };
        private static readonly Color[] TrouserColors =
        {
            new Color(0.25f, 0.25f, 0.28f), new Color(0.40f, 0.30f, 0.20f), new Color(0.15f, 0.18f, 0.30f),
        };
        private static readonly Color[] Skins =
        {
            new Color(0.96f, 0.82f, 0.68f), new Color(0.85f, 0.65f, 0.48f),
            new Color(0.66f, 0.46f, 0.32f), new Color(0.48f, 0.32f, 0.22f),
        };
        private static readonly Color[] ScarfColors =
        {
            new Color(0.95f, 0.95f, 0.92f), new Color(0.20f, 0.20f, 0.25f), new Color(0.85f, 0.35f, 0.30f),
            new Color(0.30f, 0.55f, 0.65f), new Color(0.75f, 0.60f, 0.25f),
        };
        private static readonly Color Hair = new Color(0.08f, 0.06f, 0.05f);
        private static readonly Color Shoe = new Color(0.30f, 0.18f, 0.10f);
        private static readonly Color Tarbush = new Color(0.75f, 0.12f, 0.10f);

        [MenuItem("TukTuk/Genera persone (30 modelli)")]
        public static void Generate()
        {
            EnsureFolder(Root, "People");
            var rng = new System.Random(21);
            for (int i = 0; i < 30; i++)
            {
                Build(i, rng);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TukTuk] Generate 30 persone in " + Root + "/People.");
        }

        private static void Build(int i, System.Random rng)
        {
            bool female = i % 3 == 0;
            bool child = i % 7 == 6;
            bool elder = !child && !female && i % 5 == 2;
            bool robe = female || (!child && rng.NextDouble() < 0.6);
            Color skin = Skins[rng.Next(Skins.Length)];
            Color robeColor = RobeColors[rng.Next(RobeColors.Length)];
            Color shirt = ShirtColors[rng.Next(ShirtColors.Length)];
            Color trouser = TrouserColors[rng.Next(TrouserColors.Length)];

            string name = (child ? "Bambino" : (female ? "Donna" : "Uomo")) + "_" + (i + 1);
            var root = new GameObject(name);
            string key = name;
            int idx = 0;

            Material skinMat = Mat("white", skin);
            Material hairMat = Mat("white", Hair);
            Material robeMat = Mat("white", robeColor);
            Material shirtMat = Mat("white", shirt);
            Material trouserMat = Mat("white", trouser);
            Material shoeMat = Mat("white", Shoe);

            // Gambe: pivot all'anca, animate dal codice del pedone.
            var hipL = Pivot(root, "Hip_L", new Vector3(-0.1f, 0.9f, 0f));
            var hipR = Pivot(root, "Hip_R", new Vector3(0.1f, 0.9f, 0f));
            Piece(hipL, "Gamba", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.08f, 0.9f, 0.06f, 3), 12),
                  robe ? robeMat : trouserMat, new Vector3(0f, -0.9f, 0f), Quaternion.identity, key, ref idx);
            Piece(hipR, "Gamba", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.08f, 0.9f, 0.06f, 3), 12),
                  robe ? robeMat : trouserMat, new Vector3(0f, -0.9f, 0f), Quaternion.identity, key, ref idx);
            Piece(hipL, "Scarpa", MeshKit.RoundedBox(new Vector3(0.14f, 0.08f, 0.26f), 0.03f, 4),
                  shoeMat, new Vector3(0f, -0.94f, 0.04f), Quaternion.identity, key, ref idx);
            Piece(hipR, "Scarpa", MeshKit.RoundedBox(new Vector3(0.14f, 0.08f, 0.26f), 0.03f, 4),
                  shoeMat, new Vector3(0f, -0.94f, 0.04f), Quaternion.identity, key, ref idx);

            // Busto: galabeya (tunica lunga) oppure camicia e pantaloni.
            if (robe)
            {
                Piece(root, "Galabeya", MeshKit.Lathe(new List<Vector2>
                {
                    new Vector2(0.26f, 0.02f), new Vector2(0.28f, 0.45f), new Vector2(0.25f, 0.9f),
                    new Vector2(0.23f, 1.2f), new Vector2(0.21f, 1.42f), new Vector2(0.18f, 1.50f),
                }, 20), robeMat, Vector3.zero, Quaternion.identity, key, ref idx);
            }
            else
            {
                Piece(root, "Busto", MeshKit.RoundedBox(new Vector3(0.42f, 0.62f, 0.24f), 0.10f, 6),
                      shirtMat, new Vector3(0f, 1.2f, 0f), Quaternion.identity, key, ref idx);
            }

            // Collo e testa.
            Piece(root, "Collo", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.05f, 0.12f, 0.03f, 2), 10),
                  skinMat, new Vector3(0f, 1.44f, 0f), Quaternion.identity, key, ref idx);
            Piece(root, "Testa", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.12f, 0.14f, 14), 16),
                  skinMat, new Vector3(0f, 1.6f, 0f), Quaternion.identity, key, ref idx);

            // Capelli, foulard o copricapo.
            if (female)
            {
                Color scarf = ScarfColors[rng.Next(ScarfColors.Length)];
                Piece(root, "Foulard", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.165f, 0.17f, 14, true), 18),
                      Mat("white", scarf), new Vector3(0f, 1.6f, -0.02f), Quaternion.identity, key, ref idx);
            }
            else if (elder && rng.NextDouble() < 0.5)
            {
                Piece(root, "Tarbush", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.13f, 0.14f, 0.05f, 3), 16),
                      Mat("white", Tarbush), new Vector3(0f, 1.72f, 0f), Quaternion.identity, key, ref idx);
            }
            else if (!robe || rng.NextDouble() < 0.5)
            {
                Piece(root, "Capelli", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.125f, 0.07f, 12, true), 16),
                      hairMat, new Vector3(0f, 1.67f, 0f), Quaternion.identity, key, ref idx);
            }
            else
            {
                // Ambulante: turbante o kufi bianco.
                if (rng.NextDouble() < 0.5)
                {
                    Piece(root, "Turbante", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.17f, 0.12f, 14, true), 18),
                          Mat("white", ScarfColors[rng.Next(ScarfColors.Length)]),
                          new Vector3(0f, 1.72f, 0f), Quaternion.identity, key, ref idx);
                }
                else
                {
                    Piece(root, "Kufi", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.12f, 0.1f, 0.04f, 3), 16),
                          Mat("white", new Color(0.97f, 0.97f, 0.95f)), new Vector3(0f, 1.70f, 0f), Quaternion.identity, key, ref idx);
                }
            }

            // Barba per gli anziani.
            if (elder && !female)
            {
                Piece(root, "Barba", MeshKit.RoundedBox(new Vector3(0.14f, 0.12f, 0.08f), 0.04f, 4),
                      hairMat, new Vector3(0f, 1.5f, 0.1f), Quaternion.identity, key, ref idx);
            }

            // Braccia: pivot alla spalla, con la manica del colore del busto.
            Material sleeve = robe ? robeMat : shirtMat;
            var shL = Pivot(root, "Shoulder_L", new Vector3(-0.26f, 1.44f, 0f));
            var shR = Pivot(root, "Shoulder_R", new Vector3(0.26f, 1.44f, 0f));
            foreach (Transform sh in new[] { shL.transform, shR.transform })
            {
                Piece(sh, "Braccio", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(0.065f, 0.62f, 0.03f, 3), 10),
                      sleeve, new Vector3(0f, -0.62f, 0f), Quaternion.identity, key, ref idx);
                Piece(sh, "Mano", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.06f, 0.06f, 8), 10),
                      skinMat, new Vector3(0f, -0.7f, 0f), Quaternion.identity, key, ref idx);
            }

            if (child) root.transform.localScale = Vector3.one * 0.65f;

            string path = $"{Root}/People/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ---------- helper condivisi ----------

        private static Transform Pivot(GameObject parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        private static void Piece(Object owner, string name, MeshBuilder b, Material mat,
                                  Vector3 pos, Quaternion rot, string key, ref int idx)
        {
            Transform parent = owner is GameObject g ? g.transform : (Transform)owner;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            var mesh = b.ToMesh(key + "_" + name + "_" + idx);
            string path = $"{Root}/Meshes/{key}_{name}_{idx}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            idx++;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // Overload per i pivot (Transform come genitore).
        private static void Piece(Transform parent, string name, MeshBuilder b, Material mat,
                                  Vector3 pos, Quaternion rot, string key, ref int idx)
        {
            Piece((Object)parent, name, b, mat, pos, rot, key, ref idx);
        }

        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        /// <summary>Materiale cartoon, texture (nome in Generated/Textures) e colore.</summary>
        public static Material Mat(string texName, Color color)
        {
            string key = texName + "_" + ColorUtility.ToHtmlStringRGB(color);
            if (cache.TryGetValue(key, out Material m)) return m;
            string path = $"{Root}/Materials/{key}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("TukTuk/Toon"));
                AssetDatabase.CreateAsset(m, path);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Textures/{texName}.png");
            m.SetTexture("_MainTex", tex);
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            cache[key] = m;
            return m;
        }

        public static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
