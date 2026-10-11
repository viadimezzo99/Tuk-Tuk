using UnityEngine;

namespace TukTuk.Gameplay
{
    public enum VehicleKind { Car, Moto, Cart }

    /// <summary>
    /// Veicolo di traffico: auto, moto con rider o carretto con asino.
    /// Avanza lungo +Z nella sua corsia, rispetta il limite di velocità imposto dal traffico
    /// (distanza di sicurezza, semafori) e cambia corsia con una velocità che dipende dal tipo.
    /// Dopo un urto: l'auto viene spinta in avanti, la moto può volare via.
    /// </summary>
    public class TrafficCar : MonoBehaviour
    {
        private const float Accel = 4f;
        private const float HitCooldownTime = 1f;

        private float desired;
        private float current;
        private float cap = float.MaxValue;
        private float pushExtra;
        private float pushTimer;
        private float hitCooldown;
        private float weaveTimer;
        private float laneSpeed;
        private bool knocked;

        public VehicleKind Kind { get; private set; }
        /// <summary>Corsia di destinazione (0 = sinistra ... LaneCount-1 = destra).</summary>
        public int Lane { get; private set; }
        /// <summary>Corsia da cui il veicolo viene (utile per annullare un cambio corsia).</summary>
        public int PreviousLane { get; private set; }
        /// <summary>Velocità reale del veicolo, in m/s.</summary>
        public float CurrentSpeed => current;
        public bool IsKnocked => knocked;
        /// <summary>Un veicolo colpito non può essere colpito di nuovo per un breve periodo.</summary>
        public bool IsHittable => !knocked && hitCooldown <= 0f;
        public bool IsChangingLane => Mathf.Abs(transform.position.x - Prims.LaneX(Lane)) > 0.01f;
        /// <summary>True mentre il tuk-tuk è a contatto con questo veicolo (per contare un urto una sola volta).</summary>
        public bool Touching { get; set; }
        /// <summary>Metà larghezza (x) e metà lunghezza (y) del veicolo, in metri.</summary>
        public Vector2 HalfExtents => HalfSize(Kind);

        public static Vector2 HalfSize(VehicleKind kind)
        {
            switch (kind)
            {
                case VehicleKind.Moto: return new Vector2(0.45f, 0.9f);
                case VehicleKind.Cart: return new Vector2(0.75f, 1.8f);
                default: return new Vector2(0.95f, 1.1f);
            }
        }

        /// <summary>Velocità con cui il veicolo cambia corsia: le auto sono lente, le moto veloci.</summary>
        private static float LaneChangeSpeed(VehicleKind kind)
        {
            switch (kind)
            {
                case VehicleKind.Moto: return 7f;
                case VehicleKind.Cart: return 1.2f;
                default: return 2.5f;
            }
        }

        public void Init(float desiredSpeed, int lane, VehicleKind kind)
        {
            desired = desiredSpeed;
            current = desiredSpeed;
            Lane = lane;
            PreviousLane = lane;
            Kind = kind;
            laneSpeed = LaneChangeSpeed(kind);
            weaveTimer = Random.Range(2f, 5f);
        }

        /// <summary>Imposta la nuova corsia: il veicolo scivola fino al suo centro.</summary>
        public void ChangeLane(int lane)
        {
            PreviousLane = Lane;
            Lane = lane;
        }

        /// <summary>Annulla il cambio corsia: il veicolo torna nella corsia da cui arrivava.</summary>
        public void AbortLaneChange()
        {
            Lane = PreviousLane;
        }

        /// <summary>Riporta il veicolo a una posizione lungo la strada (usato per evitare sovrapposizioni).</summary>
        public void SnapBehind(float z)
        {
            Vector3 p = transform.position;
            p.z = z;
            transform.position = p;
        }

        /// <summary>Limite di velocità per questo frame: il più basso vince.</summary>
        public void Cap(float maxSpeed)
        {
            cap = Mathf.Min(cap, maxSpeed);
        }

        public void ResetCap()
        {
            cap = float.MaxValue;
        }

        /// <summary>Le moto zigzagano ogni tanto: restituisce true quando è il momento di provare a cambiare corsia.</summary>
        public bool WeaveReady(float dt)
        {
            if (Kind != VehicleKind.Moto) return false;
            weaveTimer -= dt;
            if (weaveTimer > 0f) return false;
            weaveTimer = Random.Range(2f, 5f);
            return Random.value < 0.5f;
        }

        /// <summary>Spinta in avanti (tamponamento): aggiunge velocità che poi si smorza.</summary>
        public void Push(float extraSpeed, float duration)
        {
            pushExtra = extraSpeed;
            pushTimer = duration;
        }

        public void MarkHit()
        {
            hitCooldown = HitCooldownTime;
        }

        /// <summary>Il veicolo esce di strada: diventa un corpo fisico che vola via e poi sparisce.</summary>
        public void KnockOut(Vector3 velocity)
        {
            if (knocked) return;
            knocked = true;
            var body = gameObject.AddComponent<Rigidbody>();
            body.linearVelocity = velocity;
            body.angularVelocity = Random.insideUnitSphere * 10f;
            Destroy(gameObject, 3f);
        }

        private void Update()
        {
            if (knocked) return;

            float dt = Time.deltaTime;
            if (hitCooldown > 0f) hitCooldown -= dt;

            float boost = 0f;
            if (pushTimer > 0f)
            {
                pushTimer -= dt;
                boost = pushExtra * Mathf.Clamp01(pushTimer);
            }

            // Accelera e frena dolcemente verso la velocità desiderata (o il limite imposto).
            float target = Mathf.Min(desired + boost, cap);
            current = Mathf.MoveTowards(current, Mathf.Max(0f, target), Accel * dt);

            Vector3 p = transform.position;
            p.x = Mathf.MoveTowards(p.x, Prims.LaneX(Lane), laneSpeed * dt);
            p.z += current * dt;
            transform.position = p;
        }
    }
}
