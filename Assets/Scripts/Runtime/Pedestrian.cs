using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Pedone low-poly animato.
    /// - Segue un percorso (marciapiede, strisce, marciapiede opposto); può aspettare alle strisce.
    /// - Se il tuk-tuk arriva vicino si spaventa: scappa di lato verso il marciapiede e accelera.
    /// - Se investito cade in avanti e resta steso sull'asfalto (la pozza di sangue la crea la strada).
    /// Il modello cammina lungo +Z locale.
    /// </summary>
    public class Pedestrian : MonoBehaviour
    {
        private const float RoadHalfWidth = 5f;
        private const float MaxX = CityLayout.FacadeX - 0.4f;
        private const float FallTime = 0.3f;
        private const float ScareTime = 2f;
        private const float FleeSideSpeed = 3.5f;
        private const float RunFactor = 1.6f;
        private const float LyingHeight = 0.35f;

        private float speed;
        private float phase;
        private Transform legLeft;
        private Transform legRight;
        private Transform armLeft;
        private Transform armRight;
        private const float ModelScale = 0.9f;

        private Transform player;
        private float scareTimer;
        private bool wasScared;

        private Vector3[] path;
        private int target;
        private bool paused;

        private bool hit;
        private float hitTime;
        private Quaternion hitStartRot;

        public bool Hit => hit;

        public void Init(Material mat, Transform playerTransform, Color shirt, Color skin, Color hair, Color pants)
        {
            player = playerTransform;
            // Persone in scala rispetto al tuk-tuk (circa 1,65 m reali).
            transform.localScale = Vector3.one * 0.8f;

            AddPart(mat, PrimitiveType.Capsule, "Corpo", new Vector3(0f, 1.15f, 0f),
                    new Vector3(0.6f, 0.45f, 0.6f), shirt);
            AddPart(mat, PrimitiveType.Sphere, "Testa", new Vector3(0f, 1.9f, 0f),
                    new Vector3(0.4f, 0.4f, 0.4f), skin);
            AddPart(mat, PrimitiveType.Sphere, "Capelli", new Vector3(0f, 2.02f, 0f),
                    new Vector3(0.42f, 0.22f, 0.42f), hair);

            legLeft = AddLeg(mat, new Vector3(-0.15f, 0.8f, 0f), pants);
            legRight = AddLeg(mat, new Vector3(0.15f, 0.8f, 0f), pants);
        }

        /// <summary>Usa un modello 3D (prefab) con pivot Hip_L/Hip_R e Shoulder_L/Shoulder_R.</summary>
        public void InitModel(Transform model, Transform playerTransform)
        {
            player = playerTransform;
            transform.localScale = Vector3.one * ModelScale;
            legLeft = model.Find("Hip_L");
            legRight = model.Find("Hip_R");
            armLeft = model.Find("Shoulder_L");
            armRight = model.Find("Shoulder_R");
        }

        /// <summary>Imposta il percorso. Se startPaused, il pedone aspetta Release().</summary>
        public void SetPath(Vector3[] points, float walkSpeed, bool startPaused)
        {
            path = points;
            speed = walkSpeed;
            paused = startPaused;
            target = 1;
            transform.position = new Vector3(points[0].x, 0f, points[0].z);
            if (points.Length > 1) FaceTowards(points[1], float.MaxValue);
        }

        public void Release()
        {
            paused = false;
        }

        /// <summary>
        /// Investimento forte: il pedone vola via di lato e in alto, come un corpo fisico,
        /// poi sparisce. Usato quando il tuk-tuk lo prende in pieno.
        /// </summary>
        public void FlyOff(Vector3 velocity)
        {
            if (hit) return;
            hit = true;
            enabled = false;
            var body = gameObject.AddComponent<Rigidbody>();
            body.linearVelocity = velocity;
            body.angularVelocity = Random.insideUnitSphere * 8f;
            Destroy(gameObject, 3f);
        }

        /// <summary>Investimento: il pedone cade in avanti e resta a terra.</summary>
        public void Knockdown()
        {
            if (hit) return;
            hit = true;
            hitTime = 0f;
            hitStartRot = transform.rotation;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (hit)
            {
                UpdateHit(dt);
                return;
            }

            if (path == null) return;

            UpdateScare(dt);

            if (scareTimer > 0f)
            {
                Flee(dt);
                wasScared = true;
                return;
            }

            if (wasScared)
            {
                wasScared = false;
                Repath();
            }

            if (paused)
            {
                Swing(0f);
                return;
            }

            if (target >= path.Length)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 goal = new Vector3(path[target].x, 0f, path[target].z);
            Vector3 flat = goal - transform.position;
            float dist = flat.magnitude;
            float step = speed * dt;

            FaceTowards(goal, 540f * dt);

            if (dist <= step)
            {
                transform.position = goal;
                target++;
            }
            else
            {
                transform.position += flat / dist * step;
            }

            phase += dt * speed * 5f;
            Swing(Mathf.Sin(phase) * 30f);
        }

        /// <summary>Il tuk-tuk è davanti e vicino, e il pedone è sulla sua traiettoria o accanto: scappa.</summary>
        private void UpdateScare(float dt)
        {
            if (player != null)
            {
                Vector3 d = transform.position - player.position;
                if (d.z > -2f && d.z < 14f && Mathf.Abs(d.x) < 4.5f)
                    scareTimer = ScareTime;
            }
            if (scareTimer > 0f) scareTimer -= dt;
        }

        private void Flee(float dt)
        {
            float side = player != null ? transform.position.x - player.position.x : transform.position.x;
            float dir = Mathf.Sign(side);
            if (dir == 0f) dir = 1f;

            Vector3 pos = transform.position;
            float x = pos.x + dir * FleeSideSpeed * dt;
            pos.x = Mathf.Clamp(x, -MaxX, MaxX);
            pos.z += speed * RunFactor * dt;
            transform.position = pos;

            Vector3 move = new Vector3(dir * FleeSideSpeed * 0.4f, 0f, speed * RunFactor);
            transform.rotation = Quaternion.LookRotation(move);

            phase += dt * speed * RunFactor * 7f;
            Swing(Mathf.Sin(phase) * 40f);
        }

        /// <summary>Dopo la fuga, un pedone sul marciapiede torna a camminare in avanti.</summary>
        private void Repath()
        {
            if (Mathf.Abs(transform.position.x) > RoadHalfWidth)
            {
                Vector3 p = transform.position;
                path = new[] { p, new Vector3(p.x, 0f, p.z + 250f) };
                target = 1;
                paused = false;
            }
        }

        private void UpdateHit(float dt)
        {
            hitTime += dt;

            float fall = Mathf.Clamp01(hitTime / FallTime);
            transform.rotation = hitStartRot * Quaternion.Euler(90f * fall, 0f, 0f);
            Vector3 p = transform.position;
            p.y = LyingHeight * fall;
            transform.position = p;

            Swing(0f);
        }

        private void FaceTowards(Vector3 goal, float maxDegrees)
        {
            Vector3 flat = goal - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;
            Quaternion want = Quaternion.LookRotation(flat);
            transform.rotation = maxDegrees >= float.MaxValue
                ? want
                : Quaternion.RotateTowards(transform.rotation, want, maxDegrees);
        }

        private void Swing(float angle)
        {
            if (legLeft != null) legLeft.localRotation = Quaternion.Euler(angle, 0f, 0f);
            if (legRight != null) legRight.localRotation = Quaternion.Euler(-angle, 0f, 0f);
            if (armLeft != null) armLeft.localRotation = Quaternion.Euler(-angle * 0.6f, 0f, 0f);
            if (armRight != null) armRight.localRotation = Quaternion.Euler(angle * 0.6f, 0f, 0f);
        }

        private Transform AddLeg(Material mat, Vector3 hip, Color pants)
        {
            var pivot = new GameObject("Gamba").transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = hip;
            AddPart(pivot, mat, PrimitiveType.Cylinder, new Vector3(0f, -0.4f, 0f),
                    new Vector3(0.18f, 0.4f, 0.18f), pants, "Coscia");
            return pivot;
        }

        private void AddPart(Material mat, PrimitiveType type, string name,
                             Vector3 localPos, Vector3 scale, Color color)
        {
            AddPart(transform, mat, type, localPos, scale, color, name);
        }

        private static void AddPart(Transform parent, Material mat, PrimitiveType type,
                                    Vector3 localPos, Vector3 scale, Color color, string name = "Parte")
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            ToonUtil.Paint(go.GetComponent<Renderer>(), mat, color);
            Destroy(go.GetComponent<Collider>());
        }
    }
}
