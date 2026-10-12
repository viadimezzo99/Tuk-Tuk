using TukTuk.Art;
using UnityEditor;
using UnityEngine;

namespace TukTuk.EditorTools
{
    /// <summary>
    /// Menu: TukTuk > Genera auto del traffico.
    /// - Auto in Assets/Art/Generated/Cars (entrano nel pool delle auto): taxi bianco, berlina argento,
    ///   crossover argento, tuk-tuk giallo e verde da traffico.
    /// - Carrozza nera con capote rosso e cavallo in Assets/Art/Generated/Stalls (pool dei carretti).
    /// Origine a terra al centro del mezzo, fronte su +Z. Dimensioni entro le hitbox di TrafficCar.HalfSize.
    /// Dopo la generazione: TukTuk > Crea scena prototipo, così il pool delle auto le include.
    /// </summary>
    public static class RoadVehicleGenerator
    {
        private const string Root = "Assets/Art/Generated";

        private static readonly Color White = new Color(0.95f, 0.94f, 0.90f);
        private static readonly Color Silver = new Color(0.70f, 0.76f, 0.82f);
        private static readonly Color Yellow = new Color(0.98f, 0.80f, 0.20f);
        private static readonly Color Gold = new Color(0.90f, 0.65f, 0.12f);
        private static readonly Color Green = new Color(0.18f, 0.64f, 0.42f);
        private static readonly Color Glass = new Color(0.30f, 0.45f, 0.55f);
        private static readonly Color Tyre = new Color(0.12f, 0.12f, 0.12f);
        private static readonly Color Rim = new Color(0.78f, 0.80f, 0.83f);
        private static readonly Color Metal = new Color(0.45f, 0.45f, 0.50f);
        private static readonly Color Dark = new Color(0.16f, 0.15f, 0.18f);
        private static readonly Color Lamp = new Color(1f, 0.95f, 0.70f);
        private static readonly Color Brake = new Color(0.85f, 0.15f, 0.12f);
        private static readonly Color Red = new Color(0.78f, 0.22f, 0.18f);
        private static readonly Color Horse = new Color(0.48f, 0.29f, 0.16f);
        private static readonly Color Mane = new Color(0.18f, 0.11f, 0.07f);

        [MenuItem("TukTuk/Genera auto del traffico")]
        public static void Generate()
        {
            PeopleGenerator.EnsureFolder(Root, "Meshes");
            PeopleGenerator.EnsureFolder(Root, "Cars");
            PeopleGenerator.EnsureFolder(Root, "Stalls");
            BuildSedan("Taxi_Bianco", White, true);
            BuildSedan("Berlina_Argento", Silver, false);
            BuildSuv("Crossover_Argento");
            BuildTuk("TukTuk_Traffico");
            BuildCarriage("Carrozza_Nera");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TukTuk] Generate 4 auto e 1 carrozza in " + Root + ".");
        }

        // ---------- helper ----------

        private static MeshBuilder Box(Vector3 size, float radius = 0.03f)
        {
            return MeshKit.RoundedBox(size, radius, 3);
        }

