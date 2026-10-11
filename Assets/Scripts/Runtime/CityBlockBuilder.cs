using UnityEngine;
using static TukTuk.Gameplay.CityPalette;

namespace TukTuk.Gameplay
{
    /// <summary>Risorse per costruire i blocchi, assegnate da EndlessRoad.</summary>
    public class BlockKit
    {
        public Material Toon;
        public GameObject[] Buildings;   // 30 modelli: 6 per distretto
        public GameObject[] Trees;
        public float StallChance;
    }

    /// <summary>
    /// Costruisce il contenuto di un blocco di strada (asfalto, marciapiedi con varianti,
    /// edifici per lotto, arredi). Usa solo le misure di CityLayout e non conosce il gioco.
    /// </summary>
    public class CityBlockBuilder
    {
        private const float StallX = CityLayout.StallX;
        private const float OuterX = CityLayout.OuterX;

        private static readonly Color[] SidewalkTints =
        {
            new Color(0.93f, 0.84f, 0.64f), // sabbia
            new Color(0.84f, 0.82f, 0.78f), // grigio chiaro
            new Color(0.86f, 0.64f, 0.46f), // terracotta
            new Color(0.96f, 0.92f, 0.80f), // crema
        };
        private static readonly Color Joint = new Color(0.55f, 0.48f, 0.38f);
        private static readonly Color PlanterBrick = new Color(0.72f, 0.42f, 0.28f);
        private static readonly Color Leaf = new Color(0.30f, 0.62f, 0.25f);
        private static readonly Color BinGreen = new Color(0.20f, 0.36f, 0.26f);

        private readonly BlockKit kit;
        private bool warnedMissingBuildings;

        public CityBlockBuilder(BlockKit kit)
        {
            this.kit = kit;
        }

        /// <summary>Ricostruisce tutto il blocco `root`, che sta alla sua posizione z.</summary>
        public void Populate(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.Destroy(root.GetChild(i).gameObject);

            float z = root.position.z;
            BuildRoad(root);
            for (int side = -1; side <= 1; side += 2)
            {
                BuildSidewalk(root, side);
                BuildBuildings(root, side, z);
            }
        }

        private void BuildRoad(Transform root)
        {
            float len = CityLayout.BlockLength;
            Prim(root, PrimitiveType.Cube, "Asfalto", new Vector3(0f, -0.1f, len * 0.5f),
                 new Vector3(CityLayout.RoadWidth, 0.2f, len), Asphalt,
                 ProceduralTextures.Asphalt, new Vector2(CityLayout.RoadWidth / 4f, len / 4f));

            for (int b = 1; b < CityLayout.LaneCount; b++)
            {
                float x = (b - CityLayout.LaneCount * 0.5f) * CityLayout.LaneWidth;
                for (float zs = 2.5f; zs < len; zs += 5f)
                    Prim(root, PrimitiveType.Cube, "Striscia", new Vector3(x, 0.02f, zs),
                         new Vector3(0.12f, 0.04f, 2.5f), Paint);
            }
        }

        /// <summary>Marciapiede per lotti: ogni lotto ha la sua lastra, una tinta, eventuali toppe e un arredo.</summary>
        private void BuildSidewalk(Transform root, int side)
        {
            float x = side * CityLayout.SidewalkCenterX;
            float innerX = side * StallX;
            float outerX = side * OuterX;
            float lot = CityLayout.LotWidth;

            for (int i = 0; i < CityLayout.LotsPerBlock; i++)
            {
                float z0 = i * lot;
                float zc = z0 + lot * 0.5f;

                Color tint = SidewalkTints[Random.Range(0, SidewalkTints.Length)];
                Prim(root, PrimitiveType.Cube, "Marciapiede", new Vector3(x, CityLayout.CurbHeight * 0.5f, zc),
                     new Vector3(CityLayout.SidewalkWidth, CityLayout.CurbHeight, lot), tint,
                     ProceduralTextures.Sidewalk, new Vector2(CityLayout.SidewalkWidth / 2f, lot / 4f));

                Prim(root, PrimitiveType.Cube, "Giunto", new Vector3(x, CityLayout.CurbHeight + 0.01f, z0),
                     new Vector3(CityLayout.SidewalkWidth, 0.02f, 0.06f), Joint);

                if (Random.value < 0.2f)
                    Prim(root, PrimitiveType.Cube, "Toppa",
                         new Vector3(x + Random.Range(-0.4f, 0.4f), CityLayout.CurbHeight + 0.012f, zc + Random.Range(-3f, 3f)),
                         new Vector3(0.8f, 0.02f, 1.2f), Joint);

                // Lato strada: un solo arredo per lotto (banco, fioriera o bidone).
                float r = Random.value;
                if (r < kit.StallChance * 0.5f)
                    BuildSidewalkStall(root, side, zc);
                else if (r < 0.45f)
                    BuildPlanter(root, innerX, zc);
                else if (r < 0.6f)
                    BuildBin(root, innerX, zc);

                // Lato edifici: albero oppure palo con cartello.
                float zo = zc + Random.Range(-2f, 2f);
                if (kit.Trees != null && kit.Trees.Length > 0 && Random.value < 0.5f)
                {
                    GameObject tree = Object.Instantiate(kit.Trees[Random.Range(0, kit.Trees.Length)], root);
                    tree.name = "Albero";
                    tree.transform.localPosition = new Vector3(outerX, CityLayout.CurbHeight, zo);
                }
                else if (Random.value < 0.4f)
                {
                    BuildSign(root, outerX, zo);
                }
            }
        }

