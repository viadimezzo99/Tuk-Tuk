using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Tuk-tuk: avanzamento automatico, cambio corsia a scatto (tastiera o swipe; sempre al centro
    /// di una corsia, mai a cavallo), salto (spazio, W, freccia su o swipe verso l'alto)
    /// e velocità crescente. Dopo un incidente: frenata, fumo, fuoco.
    /// Può essere rallentato (ostacoli, pedoni investiti) e lanciato in avanti (dossi).
    /// Avanza lungo +Z.
    /// </summary>
    public class TukTukController : MonoBehaviour
    {
        [Header("Modello")]
        [SerializeField] private Material toonMaterial;

        [Header("Corsie")]
        [Tooltip("Sbandata visiva (solo il modello) durante il cambio corsia")]
        [SerializeField] private float slideYawMax = 22f;
        [SerializeField] private float bankMax = 16f;

        [Header("Incidente")]
        [Tooltip("Secondi per fermarsi dopo l'urto")]
        [SerializeField] private float crashBrakeTime = 1.2f;
        [Tooltip("Secondo in cui compare il fumo")]
        [SerializeField] private float smokeDelay = 0.5f;
        [Tooltip("Secondo in cui compare il fuoco")]
        [SerializeField] private float fireDelay = 1.3f;
        [Tooltip("Secondo in cui compare la scritta INCIDENTE")]
        [SerializeField] private float crashTextDelay = 2.4f;

        [Header("Velocità (m/s)")]
        [SerializeField] private float startSpeed = 12f;
        [SerializeField] private float minSpeed = 6f;
        [SerializeField] private float acceleration = 0.9f;

        [Header("Salto")]
        [SerializeField] private float jumpVelocity = 9f;
        [SerializeField] private float gravity = 22f;

        [Header("Swipe (pixel)")]
        [SerializeField] private float minSwipeDistance = 80f;

        /// <summary>Numero di corsie della strada (0 = sinistra ... LaneCount-1 = destra).</summary>
        public const int LaneCount = 4;
        /// <summary>Metà larghezza e metà lunghezza del tuk-tuk: usate da tutte le collisioni.</summary>
        public const float HalfWidth = 0.7f;
        public const float HalfLength = 1.0f;
        /// <summary>Quota del suolo sotto il tuk-tuk (marciapiedi e rampe), impostata dalla strada.</summary>
        public float GroundHeight { get; set; }

        public float Speed { get; private set; }
        /// <summary>Velocità effettiva in avanti (con eventuali rallentamenti e lanci).</summary>
        public float EffectiveSpeed { get; private set; }
        public float Distance { get; private set; }
        /// <summary>0 = sinistra, 1 = centro, 2 = destra.</summary>
        public int Lane { get; private set; } = 1;
        public bool IsAlive { get; private set; } = true;
        /// <summary>Altezza da terra in metri.</summary>
        public float Height { get; private set; }
        public bool IsAirborne => Height > 0f;
        /// <summary>Velocità laterale, usata per le tracce di sbandata.</summary>
        public float LateralSpeed { get; private set; }
        /// <summary>Moltiplicatore del salto, impostato dalla strada vicino ai dossi.</summary>
        public float JumpBoost { get; set; } = 1f;

        private float targetX;
        private float prevX;
        private float yaw;
        private float bank;
        private float verticalVelocity;
        private bool jumpPressed;
        private Vector2 swipeStart;
        private bool swiping;

        private float slowTimer;
        private float slowAmount;
        private float launchTimer;
        private float launchBoost;
        /// <summary>Limite di velocità imposto da un veicolo che blocca il tuk-tuk in questo fotogramma.</summary>
        private float blockSpeed = float.MaxValue;
        /// <summary>Z oltre il quale il tuk-tuk non può andare in questo fotogramma (veicolo davanti).</summary>
        private float blockZ = float.MaxValue;
        /// <summary>Velocità massima con cui il tuk-tuk scivola indietro se è finito dentro un veicolo.</summary>
        private const float PushBackSpeed = 6f;
        /// <summary>Secondi per scivolare nella corsia nuova: il cambio è morbido, non uno scatto.</summary>
        private const float LaneSlideTime = 0.18f;
        private float laneVelocity;
        /// <summary>Molla dell'inclinazione: più alta = inclinazione più rigida; più smorzata = meno oscillazione.</summary>
        private const float LeanStiffness = 120f;
        private const float LeanDamping = 7f;
        private float bankVelocity;
        /// <summary>Beccheggio: il muso si alza in salita e si abbassa all'atterraggio, poi si riassesta.</summary>
        private float pitch;
        private float pitchVelocity;
        private static readonly Color Charcoal = new Color(0.08f, 0.07f, 0.06f);
        private float bobPhase;
        private Transform model;

        private float crashTime;
        private float crashSpeed;
        private bool smokeOn;
        private bool fireOn;
        private float burnTimer;
        private GameObject burnFx;

        /// <summary>True quando la scritta INCIDENTE va mostrata (dopo fumo e fuoco).</summary>
        public bool CrashTextVisible => !IsAlive && crashTime >= crashTextDelay;
        /// <summary>True mentre il tuk-tuk è in fiamme (urto con carretto o moto, o dopo l'incidente).</summary>
        public bool IsBurning => burnFx != null || fireOn;

        private void Start()
        {
            // Il modello si costruisce a runtime: i colori (MaterialPropertyBlock) non si salvano nella scena.
            if (toonMaterial != null && transform.Find("Modello") == null)
                TukTukModel.Build(transform, toonMaterial);

            // Parte già in strada, a velocità ridotta, e accelera da sola.
            Speed = startSpeed * 0.6f;
            // Parte al centro della sua corsia, mai sul confine.
            targetX = Prims.LaneX(Lane);
            Vector3 start = transform.position;
            start.x = targetX;
            transform.position = start;
            prevX = targetX;
            model = transform.Find("Modello");
        }

        private void Update()
        {
            if (!IsAlive)
            {
                UpdateCrash(Time.deltaTime);
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && CrashTextVisible)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }

            float dt = Time.deltaTime;
            // Nessun tetto: l'infinite runner accelera sempre, finché non ci si schianta.
            Speed += acceleration * dt;

            // Rallentamento temporaneo (ostacoli, pedoni investiti) e spinta dei dossi.
            if (slowTimer > 0f) slowTimer -= dt;
            if (launchTimer > 0f) launchTimer -= dt;
            if (burnTimer > 0f)
            {
                burnTimer -= dt;
                if (burnTimer <= 0f && burnFx != null)
                {
                    Destroy(burnFx);
                    burnFx = null;
                }
            }
            float boost = launchTimer > 0f ? launchBoost : 0f;
            EffectiveSpeed = Mathf.Max(minSpeed * 0.5f, Speed + boost - (slowTimer > 0f ? slowAmount : 0f));
            EffectiveSpeed = Mathf.Min(EffectiveSpeed, blockSpeed);
            blockSpeed = float.MaxValue;
            Distance += EffectiveSpeed * dt;

            ReadInput();

            // Salto: parte solo da terra.
            if (jumpPressed && !IsAirborne)
            {
                verticalVelocity = jumpVelocity * JumpBoost;
                SoundLibrary.Play(Sfx.Jump, 0.6f);
            }
            jumpPressed = false;

            // Gravità.
            if (IsAirborne || verticalVelocity > 0f)
            {
                verticalVelocity -= gravity * dt;
                Height += verticalVelocity * dt;
                if (Height <= 0f)
                {
                    Height = 0f;
                    verticalVelocity = 0f;
                }
            }

            // Corsia: la posizione laterale è sempre il centro della corsia (niente posizioni intermedie).
            Vector3 p = transform.position;
            prevX = p.x;
            p.x = Mathf.SmoothDamp(p.x, targetX, ref laneVelocity, LaneSlideTime, Mathf.Infinity, dt);
            p.y = Height + GroundHeight;
            p.z = NextZ(p.z, EffectiveSpeed * dt);
            transform.position = p;

            // Sbandata solo visiva: il cambio corsia è uno scatto, quindi si usa il salto di posizione.
            LateralSpeed = dt > 0f ? (p.x - prevX) / dt : 0f;
            float targetYaw = Mathf.Clamp(-LateralSpeed * 0.02f, -slideYawMax, slideYawMax);
            yaw = Mathf.Lerp(yaw, targetYaw, 10f * dt);
            // Inclinazione a molla: si piega nella curva, supera un po' il punto di equilibrio e si riassesta.
            float leanTarget = Mathf.Clamp(-LateralSpeed * 0.7f, -bankMax, bankMax);
            float stepDt = Mathf.Min(dt, 0.033f);
            float leanAccel = LeanStiffness * (leanTarget - bank) - LeanDamping * bankVelocity;
            bankVelocity += leanAccel * stepDt;
            bank += bankVelocity * stepDt;
            // Salto: il muso si alza mentre sale e cade mentre atterra, con un piccolo rimbalzo.
            float pitchTarget = IsAirborne ? Mathf.Clamp(-verticalVelocity * 1.2f, -14f, 14f) : 0f;
            float pitchAccel = LeanStiffness * (pitchTarget - pitch) - LeanDamping * pitchVelocity;
            pitchVelocity += pitchAccel * stepDt;
            pitch += pitchVelocity * stepDt;
            transform.rotation = Quaternion.Euler(pitch, yaw, bank);

            // Oscillazione del modello su e giù mentre avanza (solo visiva: il corpo di collisione non cambia).
            bobPhase += dt * (4f + EffectiveSpeed * 0.3f);
            if (model != null)
                model.localPosition = new Vector3(0f, Mathf.Sin(bobPhase * 2f) * 0.025f * (IsAirborne ? 0f : 1f), 0f);
        }

        private void ReadInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                    ChangeLane(-1);
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                    ChangeLane(1);
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame
                    || keyboard.upArrowKey.wasPressedThisFrame)
                    jumpPressed = true;
            }

            var touch = Touchscreen.current;
            if (touch != null)
            {
                var primary = touch.primaryTouch;
                if (primary.press.wasPressedThisFrame)
                {
                    swipeStart = primary.position.ReadValue();
                    swiping = true;
                }
                if (swiping && primary.press.wasReleasedThisFrame)
                {
                    swiping = false;
                    Vector2 delta = primary.position.ReadValue() - swipeStart;
                    if (delta.y >= minSwipeDistance && delta.y > Mathf.Abs(delta.x))
                        jumpPressed = true;
                    else if (Mathf.Abs(delta.x) >= minSwipeDistance)
                        ChangeLane(delta.x > 0f ? 1 : -1);
                }
            }
        }

        /// <summary>Cambio di una corsia a sinistra (-1) o a destra (+1), a scatto.</summary>
        public void ChangeLane(int direction)
        {
            if (!IsAlive) return;
            Lane = Mathf.Clamp(Lane + direction, 0, LaneCount - 1);
            targetX = Prims.LaneX(Lane);
            SoundLibrary.Play(Sfx.Whoosh, 0.4f);
        }

        /// <summary>Rallenta il tuk-tuk per un breve periodo (ostacolo colpito, pedone investito).</summary>
        public void Slow(float amount, float duration)
        {
            if (!IsAlive) return;
            slowAmount = Mathf.Max(slowAmount * (slowTimer > 0f ? 1f : 0f), amount);
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        /// <summary>Spinta in avanti (dossi): aumenta la velocità per un breve periodo.</summary>
        public void Launch(float extraSpeed, float duration)
        {
            if (!IsAlive) return;
            launchBoost = extraSpeed;
            launchTimer = duration;
        }

        /// <summary>Salto automatico (es. sul dosso), solo da terra.</summary>
        public void Hop()
        {
            if (!IsAlive || IsAirborne) return;
            verticalVelocity = jumpVelocity * JumpBoost;
        }

        /// <summary>
        /// Incidente: il tuk-tuk non si ferma di colpo. Frena in pochi secondi, poi compaiono
        /// fumo e fuoco, e infine la scritta INCIDENTE.
        /// </summary>
        /// <summary>Urto con carretto o moto: rallenta e prende fuoco per qualche secondo, senza fermarsi.</summary>
        public void Burn(float slowAmount, float duration)
        {
            if (!IsAlive) return;
            Slow(slowAmount, duration);
            if (burnFx == null) burnFx = CrashEffects.StartFire(transform);
            burnTimer = Mathf.Max(burnTimer, duration);
        }

        /// <summary>
        /// Un veicolo fermo o più lento davanti al tuk-tuk lo blocca: il tuk-tuk non può superare
        /// zLimit e non può andare più veloce di speedLimit. Va richiamato ogni fotogramma di contatto.
        /// </summary>
        public void Block(float zLimit, float speedLimit)
        {
            // Niente teletrasporti: il limite viene applicato al passo di movimento del fotogramma successivo.
            blockZ = Mathf.Min(blockZ, zLimit);
            blockSpeed = Mathf.Min(blockSpeed, speedLimit);
        }

        /// <summary>
        /// Posizione z dopo il passo di questo fotogramma, senza superare il veicolo che blocca.
        /// Se il tuk-tuk è già dentro (per un urto), scivola indietro piano fino al limite.
        /// </summary>
        private float NextZ(float z, float forward)
        {
            float limit = blockZ;
            blockZ = float.MaxValue;
            if (limit == float.MaxValue) return z + forward;
            if (z > limit) return Mathf.Max(limit, z - PushBackSpeed * Time.deltaTime);
            return Mathf.Min(z + forward, limit);
        }

        /// <summary>Tutte le parti del modello diventano carbone.</summary>
        private void Char()
        {
            if (model == null || toonMaterial == null) return;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
                ToonUtil.Paint(r, toonMaterial, Charcoal, null, default);
        }

        public void Crash()
        {
            if (!IsAlive) return;
            IsAlive = false;
            SoundLibrary.Play(Sfx.Brake, 0.7f);
            crashTime = 0f;
            crashSpeed = EffectiveSpeed;
            Speed = 0f;
        }

        private void UpdateCrash(float dt)
        {
            crashTime += dt;

            // Frenata progressiva: la velocità scende a zero in crashBrakeTime secondi.
            float t = Mathf.Clamp01(crashTime / Mathf.Max(0.01f, crashBrakeTime));
            EffectiveSpeed = Mathf.Min(Mathf.Lerp(crashSpeed, 0f, t), blockSpeed);
            blockSpeed = float.MaxValue;
            Distance += EffectiveSpeed * dt;

            Vector3 p = transform.position;
            p.z = NextZ(p.z, EffectiveSpeed * dt);
            p.x = targetX;
            transform.position = p;

            if (!smokeOn && crashTime >= smokeDelay)
            {
                smokeOn = true;
                CrashEffects.StartSmoke(transform);
            }
            if (!fireOn && crashTime >= fireDelay)
            {
                fireOn = true;
                CrashEffects.StartFire(transform);
                Char();
                Impacts.Burst(toonMaterial, transform.parent, transform.position + Vector3.up, Vector3.up * 4f, 24, false);
            }

            // Il mezzo si affloscia lentamente sul suo stesso telaio: distrutto.
            if (model != null && crashTime >= fireDelay)
            {
                float ruin = Mathf.Clamp01((crashTime - fireDelay) / 3f);
                model.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.6f, ruin), 1f);
                model.localPosition = new Vector3(0f, Mathf.Lerp(0f, -0.25f, ruin), 0f);
            }
        }
    }
}
