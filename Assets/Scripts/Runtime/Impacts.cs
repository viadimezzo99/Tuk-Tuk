using UnityEngine;
using static TukTuk.Gameplay.CityPalette;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Effetti d'impatto animati: schegge, scintille e pezzi di carrozzeria che volano via.
    /// I pezzi hanno fisica di caduta ma nessun collider e spariscono dopo poco.
    /// </summary>
    public static class Impacts
    {
        private static readonly Color Spark = new Color(1f, 0.9f, 0.4f);
        private static readonly Color[] Body = { Dome, Warning, Paint, Pole, Tyre };

        /// <summary>
        /// Scintille e pochi pezzi nel punto di contatto. Restano poco sulla strada e non toccano il veicolo.
        /// </summary>
        public static void Burst(Material toon, Transform parent, Vector3 point, Vector3 baseVelocity, int count, bool sparks)
        {
            for (int i = 0; i < count; i++)
            {
                bool isSpark = sparks && i % 2 == 0;
                Color color = isSpark ? Spark : Body[Random.Range(0, Body.Length)];
                PrimitiveType type = Random.value < 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere;
                float size = isSpark ? Random.Range(0.05f, 0.1f) : Random.Range(0.12f, 0.3f);

                GameObject piece = Prims.Create(toon, parent, type, isSpark ? "Scintilla" : "Scheggia",
                                                point + Random.insideUnitSphere * 0.3f,
                                                Vector3.one * size, color);
                piece.transform.rotation = Random.rotation;

                var body = piece.AddComponent<Rigidbody>();
                body.linearVelocity = baseVelocity * 0.4f
                    + new Vector3(Random.Range(-4f, 4f), Random.Range(3f, 8f), Random.Range(-4f, 4f));
                body.angularVelocity = Random.insideUnitSphere * 15f;

                Object.Destroy(piece, isSpark ? 0.6f : 1.5f);
            }
        }
    }
}
