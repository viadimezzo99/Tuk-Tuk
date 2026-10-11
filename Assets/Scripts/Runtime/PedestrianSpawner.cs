using System.Collections.Generic;
using UnityEngine;
using static TukTuk.Gameplay.CityPalette;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Pedoni: camminano sui marciapiedi nella direzione di marcia; alcuni attraversano sulle strisce
    /// quando il tuk-tuk si avvicina. Investirli li stende nel sangue e rallenta il tuk-tuk.
    /// </summary>
    public class PedestrianSpawner
    {
        [System.Serializable]
        public class Settings
        {
            public float WalkerStepMin = 25f;
            public float WalkerStepMax = 50f;
            [Range(0f, 1f)] public float WalkerChance = 0.7f;
            public float SlowAmount = 5f;
            public float SlowTime = 1.5f;

            public float CrossingStepMin = 90f;
            public float CrossingStepMax = 140f;
            [Tooltip("Distanza dalla striscia alla quale i pedoni iniziano ad attraversare")]
            public float ReleaseDistance = 45f;
            public int WalkersPerCrossing = 2;
        }

        private const float SidewalkX = CityLayout.SidewalkCenterX;
        private const float FirstWalkerZ = 40f;
        private const float FirstCrossingZ = 120f;
        private const float WalkerLength = 250f;
        private static readonly Color SkinColor = new Color(0.85f, 0.6f, 0.45f);
        private static readonly Color PoleColor = new Color(0.25f, 0.25f, 0.28f);
        private static readonly Color LampOff = new Color(0.3f, 0.3f, 0.3f);

        private class Crossing
        {
            public float Z;
            public bool Released;
            public Transform Root;
            public readonly List<Pedestrian> Walkers = new List<Pedestrian>();
        }

        private readonly Settings settings;
        private readonly Material toon;
        private readonly GameObject[] models;
        private readonly TukTukController player;
        private readonly Transform parent;
        private readonly TrafficSignals signals;
        private readonly List<Pedestrian> pedestrians = new List<Pedestrian>();
        private readonly List<Crossing> crossings = new List<Crossing>();
        private float nextWalkerZ = FirstWalkerZ;
        private float nextCrossingZ = FirstCrossingZ;

        public PedestrianSpawner(Settings settings, Material toon, GameObject[] models, TukTukController player,
                                 TrafficSignals signals, Transform parent)
        {
            this.settings = settings;
            this.toon = toon;
            this.models = models;
            this.player = player;
            this.signals = signals;
            this.parent = parent;
        }

        public void Spawn(float playerZ, float ahead)
        {
            while (nextWalkerZ < playerZ + ahead * 0.8f)
            {
                if (Random.value < settings.WalkerChance) SpawnSidewalkWalker(nextWalkerZ);
                nextWalkerZ += Random.Range(settings.WalkerStepMin, settings.WalkerStepMax);
            }

            while (nextCrossingZ < playerZ + ahead)
            {
                SpawnCrossing(nextCrossingZ);
                nextCrossingZ += Random.Range(settings.CrossingStepMin, settings.CrossingStepMax);
            }

            // I pedoni delle strisce partono solo quando il tuk-tuk si avvicina.
            foreach (Crossing crossing in crossings)
            {
                if (crossing.Released || playerZ < crossing.Z - settings.ReleaseDistance) continue;
                crossing.Released = true;
                foreach (Pedestrian walker in crossing.Walkers)
                    if (walker != null) walker.Release();
            }
        }

        public void Cleanup(float playerZ)
        {
            Prims.RemoveBehind(pedestrians, playerZ);

            for (int i = crossings.Count - 1; i >= 0; i--)
            {
                if (crossings[i].Z < playerZ - 20f)
                {
                    if (crossings[i].Root != null) Object.Destroy(crossings[i].Root.gameObject);
                    crossings.RemoveAt(i);
                }
            }
        }

        /// <summary>Investimenti: il pedone resta steso, lascia sangue, rallenta e azzera la corsa pulita.</summary>
        public void CheckCollisions(TukTukController playerRef, RunState run, DecalSpawner decals)
        {
            Vector3 pos = playerRef.transform.position;
            foreach (Pedestrian ped in pedestrians)
            {
                if (ped == null || ped.Hit) continue;
                Vector3 d = ped.transform.position - pos;
                if (Mathf.Abs(d.x) < 0.35f + TukTukController.HalfWidth
                    && Mathf.Abs(d.z) < 0.35f + TukTukController.HalfLength
                    && playerRef.Height < 1.2f)
                {
                    // Investito di piena: vola via di lato, dal lato da cui arriva il tuk-tuk.
                    float side = Mathf.Sign(ped.transform.position.x - pos.x);
                    if (side == 0f) side = 1f;
                    ped.FlyOff(new Vector3(side * 4f, 6f, playerRef.EffectiveSpeed * 0.4f));
                    SoundLibrary.Play(Sfx.Scream);
                    SoundLibrary.Play(Sfx.Thud, 0.6f);
                    run.AddInvested();
                    decals.BloodPool(ped.transform.position);
                    playerRef.Slow(settings.SlowAmount, settings.SlowTime);
                    run.ResetCleanRun();
                }
            }
        }

        /// <summary>Strisce pedonali sull'asfalto, con due pedoni che aspettano sul marciapiede.</summary>
        private void SpawnCrossing(float z)
        {
            var crossing = new Crossing { Z = z };

            var root = new GameObject("Attraversamento").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(0f, 0f, z);
            crossing.Root = root;

            for (int i = 0; i < 10; i++)
            {
                float x = -4.5f + i * 1f;
                Prims.Create(toon, root, PrimitiveType.Cube, "Zebra",
                             new Vector3(x, 0.03f, 0f), new Vector3(0.55f, 0.05f, 3.2f), Paint);
            }

            for (int i = 0; i < settings.WalkersPerCrossing; i++)
            {
                float side = Random.value < 0.5f ? -1f : 1f;
                float offset = Random.Range(-0.8f, 0.8f);
                Vector3[] path =
                {
                    new Vector3(side * SidewalkX, 0f, z - 1.5f),
                    new Vector3(side * SidewalkX, 0f, z + offset),
                    new Vector3(-side * SidewalkX, 0f, z + offset),
                    new Vector3(-side * SidewalkX, 0f, z + offset + 30f),
                };
                crossing.Walkers.Add(MakePedestrian(path, Random.Range(1.3f, 1.7f), true));
            }

            // Semaforo sul marciapiede prima delle strisce: i veicoli si fermano con il rosso.
            float phase = Random.Range(0f, TrafficSignals.CycleLength);
            signals.Add(z, phase);
            BuildLight(root, phase);

            crossings.Add(crossing);
        }

        /// <summary>Palo, scatola e due lampade (rossa e verde) che guardano il traffico in arrivo.</summary>
        private void BuildLight(Transform root, float phase)
        {
            var holder = new GameObject("Semaforo").transform;
            holder.SetParent(root, false);
            holder.localPosition = new Vector3(SidewalkX + 0.6f, 0f, -7f);

            Prims.Create(toon, holder, PrimitiveType.Cylinder, "Palo",
                         new Vector3(0f, 2f, 0f), new Vector3(0.12f, 2f, 0.12f), PoleColor);
            Prims.Create(toon, holder, PrimitiveType.Cube, "Scatola",
                         new Vector3(0f, 3.7f, 0f), new Vector3(0.5f, 1.3f, 0.35f), PoleColor);
            GameObject red = Prims.Create(toon, holder, PrimitiveType.Sphere, "LampRosso",
                         new Vector3(0f, 4.05f, -0.2f), Vector3.one * 0.3f, LampOff);
            GameObject green = Prims.Create(toon, holder, PrimitiveType.Sphere, "LampVerde",
                         new Vector3(0f, 3.35f, -0.2f), Vector3.one * 0.3f, LampOff);

            holder.gameObject.AddComponent<TrafficLightView>()
                .Init(toon, phase, red.GetComponent<Renderer>(), green.GetComponent<Renderer>());
        }

        /// <summary>Pedone che cammina sul marciapiede nella direzione di marcia.</summary>
        private void SpawnSidewalkWalker(float z)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            Vector3[] path =
            {
                new Vector3(side * SidewalkX, 0f, z),
                new Vector3(side * SidewalkX, 0f, z + WalkerLength),
            };
            MakePedestrian(path, Random.Range(1.2f, 1.8f), false);
        }

        private Pedestrian MakePedestrian(Vector3[] path, float walkSpeed, bool startPaused)
        {
            var go = new GameObject("Pedone");
            go.transform.SetParent(parent, false);

            var ped = go.AddComponent<Pedestrian>();
            GameObject model = (models != null && models.Length > 0)
                ? models[Random.Range(0, models.Length)] : null;

            if (model != null)
            {
                Transform body = Object.Instantiate(model, go.transform).transform;
                body.localPosition = Vector3.zero;
                body.localRotation = Quaternion.identity;
                ped.InitModel(body, player.transform);
            }
            else
            {
                ped.Init(toon, player.transform,
                         RandomColor(),
                         SkinColor,
                         Random.value < 0.5f ? new Color(0.1f, 0.1f, 0.12f) : new Color(0.45f, 0.3f, 0.2f),
                         new Color(0.2f, 0.22f, 0.3f));
            }

            ped.SetPath(path, walkSpeed, startPaused);
            pedestrians.Add(ped);
            return ped;
        }

        private static Color RandomColor()
        {
            return Color.HSVToRGB(Random.value, Random.Range(0.5f, 0.9f), Random.Range(0.6f, 0.95f));
        }
    }
}
