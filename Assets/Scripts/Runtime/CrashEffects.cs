using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Effetti dell'incidente sul tuk-tuk: fumo che sale dal cofano, poi fuoco.
    /// Sono sistemi di particelle creati a runtime e collegati al tuk-tuk.
    /// </summary>
    public static class CrashEffects
    {
        // Punto del cofano, in coordinate locali del tuk-tuk (metri).
        private static readonly Vector3 Hood = new Vector3(0f, 1.1f, 0.9f);

        public static GameObject StartSmoke(Transform tuk)
        {
            return Emitter(tuk, "Fumo", Hood, new Color(0.25f, 0.25f, 0.27f, 0.85f),
                    startSize: 0.45f, startSpeed: 1.2f, lifetime: 1.6f, rate: 18f,
                    grow: 2.2f, gravity: -0.15f, radius: 0.15f);
        }

        public static GameObject StartFire(Transform tuk)
        {
            return Emitter(tuk, "Fuoco", Hood, new Color(1f, 0.5f, 0.1f),
                    startSize: 0.3f, startSpeed: 2.2f, lifetime: 0.5f, rate: 40f,
                    grow: 0.4f, gravity: -0.6f, radius: 0.12f);
        }

        private static GameObject Emitter(Transform parent, string name, Vector3 localPos, Color color,
                                    float startSize, float startSpeed, float lifetime, float rate,
                                    float grow, float gravity, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = lifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            // Le particelle crescono nel tempo: fumo che si allarga, fuoco che si restringe.
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(1f, grow)));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) renderer.material = new Material(shader);
            renderer.sortingOrder = 5;
            return go;
        }
    }
}
