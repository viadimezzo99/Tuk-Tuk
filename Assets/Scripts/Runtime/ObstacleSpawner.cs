using System.Collections.Generic;
using UnityEngine;
using static TukTuk.Gameplay.CityPalette;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Dossi a cupola sulla carreggiata: rallentamento leggero e dolce, nessun salto.
    /// Le bancarelle fisse sono state tolte (pesavano sul mobile): i carretti con asino
    /// ora sono nel traffico, vedi TrafficSpawner.
    /// </summary>
    public class ObstacleSpawner
    {
        [System.Serializable]
        public class Settings
        {
            public float BumpStepMin = 60f;
            public float BumpStepMax = 120f;
            public float BumpSlowAmount = 2.5f;
            public float BumpSlowTime = 0.35f;
        }

        private const float FirstBumpZ = 80f;

        private class Bump
        {
            public Transform Root;
            public bool Used;
        }

        private readonly Settings settings;
        private readonly Material toon;
        private readonly Transform parent;
        private readonly List<Bump> bumps = new List<Bump>();
        private float nextBumpZ = FirstBumpZ;

        public ObstacleSpawner(Settings settings, Material toon, Transform parent)
        {
            this.settings = settings;
            this.toon = toon;
            this.parent = parent;
        }

        public void Spawn(float playerZ, float ahead)
        {
            while (nextBumpZ < playerZ + ahead)
            {
                SpawnBump(nextBumpZ);
                nextBumpZ += Random.Range(settings.BumpStepMin, settings.BumpStepMax);
            }
        }

        public void Cleanup(float playerZ)
        {
            for (int i = bumps.Count - 1; i >= 0; i--)
            {
                Bump bump = bumps[i];
                if (bump.Root == null || bump.Root.position.z < playerZ - 20f)
                {
                    if (bump.Root != null) Object.Destroy(bump.Root.gameObject);
                    bumps.RemoveAt(i);
                }
            }
        }

        /// <summary>Rallenta una volta sola il tuk-tuk quando passa sopra un dosso.</summary>
        public void CheckBumps(float playerZ, TukTukController player)
        {
            foreach (Bump bump in bumps)
            {
                if (bump.Root == null) continue;
                float dz = bump.Root.position.z - playerZ;
                if (!bump.Used && dz <= 0.5f && dz > -0.5f && player.Height <= 0f)
                {
                    bump.Used = true;
                    player.Slow(settings.BumpSlowAmount, settings.BumpSlowTime);
                }
            }
        }

        /// <summary>Dosso basso e largo: un cilindro schiacciato che attraversa la carreggiata.</summary>
        private void SpawnBump(float z)
        {
            GameObject bump = Prims.Create(toon, parent, PrimitiveType.Cylinder, "Dosso",
                                           new Vector3(0f, 0.02f, z), new Vector3(0.22f, CityLayout.RoadWidth * 0.5f, 0.22f),
                                           Warning, ProceduralTextures.Awning, new Vector2(10f, 1f));
            bump.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            bumps.Add(new Bump { Root = bump.transform });
        }
    }
}
