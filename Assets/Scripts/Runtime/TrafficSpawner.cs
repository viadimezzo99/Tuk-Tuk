using System.Collections.Generic;
using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Traffico su strada: auto, moto con rider e carretti con asino (lenti, con cavaliere).
    /// Regole di guida e di collisione:
    /// - distanza di sicurezza: nessun veicolo supera quello che ha davanti nella stessa corsia;
    /// - nessuna compenetrazione tra veicoli: chi si sovrappone viene riportato dietro (ResolveOverlaps);
    /// - nessuna compenetrazione col tuk-tuk: un carretto o una moto davanti lo bloccano (Solidify);
    ///   un veicolo che arriva da dietro si ferma contro il tuk-tuk;
    /// - semafori: i veicoli si fermano alla linea d'arresto quando il semaforo degli attraversamenti è rosso;
    /// - dopo un incidente i veicoli che arrivano da dietro frenano prima del tuk-tuk fermo;
    /// - le moto zigzagano tra le corsie; le auto cambiano corsia più lentamente.
    /// Collisioni: auto = incidente; carretto e moto = il tuk-tuk rallenta e prende fuoco (la moto vola via).
    /// </summary>
    public class TrafficSpawner
    {
        [System.Serializable]
        public class Settings
        {
            public float StepMin = 14f;
            public float StepMax = 26f;
            public float SpeedMin = 7f;
            public float SpeedMax = 11f;
            [Tooltip("Probabilità che il veicolo sia una moto con rider")]
            [Range(0f, 1f)] public float MotorbikeChance = 0.35f;
            [Tooltip("Probabilità che il veicolo sia un carretto con asino")]
            [Range(0f, 1f)] public float CartChance = 0.2f;
            [Tooltip("Distanza di sicurezza tra due veicoli nella stessa corsia (metri)")]
            public float SafeGap = 5f;
        }

        private const float FirstVehicleZ = 18f;
        private const float BrakeRange = 25f;
        private const float FastBehindRange = 25f;
        private const float LeaderRange = 12f;
        private const float LaneClearance = 7f;
        /// <summary>Distanza entro la quale i veicoli iniziano a fermarsi al semaforo rosso.</summary>
        private const float SignalRange = 40f;
        /// <summary>Decelerazione dolce con cui i veicoli si fermano alla linea d'arresto (m/s²).</summary>
        private const float StopDecel = 2.5f;
        /// <summary>Margine extra sulla hitbox del tuk-tuk in z: impedisce di attraversare un veicolo tra due fotogrammi.</summary>
        private const float ContactMargin = 0.4f;
        /// <summary>Spazio minimo tra le hitbox di due veicoli (metri).</summary>
        private const float OverlapGap = 0.05f;

        private readonly Settings settings;
        private readonly GameObject[] carPrefabs;
        private readonly GameObject[] motorbikePrefabs;
        private readonly GameObject[] cartPrefabs;
        private readonly Transform parent;
        private readonly TrafficSignals signals;
        private readonly List<TrafficCar> cars = new List<TrafficCar>();
        private float nextVehicleZ = FirstVehicleZ;

        public TrafficSpawner(Settings settings, GameObject[] carPrefabs, GameObject[] motorbikePrefabs,
                              GameObject[] cartPrefabs, TrafficSignals signals, Transform parent)
        {
            this.settings = settings;
            this.carPrefabs = carPrefabs;
            this.motorbikePrefabs = motorbikePrefabs;
            this.cartPrefabs = cartPrefabs;
            this.signals = signals;
            this.parent = parent;
        }

        public void Spawn(float playerZ, float ahead)
        {
            if (!HasAnyPrefab()) return;
            // Mai veicoli alle spalle del tuk-tuk, anche se parte più avanti della prima posizione prevista.
            nextVehicleZ = Mathf.Max(nextVehicleZ, playerZ + 15f);

            while (nextVehicleZ < playerZ + ahead)
            {
                SpawnVehicle(nextVehicleZ);
                nextVehicleZ += Random.Range(settings.StepMin, settings.StepMax);
            }
        }

        /// <summary>Guida di tutti i veicoli: distanze di sicurezza, semafori, cambi corsia, sovrapposizioni.</summary>
        public void Steer(float dt, TukTukController player)
        {
            foreach (TrafficCar car in cars)
                if (car != null) car.ResetCap();

            if (player != null && !player.IsAlive) BrakeBehind(player);

            // Distanza di sicurezza: non superare chi sta davanti nella stessa corsia.
            foreach (TrafficCar car in cars)
            {
                if (car == null || car.IsKnocked) continue;
                TrafficCar leader = Leader(car);
                if (leader == null) continue;
                float gap = LeaderGap(car, leader);
                car.Cap(Mathf.Max(0f, leader.CurrentSpeed + (gap - settings.SafeGap) * 0.8f));
            }

            // Semafori: con il rosso ci si ferma alla linea d'arresto, frenando dolcemente.
            if (signals != null)
            {
                float now = Time.time;
                foreach (TrafficCar car in cars)
                {
                    if (car == null || car.IsKnocked) continue;
                    float z = car.transform.position.z;
                    if (signals.NearestRedStop(z, SignalRange, now, out float stopZ))
                        car.Cap(Mathf.Sqrt(2f * StopDecel * Mathf.Max(0f, stopZ - z)));
                }
            }

            // Le moto zigzagano ogni tanto tra le corsie.
            foreach (TrafficCar car in cars)
            {
                if (car == null || car.IsKnocked || car.IsChangingLane) continue;
                if (car.WeaveReady(dt)) TryChangeLane(car, car.transform.position.z);
            }

            ResolveOverlaps();
        }

        /// <summary>
        /// Blocchi fisici col tuk-tuk. Un veicolo davanti che lo tocca lo ferma (il tuk-tuk non lo supera
        /// e prende la sua velocità); un veicolo che arriva da dietro si ferma contro il tuk-tuk.
        /// Va richiamato dopo che il tuk-tuk si è mosso nel fotogramma (LateUpdate).
        /// </summary>
        public void Solidify(TukTukController player)
        {
            if (player == null) return;
            Vector3 pos = player.transform.position;
            foreach (TrafficCar car in cars)
            {
                if (car == null || car.IsKnocked) continue;
                if (player.Height >= HeightLimit(car) || !InContact(car, pos)) continue;

                Vector3 cp = car.transform.position;
                float clearance = car.HalfExtents.y + TukTukController.HalfLength + ContactMargin;
                if (cp.z > pos.z)
                    player.Block(cp.z - clearance, car.CurrentSpeed);
                else
                    car.SnapBehind(pos.z - clearance);
            }
        }

        /// <summary>
        /// Dopo un incidente il tuk-tuk è fermo: i veicoli che arrivano da dietro nella sua corsia
        /// frenano e si fermano prima di toccarlo, oppure scartano sulla corsia vicina.
        /// </summary>
        private void BrakeBehind(TukTukController player)
        {
            Vector3 p = player.transform.position;
            foreach (TrafficCar car in cars)
            {
                if (car == null || car.IsKnocked || car.IsChangingLane) continue;
                Vector3 c = car.transform.position;
                float dz = p.z - c.z;
                if (dz <= 0f || dz > BrakeRange) continue;
                if (Mathf.Abs(c.x - p.x) > TukTukController.HalfWidth + car.HalfExtents.x) continue;

                float gap = dz - TukTukController.HalfLength - car.HalfExtents.y;
                if (gap < 2f) TryChangeLane(car, c.z);
                car.Cap(Mathf.Max(0f, gap * 0.6f));
            }
        }

        /// <summary>
        /// Non-compenetrazione tra veicoli: se due hitbox si sovrappongono, quello dietro viene riportato
        /// alla distanza minima e non può superare quello davanti. Se stava cambiando corsia, il cambio si annulla.
        /// </summary>
        private void ResolveOverlaps()
        {
            for (int i = 0; i < cars.Count; i++)
            {
                TrafficCar a = cars[i];
                if (a == null || a.IsKnocked) continue;
                for (int j = i + 1; j < cars.Count; j++)
                {
                    TrafficCar b = cars[j];
                    if (b == null || b.IsKnocked) continue;

                    TrafficCar rear = a;
                    TrafficCar front = b;
                    if (b.transform.position.z < a.transform.position.z)
                    {
                        rear = b;
                        front = a;
                    }

                    Vector3 rp = rear.transform.position;
                    Vector3 fp = front.transform.position;
                    float needX = rear.HalfExtents.x + front.HalfExtents.x;
                    float needZ = rear.HalfExtents.y + front.HalfExtents.y + OverlapGap;
                    if (Mathf.Abs(rp.x - fp.x) >= needX || fp.z - rp.z >= needZ) continue;

                    if (rear.IsChangingLane) rear.AbortLaneChange();
                    rear.SnapBehind(fp.z - needZ);
                    rear.Cap(front.CurrentSpeed);
                }
            }
        }

        public void Cleanup(float playerZ)
        {
            Prims.RemoveBehind(cars, playerZ);
        }

        /// <summary>
        /// Restituisce il veicolo che ha appena toccato il tuk-tuk (un solo urto per contatto), altrimenti null.
        /// Ogni veicolo tiene traccia del contatto: restare a contatto non conta come nuovi urti.
        /// </summary>
        public TrafficCar CheckCrash(TukTukController player)
        {
            Vector3 pos = player.transform.position;
            TrafficCar first = null;
            foreach (TrafficCar car in cars)
            {
                if (car == null) continue;
                bool touching = !car.IsKnocked && player.Height < HeightLimit(car) && InContact(car, pos);
                bool wasTouching = car.Touching;
                car.Touching = touching;
                if (touching && !wasTouching && first == null && car.IsHittable) first = car;
            }
            return first;
        }

        /// <summary>Contatto: solo scintille nel punto di urto. L'auto viene spinta in avanti.</summary>
        public void Collide(TrafficCar car, TukTukController player, Material toon)
        {
            Vector3 contact = (car.transform.position + player.transform.position) * 0.5f + Vector3.up * 0.6f;
            Impacts.Burst(toon, parent, contact, Vector3.zero, 10, true);
            if (car.Kind == VehicleKind.Car) car.Push(4f, 1.2f);
        }

        /// <summary>La moto colpita esce di strada e vola via, dal lato opposto al tuk-tuk.</summary>
        public void Launch(TrafficCar moto, TukTukController player)
        {
            cars.Remove(moto);
            float side = moto.transform.position.x < player.transform.position.x ? -1f : 1f;
            moto.KnockOut(new Vector3(side * 5f, 7f, player.EffectiveSpeed * 0.5f));
        }

        /// <summary>
        /// Il tuk-tuk tocca il veicolo se le hitbox si sovrappongono (con margini) e il tuk-tuk è a terra
        /// abbastanza da non saltarlo.
        /// </summary>
        private static bool InContact(TrafficCar car, Vector3 pos)
        {
            Vector2 h = car.HalfExtents;
            Vector3 d = car.transform.position - pos;
            return Mathf.Abs(d.x) < h.x + TukTukController.HalfWidth + 0.2f
                && Mathf.Abs(d.z) < h.y + TukTukController.HalfLength + ContactMargin;
        }

        /// <summary>Altezza sotto la quale il tuk-tuk viene colpito: le auto sono più alte dei carretti.</summary>
        private static float HeightLimit(TrafficCar car)
        {
            return car.Kind == VehicleKind.Car ? 1.6f : 1.2f;
        }

        private bool HasAnyPrefab()
        {
            return HasPool(carPrefabs) || HasPool(motorbikePrefabs) || HasPool(cartPrefabs);
        }

        private static bool HasPool(GameObject[] pool)
        {
            return pool != null && pool.Length > 0;
        }

        private TrafficCar Leader(TrafficCar car)
        {
            float z = car.transform.position.z;
            TrafficCar best = null;
            float bestDist = LeaderRange;
            foreach (TrafficCar other in cars)
            {
                if (other == null || other == car || other.IsKnocked || other.Lane != car.Lane) continue;
                float dz = other.transform.position.z - z;
                if (dz > 0f && dz < bestDist)
                {
                    best = other;
                    bestDist = dz;
                }
            }
            return best;
        }

        private static float LeaderGap(TrafficCar car, TrafficCar leader)
        {
            return leader.transform.position.z - car.transform.position.z
                - (leader.HalfExtents.y + car.HalfExtents.y);
        }

        /// <summary>Cerca la corsia libera vicina (a sinistra o a destra) e ci scivola.</summary>
        private bool TryChangeLane(TrafficCar car, float z)
        {
            foreach (int lane in new[] { car.Lane - 1, car.Lane + 1 })
            {
                if (lane < 0 || lane >= TukTukController.LaneCount) continue;
                if (LaneOccupied(lane, z, car) || FastBehind(lane, z, car)) continue;
                car.ChangeLane(lane);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Vero se un altro veicolo è troppo vicino in quella corsia: la distanza di sicurezza
        /// tiene conto della lunghezza di entrambi, così una moto non richiede lo stesso spazio di un carretto.
        /// </summary>
        private bool LaneOccupied(int lane, float z, TrafficCar self)
        {
            float selfHalf = self != null ? self.HalfExtents.y : 1.5f;
            foreach (TrafficCar other in cars)
            {
                if (other == null || other == self || other.IsKnocked || other.Lane != lane) continue;
                float need = selfHalf + other.HalfExtents.y + settings.SafeGap;
                if (Mathf.Abs(other.transform.position.z - z) < need) return true;
            }
            return false;
        }

        /// <summary>Un veicolo più veloce arriva da dietro nella corsia: non è il momento di scartarci dentro.</summary>
        private bool FastBehind(int lane, float z, TrafficCar self)
        {
            foreach (TrafficCar other in cars)
            {
                if (other == null || other == self || other.IsKnocked || other.Lane != lane) continue;
                float dz = z - other.transform.position.z;
                if (dz > 0f && dz < FastBehindRange && other.CurrentSpeed > self.CurrentSpeed) return true;
            }
            return false;
        }

        private void SpawnVehicle(float z)
        {
            VehicleKind kind = PickKind();
            GameObject[] pool = PoolOf(kind);

            int lane = FreeLane(z);
            if (lane < 0) return;

            GameObject prefab = pool[Random.Range(0, pool.Length)];
            GameObject vehicle = Object.Instantiate(prefab, parent);
            vehicle.name = kind.ToString();
            vehicle.transform.position = new Vector3(Prims.LaneX(lane), 0f, z);
            vehicle.transform.rotation = Quaternion.identity;

            var mover = vehicle.AddComponent<TrafficCar>();
            mover.Init(SpeedFor(kind), lane, kind);
            cars.Add(mover);
        }

        private VehicleKind PickKind()
        {
            bool hasCart = HasPool(cartPrefabs);
            bool hasBike = HasPool(motorbikePrefabs);
            bool hasCar = HasPool(carPrefabs);

            float r = Random.value;
            if (hasCart && r < settings.CartChance) return VehicleKind.Cart;
            if (hasBike && r < settings.CartChance + settings.MotorbikeChance) return VehicleKind.Moto;
            if (hasCar) return VehicleKind.Car;
            return hasBike ? VehicleKind.Moto : VehicleKind.Cart;
        }

        private GameObject[] PoolOf(VehicleKind kind)
        {
            switch (kind)
            {
                case VehicleKind.Moto: return motorbikePrefabs;
                case VehicleKind.Cart: return cartPrefabs;
                default: return carPrefabs;
            }
        }

        private float SpeedFor(VehicleKind kind)
        {
            switch (kind)
            {
                case VehicleKind.Cart: return Random.Range(1.8f, 2.6f);
                case VehicleKind.Moto: return Random.Range(settings.SpeedMin + 1f, settings.SpeedMax + 2f);
                default: return Random.Range(settings.SpeedMin, settings.SpeedMax);
            }
        }

        /// <summary>Una corsia libera (senza veicoli vicini) per il nuovo veicolo, o -1.</summary>
        private int FreeLane(float z)
        {
            int start = Random.Range(0, TukTukController.LaneCount);
            for (int k = 0; k < TukTukController.LaneCount; k++)
            {
                int lane = (start + k) % TukTukController.LaneCount;
                if (!LaneOccupied(lane, z, null))
                    return lane;
            }
            return -1;
        }
    }
}