        /// <summary>Ruota con cerchione: asse lungo X, centrata sulla posizione indicata.</summary>
        private static void Wheel(VehicleGenerator.Rig rig, Vector3 pos, float radius, float width)
        {
            rig.Add("Ruota", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(radius, width, 0.04f, 2), 16), Tyre,
                    pos, new Vector3(0f, 0f, 90f));
            rig.Add("Mozzo", MeshKit.Lathe(MeshKit.RoundedCylinderProfile(radius * 0.6f, width + 0.03f, 0.02f, 1), 12), Rim,
                    pos, new Vector3(0f, 0f, 90f));
        }

        // ---------- berline e taxi (hitbox auto: 1.9 x 2.2 m) ----------

        private static void BuildSedan(string key, Color body, bool taxi)
        {
            var rig = new VehicleGenerator.Rig(key);

            rig.Add("Carrozzeria", Box(new Vector3(1.6f, 0.5f, 2.1f), 0.12f), body, new Vector3(0f, 0.55f, 0f), Vector3.zero);
            rig.Add("Abitacolo", Box(new Vector3(1.36f, 0.5f, 1.15f), 0.12f), body, new Vector3(0f, 1.05f, -0.1f), Vector3.zero);
            rig.Add("Vetri", Box(new Vector3(1.4f, 0.28f, 1.17f), 0.06f), Glass, new Vector3(0f, 1.1f, -0.1f), Vector3.zero);
            rig.Add("Cofano", Box(new Vector3(1.5f, 0.08f, 0.7f), 0.03f), body, new Vector3(0f, 0.86f, 0.7f), new Vector3(-6f, 0f, 0f));

            if (taxi)
            {
                rig.Add("Striscia", Box(new Vector3(1.62f, 0.06f, 2.12f), 0.02f), Yellow, new Vector3(0f, 0.74f, 0f), Vector3.zero);
                rig.Add("Insegna", Box(new Vector3(0.4f, 0.14f, 0.3f), 0.04f), Yellow, new Vector3(0f, 1.36f, -0.1f), Vector3.zero);
            }

            rig.Add("Griglia", Box(new Vector3(0.7f, 0.18f, 0.04f), 0.02f), Dark, new Vector3(0f, 0.55f, 1.07f), Vector3.zero);
            foreach (float x in new[] { -0.5f, 0.5f })
            {
                rig.Add("Faro", Box(new Vector3(0.22f, 0.1f, 0.05f), 0.02f), Lamp, new Vector3(x, 0.66f, 1.06f), Vector3.zero);
                rig.Add("Fanale", Box(new Vector3(0.22f, 0.1f, 0.05f), 0.02f), Brake, new Vector3(x, 0.66f, -1.06f), Vector3.zero);
            }
            foreach (float x in new[] { -0.78f, 0.78f })
                foreach (float z in new[] { -0.72f, 0.72f })
                    Wheel(rig, new Vector3(x, 0.3f, z), 0.3f, 0.2f);

            rig.Save(Root + "/Cars/" + rig.Key + ".prefab");
        }

        // ---------- crossover (hitbox auto) ----------

        private static void BuildSuv(string key)
        {
            var rig = new VehicleGenerator.Rig(key);

            rig.Add("Carrozzeria", Box(new Vector3(1.62f, 0.62f, 2.14f), 0.14f), Silver, new Vector3(0f, 0.6f, 0f), Vector3.zero);
            rig.Add("Abitacolo", Box(new Vector3(1.5f, 0.6f, 1.5f), 0.14f), Silver, new Vector3(0f, 1.2f, -0.1f), Vector3.zero);
            rig.Add("Vetri", Box(new Vector3(1.53f, 0.4f, 1.53f), 0.06f), Glass, new Vector3(0f, 1.25f, -0.1f), Vector3.zero);
            rig.Add("Portapacchi", Box(new Vector3(1.36f, 0.05f, 1.2f), 0.02f), Metal, new Vector3(0f, 1.53f, -0.1f), Vector3.zero);
            rig.Add("Griglia", Box(new Vector3(0.9f, 0.22f, 0.04f), 0.02f), Dark, new Vector3(0f, 0.6f, 1.08f), Vector3.zero);
            foreach (float x in new[] { -0.55f, 0.55f })
            {
                rig.Add("Faro", Box(new Vector3(0.3f, 0.1f, 0.05f), 0.02f), Lamp, new Vector3(x, 0.75f, 1.07f), Vector3.zero);
                rig.Add("Fanale", Box(new Vector3(0.3f, 0.12f, 0.05f), 0.02f), Brake, new Vector3(x, 0.75f, -1.07f), Vector3.zero);
            }
            foreach (float x in new[] { -0.8f, 0.8f })
                foreach (float z in new[] { -0.72f, 0.72f })
                    Wheel(rig, new Vector3(x, 0.36f, z), 0.36f, 0.28f);

            rig.Save(Root + "/Cars/" + rig.Key + ".prefab");
        }

        // ---------- tuk-tuk da traffico (hitbox auto) ----------

        private static void BuildTuk(string key)
        {
            var rig = new VehicleGenerator.Rig(key);

            rig.Add("Cassone", Box(new Vector3(1.3f, 0.85f, 1.25f), 0.1f), Yellow, new Vector3(0f, 0.72f, -0.35f), Vector3.zero);
            rig.Add("Muso", Box(new Vector3(0.95f, 0.6f, 0.7f), 0.12f), Yellow, new Vector3(0f, 0.55f, 0.7f), Vector3.zero);
            rig.Add("Parabrezza", Box(new Vector3(1.1f, 0.5f, 0.05f), 0.02f), Glass, new Vector3(0f, 0.92f, 0.44f), new Vector3(-25f, 0f, 0f));
            rig.Add("Tettuccio", Box(new Vector3(1.4f, 0.07f, 2.0f), 0.03f), Green, new Vector3(0f, 1.5f, -0.25f), Vector3.zero);
            foreach (float x in new[] { -0.6f, 0.6f })
                foreach (float z in new[] { -0.9f, 0.4f })
                    rig.Add("Montante", Box(new Vector3(0.05f, 1.2f, 0.05f), 0.01f), Dark, new Vector3(x, 0.9f, z), Vector3.zero);
            rig.Add("Faro", Box(new Vector3(0.22f, 0.12f, 0.05f), 0.02f), Lamp, new Vector3(0f, 0.7f, 1.07f), Vector3.zero);
            rig.Add("Colonna", Box(new Vector3(0.05f, 0.35f, 0.05f), 0.01f), Metal, new Vector3(0f, 0.85f, 0.55f), new Vector3(-35f, 0f, 0f));
            rig.Add("Manubrio", Box(new Vector3(0.75f, 0.04f, 0.04f), 0.01f), Metal, new Vector3(0f, 1.0f, 0.5f), Vector3.zero);

            Wheel(rig, new Vector3(0f, 0.3f, 0.75f), 0.3f, 0.2f);
            Wheel(rig, new Vector3(-0.66f, 0.3f, -0.8f), 0.3f, 0.22f);
            Wheel(rig, new Vector3(0.66f, 0.3f, -0.8f), 0.3f, 0.22f);

            rig.Save(Root + "/Cars/" + rig.Key + ".prefab");
        }

        // ---------- carrozza con cavallo (pool dei carretti, hitbox carretto) ----------

        private static void BuildCarriage(string key)
        {
            var rig = new VehicleGenerator.Rig(key);

            rig.Add("Cassa", Box(new Vector3(1.2f, 0.5f, 2.0f), 0.08f), Dark, new Vector3(0f, 0.85f, 0f), Vector3.zero);
            rig.Add("Filettatura", Box(new Vector3(1.24f, 0.06f, 2.02f), 0.02f), Gold, new Vector3(0f, 0.62f, 0f), Vector3.zero);
            rig.Add("Capote", Box(new Vector3(1.35f, 0.07f, 2.1f), 0.03f), Red, new Vector3(0f, 1.45f, -0.1f), Vector3.zero);
            foreach (float x in new[] { -0.6f, 0.6f })
                foreach (float z in new[] { -0.9f, 0.8f })
                    rig.Add("Montante", Box(new Vector3(0.05f, 0.85f, 0.05f), 0.01f), Dark, new Vector3(x, 1.02f, z), Vector3.zero);
            foreach (float x in new[] { -0.62f, 0.62f })
                rig.Add("Lanterna", Box(new Vector3(0.12f, 0.2f, 0.12f), 0.02f), Gold, new Vector3(x, 1.0f, 1.0f), Vector3.zero);
            foreach (float x in new[] { -0.3f, 0.3f })
                rig.Add("Stanga", Box(new Vector3(0.05f, 0.05f, 1.0f), 0.01f), Dark, new Vector3(x, 0.75f, 1.5f), Vector3.zero);

            foreach (float x in new[] { -0.68f, 0.68f })
                Wheel(rig, new Vector3(x, 0.7f, -0.15f), 0.7f, 0.12f);

            // Cavallo: corpo, collo, testa, criniera, zampe.
            rig.Add("Cavallo_Corpo", MeshKit.Lathe(MeshKit.EllipsoidProfile(0.42f, 0.4f, 12), 14), Horse,
                    new Vector3(0f, 1.25f, 2.0f), new Vector3(90f, 0f, 0f));
            rig.Add("Cavallo_Collo", Box(new Vector3(0.22f, 0.75f, 0.3f), 0.08f), Horse,
                    new Vector3(0f, 1.7f, 2.4f), new Vector3(30f, 0f, 0f));
            rig.Add("Cavallo_Testa", Box(new Vector3(0.22f, 0.28f, 0.5f), 0.07f), Horse,
                    new Vector3(0f, 2.05f, 2.7f), Vector3.zero);
            rig.Add("Cavallo_Criniera", Box(new Vector3(0.06f, 0.25f, 0.4f), 0.02f), Mane,
                    new Vector3(0f, 1.9f, 2.4f), Vector3.zero);
            foreach (float x in new[] { -0.2f, 0.2f })
                foreach (float z in new[] { 1.75f, 2.25f })
                    rig.Add("Cavallo_Zampa", Box(new Vector3(0.1f, 0.85f, 0.1f), 0.03f), Horse,
                            new Vector3(x, 0.42f, z), Vector3.zero);

            rig.Save(Root + "/Stalls/" + rig.Key + ".prefab");
        }
    }
}
