using System.Collections.Generic;
using UnityEngine;
using static TukTuk.Gameplay.CityPalette;

namespace TukTuk.Gameplay
{
    /// <summary>Segni sull'asfalto: pozze di sangue e tracce di pneumatici. Non hanno collider.</summary>
    public class DecalSpawner
    {
        private const float SkidInterval = 0.12f;

        private readonly Material toon;
        private readonly Transform parent;
        private readonly List<Transform> decals = new List<Transform>();
        private float skidTimer;

        public DecalSpawner(Material toon, Transform parent)
        {
            this.toon = toon;
            this.parent = parent;
        }

        /// <summary>Pozza di sangue sotto un pedone investito.</summary>
        public void BloodPool(Vector3 p)
        {
            GameObject main = Prims.Create(toon, parent, PrimitiveType.Cylinder, "Sangue",
                                           new Vector3(p.x, 0.03f, p.z), new Vector3(0.9f, 0.01f, 0.6f), Blood);
            main.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
            decals.Add(main.transform);

            for (int i = 0; i < 3; i++)
            {
                Vector3 off = new Vector3(Random.Range(-0.9f, 0.9f), 0.035f, Random.Range(-1.2f, 1.2f));
                GameObject spot = Prims.Create(toon, parent, PrimitiveType.Cylinder, "Schizzo",
                                               p + off, Vector3.one * Random.Range(0.15f, 0.3f), Blood);
                spot.transform.localScale = new Vector3(spot.transform.localScale.x, 0.01f, spot.transform.localScale.z);
                decals.Add(spot.transform);
            }
        }

        /// <summary>Tracce di pneumatici quando il tuk-tuk sbanda (a intervalli brevi).</summary>
        public void SkidMarks(TukTukController player, float playerZ, float dt)
        {
            skidTimer -= dt;
            if (skidTimer > 0f) return;
            skidTimer = SkidInterval;

            foreach (float dx in new[] { -0.8f, 0.8f })
            {
                GameObject mark = Prims.Create(toon, parent, PrimitiveType.Cube, "Traccia",
                                               new Vector3(player.transform.position.x + dx, 0.02f, playerZ - 1.2f),
                                               new Vector3(0.22f, 0.01f, 0.5f), Tyre);
                decals.Add(mark.transform);
            }
        }

        public void Cleanup(float playerZ)
        {
            Prims.RemoveBehind(decals, playerZ);
        }
    }
}
