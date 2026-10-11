using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Modello cartoon del tuk-tuk, costruito con primitive a dimensioni reali (metri).
    /// Livrea gialla e verde come nei riferimenti del Cairo: cabina posteriore gialla,
    /// muso verde, tettuccio rosso con frange gialle, bandierine, fanali e ruote.
    /// Il muso guarda verso +Z. Larghezza ~1.3 m, lunghezza ~1.9 m, altezza ~1.3 m.
    /// </summary>
    public static class TukTukModel
    {
        public static readonly Color Yellow = new Color(1f, 0.80f, 0.10f);
        public static readonly Color Green = new Color(0.15f, 0.65f, 0.35f);
        public static readonly Color Roof = new Color(0.80f, 0.15f, 0.12f);
        public static readonly Color Dark = new Color(0.12f, 0.12f, 0.12f);
        public static readonly Color Wood = new Color(0.45f, 0.26f, 0.14f);
        public static readonly Color Chrome = new Color(0.75f, 0.78f, 0.82f);
        public static readonly Color Lamp = new Color(1f, 0.96f, 0.60f);
        public static readonly Color Tail = new Color(0.95f, 0.15f, 0.12f);
        public static readonly Color Skin = new Color(0.85f, 0.60f, 0.45f);
        public static readonly Color Shirt = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color Robe = new Color(0.20f, 0.45f, 0.85f);

        /// <summary>Crea il tuk-tuk come figlio di root. Non aggiunge collider.</summary>
        public static Transform Build(Transform root, Material toon)
        {
            var model = new GameObject("Modello").transform;
            model.SetParent(root, false);

            // Cabina posteriore gialla con striscia scura.
            Part(model, toon, PrimitiveType.Cube, "Cabina", new Vector3(0f, 0.6f, -0.3f), new Vector3(1.15f, 0.6f, 1.05f), Yellow);
            Part(model, toon, PrimitiveType.Cube, "StrisciaCabina", new Vector3(0f, 0.42f, -0.3f), new Vector3(1.18f, 0.08f, 1.08f), Dark);

            // Muso verde con faro centrale.
            Part(model, toon, PrimitiveType.Cube, "Muso", new Vector3(0f, 0.42f, 0.78f), new Vector3(0.95f, 0.5f, 0.6f), Green);
            Part(model, toon, PrimitiveType.Sphere, "Parabrezza", new Vector3(0f, 0.7f, 0.5f), new Vector3(0.7f, 0.25f, 0.5f), Chrome);

            // Fanali anteriori e posteriori.
            Part(model, toon, PrimitiveType.Sphere, "FanaleSx", new Vector3(-0.32f, 0.5f, 1.09f), Vector3.one * 0.16f, Lamp);
            Part(model, toon, PrimitiveType.Sphere, "FanaleDx", new Vector3(0.32f, 0.5f, 1.09f), Vector3.one * 0.16f, Lamp);
            Part(model, toon, PrimitiveType.Cube, "StopSx", new Vector3(-0.5f, 0.55f, -0.84f), new Vector3(0.14f, 0.12f, 0.04f), Tail);
            Part(model, toon, PrimitiveType.Cube, "StopDx", new Vector3(0.5f, 0.55f, -0.84f), new Vector3(0.14f, 0.12f, 0.04f), Tail);

            // Tettuccio rosso con frange gialle sui bordi.
            Part(model, toon, PrimitiveType.Cube, "Tettuccio", new Vector3(0f, 1.2f, -0.2f), new Vector3(1.35f, 0.08f, 1.35f), Roof);
            for (int i = 0; i < 6; i++)
            {
                float x = -0.6f + i * 0.24f;
                Part(model, toon, PrimitiveType.Cube, "Frangia", new Vector3(x, 1.12f, 0.49f), new Vector3(0.1f, 0.12f, 0.03f), Yellow);
                Part(model, toon, PrimitiveType.Cube, "FrangiaRetro", new Vector3(x, 1.12f, -0.89f), new Vector3(0.1f, 0.12f, 0.03f), Yellow);
            }

            // Pali del tettuccio.
            foreach (float px in new[] { -0.6f, 0.6f })
            {
                foreach (float pz in new[] { -0.82f, 0.42f })
                {
                    Part(model, toon, PrimitiveType.Cylinder, "Palo", new Vector3(px, 0.8f, pz), new Vector3(0.05f, 0.4f, 0.05f), Dark);
                }
            }

            // Bandierine colorate sul frontale del tettuccio.
            Part(model, toon, PrimitiveType.Cube, "BandieraRossa", new Vector3(-0.3f, 1.0f, 0.5f), new Vector3(0.18f, 0.12f, 0.02f), Roof);
            Part(model, toon, PrimitiveType.Cube, "BandieraVerde", new Vector3(0f, 1.0f, 0.5f), new Vector3(0.18f, 0.12f, 0.02f), Green);
            Part(model, toon, PrimitiveType.Cube, "BandieraGialla", new Vector3(0.3f, 1.0f, 0.5f), new Vector3(0.18f, 0.12f, 0.02f), Yellow);

            // Panca posteriore e passeggeri.
            Part(model, toon, PrimitiveType.Cube, "Panca", new Vector3(0f, 0.42f, -0.55f), new Vector3(1.0f, 0.18f, 0.45f), Wood);
            Part(model, toon, PrimitiveType.Capsule, "PasseggeroSx", new Vector3(-0.25f, 0.85f, -0.55f), new Vector3(0.3f, 0.25f, 0.3f), Robe);
            Part(model, toon, PrimitiveType.Sphere, "TestaSx", new Vector3(-0.25f, 1.2f, -0.55f), Vector3.one * 0.26f, Skin);
            Part(model, toon, PrimitiveType.Capsule, "PasseggeroDx", new Vector3(0.25f, 0.85f, -0.55f), new Vector3(0.3f, 0.25f, 0.3f), Shirt);
            Part(model, toon, PrimitiveType.Sphere, "TestaDx", new Vector3(0.25f, 1.2f, -0.55f), Vector3.one * 0.26f, Skin);

            // Conducente sul sedile anteriore, manubrio.
            Part(model, toon, PrimitiveType.Capsule, "Conducente", new Vector3(0f, 0.85f, 0.05f), new Vector3(0.3f, 0.25f, 0.3f), Shirt);
            Part(model, toon, PrimitiveType.Sphere, "TestaConducente", new Vector3(0f, 1.2f, 0.05f), Vector3.one * 0.26f, Skin);
            var bar = Part(model, toon, PrimitiveType.Cylinder, "Manubrio", new Vector3(0f, 0.75f, 0.35f), new Vector3(0.03f, 0.2f, 0.03f), Dark);
            bar.localRotation = Quaternion.Euler(70f, 0f, 0f);

            // Ruote: anteriore singola, due posteriori.
            Wheel(model, toon, "RuotaAnteriore", new Vector3(0f, 0.22f, 0.9f));
            Wheel(model, toon, "RuotaSx", new Vector3(-0.6f, 0.22f, -0.7f));
            Wheel(model, toon, "RuotaDx", new Vector3(0.6f, 0.22f, -0.7f));

            return model;
        }

        private static Transform Part(Transform parent, Material mat, PrimitiveType type, string name,
                                      Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            ToonUtil.Paint(go.GetComponent<Renderer>(), mat, color);
            Object.Destroy(go.GetComponent<Collider>());
            return go.transform;
        }

        private static void Wheel(Transform parent, Material mat, string name, Vector3 localPos)
        {
            var wheel = Part(parent, mat, PrimitiveType.Cylinder, name, localPos, new Vector3(0.44f, 0.06f, 0.44f), Dark);
            wheel.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Part(parent, mat, PrimitiveType.Cylinder, name + "Mozzo", localPos, new Vector3(0.2f, 0.07f, 0.2f), Chrome)
                .localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static Transform Part(Transform parent, Material mat, PrimitiveType type, string name,
                                      Vector3 localPos, Color color)
        {
            return Part(parent, mat, type, name, localPos, Vector3.one * 0.3f, color);
        }
    }
}