        private void BuildPlanter(Transform root, float x, float z)
        {
            Prim(root, PrimitiveType.Cube, "Fioriera", new Vector3(x, CityLayout.CurbHeight + 0.18f, z),
                 new Vector3(0.9f, 0.36f, 0.5f), PlanterBrick);
            Prim(root, PrimitiveType.Sphere, "Cespuglio", new Vector3(x, CityLayout.CurbHeight + 0.6f, z),
                 Vector3.one * 0.55f, Leaf);
        }

        private void BuildBin(Transform root, float x, float z)
        {
            Prim(root, PrimitiveType.Cylinder, "Bidone", new Vector3(x, CityLayout.CurbHeight + 0.45f, z),
                 new Vector3(0.4f, 0.45f, 0.4f), BinGreen);
        }

        private void BuildSign(Transform root, float x, float z)
        {
            Prim(root, PrimitiveType.Cylinder, "PaloCartello", new Vector3(x, CityLayout.CurbHeight + 1.5f, z),
                 new Vector3(0.12f, 1.5f, 0.12f), Pole);
            Prim(root, PrimitiveType.Cube, "Cartello", new Vector3(x, CityLayout.CurbHeight + 2.6f, z),
                 new Vector3(0.08f, 0.9f, 1.6f), SignColors[Random.Range(0, SignColors.Length)],
                 ProceduralTextures.Sign);
        }

        /// <summary>Un edificio per lotto, scelto per distretto: sempre sull'impronta del lotto, mai deformato.</summary>
        private void BuildBuildings(Transform root, int side, float tileZ)
        {
            float xCenter = side * CityLayout.BuildingCenterX;
            for (int i = 0; i < CityLayout.LotsPerBlock; i++)
            {
                float zc = i * CityLayout.LotWidth + CityLayout.LotWidth * 0.5f;
                int d = Districts.Pick(Districts.Weights(tileZ + zc));
                GameObject prefab = PickBuilding(d);
                if (prefab == null) continue;

                GameObject go = Object.Instantiate(prefab, root);
                go.name = "Edificio";
                go.transform.localPosition = new Vector3(xCenter, 0f, zc);
                go.transform.localRotation = Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f);
                go.transform.localScale = Vector3.one;
            }
        }

        private GameObject PickBuilding(int district)
        {
            if (kit.Buildings == null || kit.Buildings.Length < Districts.Count * 6)
            {
                if (!warnedMissingBuildings)
                {
                    warnedMissingBuildings = true;
                    Debug.LogWarning("[TukTuk] Edifici mancanti: esegui TukTuk > Genera modelli 3D.");
                }
                return null;
            }
            return kit.Buildings[district * 6 + Random.Range(0, 6)];
        }

        private GameObject Prim(Transform parent, PrimitiveType type, string name,
                                Vector3 localPos, Vector3 scale, Color color,
                                Texture2D tex = null, Vector2 tiling = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            ToonUtil.Paint(go.GetComponent<Renderer>(), kit.Toon, color, tex, tiling);
            Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        private void BuildSidewalkStall(Transform parent, int side, float z)
        {
            float x = side * StallX;

            Prim(parent, PrimitiveType.Cube, "BancoFrutta",
                 new Vector3(x, 0.5f, z), new Vector3(1.2f, 1f, 1.8f), Wood,
                 ProceduralTextures.Plank, new Vector2(1f, 1f));

            for (int i = 0; i < 3; i++)
            {
                Prim(parent, PrimitiveType.Sphere, "Frutta",
                     new Vector3(x, 1.15f, z - 0.5f + i * 0.5f), new Vector3(0.3f, 0.3f, 0.3f),
                     FruitColors[i % FruitColors.Length]);
            }

            Prim(parent, PrimitiveType.Cube, "Tendone",
                 new Vector3(x, 2.3f, z), new Vector3(1.9f, 0.12f, 2.1f),
                 AwningColors[Random.Range(0, AwningColors.Length)],
                 ProceduralTextures.Awning, new Vector2(3f, 1f));

            foreach (float dz in new[] { -0.9f, 0.9f })
            {
                Prim(parent, PrimitiveType.Cylinder, "Palo",
                     new Vector3(side * 7.1f, CityLayout.CurbHeight + 1.15f, z + dz), new Vector3(0.06f, 1.15f, 0.06f), Pole);
            }
        }
    }
}
