using System.Collections.Generic;
using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Mondo infinito cartoon ambientato al Cairo, ispirato a Temple Run / Subway Surfers.
    /// Questa classe coordina il mondo; la logica di generazione è nei moduli:
    /// - CityBlockBuilder: isolati, palazzi, marciapiedi e arredo;
    /// - ObstacleSpawner: bancarelle distruttibili e dossi;
    /// - TrafficSpawner: auto, moto e carretti, con distanze e blocchi fisici;
    /// - TrafficSignals: semafori agli attraversamenti pedonali;
    /// - PedestrianSpawner: pedoni sui marciapiedi e attraversamenti;
    /// - DecalSpawner: sangue e tracce di pneumatici;
    /// - RunState / RunHud: crediti, monete, record e HUD.
    /// Qui restano le monete e la distribuzione delle collisioni.
    /// Va messo nell'origine della scena (0,0,0).
    /// </summary>
    public class EndlessRoad : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] private TukTukController player;
        [SerializeField] private Material toonMaterial;

        [Header("Modelli (opzionali)")]
        [SerializeField] private GameObject[] treePrefabs;
        [SerializeField] private GameObject[] carPrefabs;
        /// <summary>Edifici generati, ordinati per quartiere (vedi Districts).</summary>
        [SerializeField] private GameObject[] buildingPrefabs;
        /// <summary>Persone modellate (prefab con pivot Hip_L/Hip_R, Shoulder_L/Shoulder_R).</summary>
        [SerializeField] private GameObject[] pedestrianPrefabs;
        /// <summary>Carretti con asino (traffico lento con cavaliere), in Generated/Stalls.</summary>
        [SerializeField] private GameObject[] cartPrefabs;
        /// <summary>Moto con rider per il traffico, in Generated/Vehicles.</summary>
        [SerializeField] private GameObject[] motorbikePrefabs;

        [Header("Strada")]
        [SerializeField] private int tileCount = 10;
        [Tooltip("Quanto avanti generare ostacoli, traffico, pedoni e monete")]
        [SerializeField] private float spawnAhead = 140f;
        [Tooltip("Probabilità di una bancarella sul marciapiede")]
        [SerializeField] private float stallChance = 0.5f;

        [Header("Dossi")]
        [SerializeField] private ObstacleSpawner.Settings obstacles = new ObstacleSpawner.Settings();

        [Header("Traffico")]
        [SerializeField] private TrafficSpawner.Settings traffic = new TrafficSpawner.Settings();
        [Tooltip("Rallentamento quando il tuk-tuk colpisce un carretto o una moto")]
        [SerializeField] private float burnSlowAmount = 6f;
        [Tooltip("Secondi in cui il tuk-tuk resta in fiamme dopo l'urto")]
        [SerializeField] private float burnTime = 2.5f;
        [Tooltip("Se si colpiscono due mezzi entro questo intervallo (secondi), il tuk-tuk si ferma")]
        [SerializeField] private float comboWindow = 4f;

        [Header("Pedoni e attraversamenti")]
        [SerializeField] private PedestrianSpawner.Settings pedestrians = new PedestrianSpawner.Settings();

        [Header("Crediti")]
        [SerializeField] private float creditsPerMeter = 0.05f;
        [SerializeField] private float cleanMetersForMax = 600f;
        [SerializeField] private float maxMultiplier = 5f;

        [Header("Nebbia (nasconde la comparsa degli oggetti)")]
        [Tooltip("Dove la nebbia inizia a coprire il mondo, in metri davanti alla camera")]
        [SerializeField] private float fogStart = 60f;
        [Tooltip("Dove la nebbia copre tutto: deve restare sotto 'spawnAhead'")]
        [SerializeField] private float fogEnd = 120f;

        [Header("Ambientazioni")]
        [SerializeField] private float zoneLength = 450f;

        [Header("Partenza")]
        [Tooltip("Quanti metri avanti parte il tuk-tuk: la strada è già generata anche dietro di lui")]
        [SerializeField] private float startOffsetZ = 36f;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
        private static readonly Color CoinRim = new Color(0.82f, 0.56f, 0.08f);
        private static readonly Color CoinFace = new Color(1f, 0.92f, 0.4f);

        private readonly List<Transform> tiles = new List<Transform>();
        private readonly List<Transform> coinList = new List<Transform>();
        private readonly List<float> recentHits = new List<float>();

        private Camera cam;
        private float nextTileZ;
        private float nextCoinZ = 18f;

        private RunState run;
        private CityBlockBuilder blocks;
        private DecalSpawner decals;
        private ObstacleSpawner obstacleSpawner;
        private TrafficSpawner trafficSpawner;
        private PedestrianSpawner walkers;
        private TrafficSignals signals;

        private void Start()
        {
            cam = Camera.main;
            blocks = new CityBlockBuilder(new BlockKit
            {
                Toon = toonMaterial,
                Buildings = buildingPrefabs,
                Trees = treePrefabs,
                StallChance = stallChance,
            });
            run = new RunState(creditsPerMeter, cleanMetersForMax, maxMultiplier);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;

            signals = new TrafficSignals();
            decals = new DecalSpawner(toonMaterial, transform);
            obstacleSpawner = new ObstacleSpawner(obstacles, toonMaterial, transform);
            trafficSpawner = new TrafficSpawner(traffic, carPrefabs, motorbikePrefabs, cartPrefabs, signals, transform);
            walkers = new PedestrianSpawner(pedestrians, toonMaterial, pedestrianPrefabs, player, signals, transform);

            // Il tuk-tuk parte un po' avanti. La strada copre anche dietro (la camera guarda indietro)
            // e fino a spawnAhead davanti: niente vuoto visibile all'avvio.
            if (player != null)
            {
                Vector3 start = player.transform.position;
                start.z = startOffsetZ;
                player.transform.position = start;
            }
            float tilesFrom = -2f * CityLayout.BlockLength;
            int needed = Mathf.Max(tileCount,
                Mathf.CeilToInt((startOffsetZ + spawnAhead - tilesFrom) / CityLayout.BlockLength) + 1);
            for (int i = 0; i < needed; i++)
                CreateTile(tilesFrom + i * CityLayout.BlockLength);

            nextTileZ = tilesFrom + needed * CityLayout.BlockLength;

            // Il mondo è già popolato al primo fotogramma: il tuk-tuk parte su una strada piena.
            float startZ = player != null ? player.transform.position.z : 0f;
            obstacleSpawner.Spawn(startZ, spawnAhead);
            trafficSpawner.Spawn(startZ, spawnAhead);
            walkers.Spawn(startZ, spawnAhead);
            SpawnCoins(startZ);
            SoundLibrary.StartAmbience();
        }

        private void Update()
        {
            if (player == null) return;

            float playerZ = player.transform.position.z;
            float dt = Time.deltaTime;

            if (cam != null)
            {
                cam.backgroundColor = Color.Lerp(cam.backgroundColor, Districts.SkyAt(playerZ), dt * 0.5f);
                // La nebbia prende il colore del cielo: il passaggio tra quartieri resta invisibile.
                RenderSettings.fogColor = cam.backgroundColor;
            }

            run.Tick(player.Distance, player.IsAlive);

            foreach (Transform tile in tiles)
            {
                if (tile.position.z + CityLayout.BlockLength < playerZ - CityLayout.BlockLength)
                {
                    tile.position = new Vector3(0f, 0f, nextTileZ);
                    nextTileZ += CityLayout.BlockLength;
                    blocks.Populate(tile);
                }
            }

            obstacleSpawner.Spawn(playerZ, spawnAhead);
            trafficSpawner.Spawn(playerZ, spawnAhead);
            trafficSpawner.Steer(dt, player);
            walkers.Spawn(playerZ, spawnAhead);

            SpawnCoins(playerZ);
            UpdateCoins(dt);

            obstacleSpawner.Cleanup(playerZ);
            trafficSpawner.Cleanup(playerZ);
            signals.Cleanup(playerZ);
            walkers.Cleanup(playerZ);
            decals.Cleanup(playerZ);
            CleanupCoins(playerZ);

            obstacleSpawner.CheckBumps(playerZ, player);

            if (player.IsAlive && player.LateralSpeed > 2.5f && player.Height <= 0f)
                decals.SkidMarks(player, playerZ, dt);

            player.GroundHeight = GroundAt(player.transform.position.x);

            SoundLibrary.SetEngine(player.Speed, 38f);
            SoundLibrary.SetFire(player.IsBurning);
            if (Random.value < dt * 0.12f) SoundLibrary.Play(Sfx.Horn, 0.35f);

            if (!player.IsAlive) return;
            CheckCollisions();
        }

        /// <summary>Dopo che il tuk-tuk si è mosso: i veicoli fermi o lenti davanti lo bloccano fisicamente.</summary>
        private void LateUpdate()
        {
            if (trafficSpawner != null) trafficSpawner.Solidify(player);
        }

        private void CheckCollisions()
        {
            TrafficCar vehicle = trafficSpawner.CheckCrash(player);
            if (vehicle != null)
            {
                trafficSpawner.Collide(vehicle, player, toonMaterial);
                SoundLibrary.Play(vehicle.Kind == VehicleKind.Car ? Sfx.Crash : Sfx.Thud);
                vehicle.MarkHit();
                if (vehicle.Kind == VehicleKind.Car)
                {
                    // Auto: tamponamento, incidente.
                    player.Crash();
                    return;
                }

                // Due urti ravvicinati: il tuk-tuk si ferma.
                recentHits.RemoveAll(t => Time.time - t > comboWindow);
                recentHits.Add(Time.time);
                if (recentHits.Count >= 2)
                {
                    player.Crash();
                    return;
                }

                // Carretto o moto: il tuk-tuk rallenta e prende fuoco; la moto vola via.
                player.Burn(burnSlowAmount, burnTime);
                run.ResetCleanRun();
                if (vehicle.Kind == VehicleKind.Moto)
                {
                    trafficSpawner.Launch(vehicle, player);
                    SoundLibrary.Play(Sfx.Whoosh);
                }
            }

            walkers.CheckCollisions(player, run, decals);
        }

        private void SpawnCoins(float playerZ)
        {
            // Le monete partono sempre davanti al tuk-tuk, anche se parte più avanti del previsto.
            nextCoinZ = Mathf.Max(nextCoinZ, playerZ + 15f);
            while (nextCoinZ < playerZ + spawnAhead)
            {
                if (Random.value < 0.6f) SpawnCoinRow(nextCoinZ);
                nextCoinZ += Random.Range(22f, 40f);
            }
        }

        /// <summary>Fila di monete d'oro su una corsia: girano e si raccolgono passandoci sopra.</summary>
        private void SpawnCoinRow(float z)
        {
            int lane = Random.Range(0, TukTukController.LaneCount);
            float x = Prims.LaneX(lane);
            for (int i = 0; i < 5; i++)
            {
                // Moneta: bordo scuro e faccia chiara, più spessa al centro, con un leggero rialzo.
                var coin = new GameObject("Moneta").transform;
                coin.SetParent(transform, false);
                coin.position = new Vector3(x, 0.9f, z + i * 1.6f);
                coin.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Prims.Create(toonMaterial, coin, PrimitiveType.Cylinder, "Bordo",
                             Vector3.zero, new Vector3(0.6f, 0.05f, 0.6f), CoinRim);
                Prims.Create(toonMaterial, coin, PrimitiveType.Cylinder, "Faccia",
                             new Vector3(0f, 0.02f, 0f), new Vector3(0.46f, 0.045f, 0.46f), CoinFace);
                coinList.Add(coin);
            }
        }

        /// <summary>Gira le monete e raccoglie quelle a contatto del tuk-tuk.</summary>
        private void UpdateCoins(float dt)
        {
            Vector3 pos = player.transform.position;
            for (int i = coinList.Count - 1; i >= 0; i--)
            {
                Transform coin = coinList[i];
                if (coin == null) { coinList.RemoveAt(i); continue; }
                coin.Rotate(Vector3.up, 200f * dt, Space.World);
                Vector3 d = coin.position - pos;
                if (Mathf.Abs(d.x) < 0.9f && Mathf.Abs(d.z) < 1.3f && Mathf.Abs(d.y - player.Height) < 1.6f)
                {
                    run.AddCoin();
                    SoundLibrary.Play(Sfx.Coin, 0.7f);
                    Destroy(coin.gameObject);
                    coinList.RemoveAt(i);
                }
            }
        }

        private void CleanupCoins(float playerZ)
        {
            for (int i = coinList.Count - 1; i >= 0; i--)
            {
                if (coinList[i] == null || coinList[i].position.z < playerZ - 20f)
                {
                    if (coinList[i] != null) Destroy(coinList[i].gameObject);
                    coinList.RemoveAt(i);
                }
            }
        }

        /// <summary>Quota del suolo a una data x: asfalto 0, marciapiede rialzato.</summary>
        private float GroundAt(float x)
        {
            return Mathf.Abs(x) <= CityLayout.RoadHalfWidth ? 0f : CityLayout.CurbHeight;
        }

        private void OnGUI()
        {
            if (player == null || run == null) return;
            RunHud.Draw(run, player);
        }

        private void CreateTile(float z)
        {
            var root = new GameObject("Tile").transform;
            root.SetParent(transform, false);
            root.position = new Vector3(0f, 0f, z);
            tiles.Add(root);
            blocks.Populate(root);
        }
    }
}
