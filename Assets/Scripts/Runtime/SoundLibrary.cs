using System;
using System.Collections.Generic;
using UnityEngine;

namespace TukTuk.Gameplay
{
    public enum Sfx { Coin, Thud, Crash, Horn, Whoosh, Scream, Brake, Jump }

    /// <summary>
    /// Tutti i suoni del gioco, generati in codice all'avvio (nessun file audio da importare, leggeri su mobile).
    /// - effetti una tantum: monete, impatti, incidente, clacson, frenata, urlo, salto;
    /// - loop: motore del tuk-tuk (tono legato alla velocità), fuoco (crepitio), traffico di sottofondo.
    /// L'audio si attiva o disattiva con SetEnabled e la scelta resta salvata.
    /// </summary>
    public static class SoundLibrary
    {
        private const string PrefKey = "TukTuk.Audio";
        private const int Rate = 22050;
        private const float SfxVolume = 0.8f;

        private static GameObject host;
        private static AudioSource oneShot;
        private static AudioSource engine;
        private static AudioSource fire;
        private static AudioSource traffic;
        private static readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        private static bool enabled = true;

        public static bool Enabled => enabled;

        /// <summary>Crea le sorgenti e i suoni la prima volta (e di nuovo dopo un riavvio di Play in editor).</summary>
        private static void EnsureReady()
        {
            if (host != null) return;

            enabled = PlayerPrefs.GetInt(PrefKey, 1) == 1;
            AudioListener.volume = enabled ? 1f : 0f;

            host = new GameObject("Audio");
            UnityEngine.Object.DontDestroyOnLoad(host);
            // Un solo ascoltatore audio per scena: se la camera non ne ha uno, lo mette il suono.
            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
                host.AddComponent<AudioListener>();
            oneShot = host.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;
            oneShot.spatialBlend = 0f;

            engine = NewLoop(BuildEngine(), 0.18f);
            fire = NewLoop(BuildFire(), 0.5f);
            traffic = NewLoop(BuildTraffic(), 0.15f);

            clips.Clear();
            clips[Sfx.Coin] = BuildCoin();
            clips[Sfx.Thud] = BuildThud();
            clips[Sfx.Crash] = BuildCrash();
            clips[Sfx.Horn] = BuildHorn();
            clips[Sfx.Whoosh] = BuildWhoosh();
            clips[Sfx.Scream] = BuildScream();
            clips[Sfx.Brake] = BuildBrake();
            clips[Sfx.Jump] = BuildJump();
        }

        /// <summary>Avvia il motore e il traffico di sottofondo (chiamato all'inizio della corsa).</summary>
        public static void StartAmbience()
        {
            EnsureReady();
            if (enabled)
            {
                if (!engine.isPlaying) engine.Play();
                if (!traffic.isPlaying) traffic.Play();
            }
        }

        public static void Play(Sfx sound, float volume = 1f)
        {
            EnsureReady();
            if (!enabled || !clips.TryGetValue(sound, out AudioClip clip)) return;
            oneShot.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            oneShot.PlayOneShot(clip, SfxVolume * volume);
        }

        /// <summary>Il tono del motore segue la velocità del tuk-tuk (0 = minimo, MaxSpeed = massimo).</summary>
        public static void SetEngine(float speed, float maxSpeed)
        {
            EnsureReady();
            engine.pitch = Mathf.Lerp(0.7f, 1.6f, Mathf.Clamp01(speed / Mathf.Max(0.01f, maxSpeed)));
        }

        /// <summary>Crepitio del fuoco mentre il tuk-tuk brucia.</summary>
        public static void SetFire(bool burning)
        {
            EnsureReady();
            if (!enabled) return;
            if (burning && !fire.isPlaying) fire.Play();
            if (!burning && fire.isPlaying) fire.Stop();
        }

        /// <summary>Attiva o disattiva tutto l'audio e ricorda la scelta.</summary>
        public static void SetEnabled(bool on)
        {
            EnsureReady();
            enabled = on;
            AudioListener.volume = on ? 1f : 0f;
            PlayerPrefs.SetInt(PrefKey, on ? 1 : 0);
            if (on) StartAmbience();
        }

        private static AudioSource NewLoop(AudioClip clip, float volume)
        {
            var src = host.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.volume = volume;
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            return src;
        }

        // ---------- generazione dei suoni ----------

        private static int Samples(float seconds)
        {
            return Mathf.Max(1, (int)(seconds * Rate));
        }

        private static AudioClip Make(string name, float[] data, bool loop)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            clip.name = name;
            return clip;
        }

        private static AudioClip BuildCoin()
        {
            float secs = 0.3f;
            float[] d = new float[Samples(secs)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = t < 0.08f ? 880f : 1320f;
                d[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 14f) * 0.5f;
            }
            return Make("Moneta", d, false);
        }

        private static AudioClip BuildThud()
        {
            float secs = 0.4f;
            float[] d = new float[Samples(secs)];
            var rng = new System.Random(3);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                lp = lp * 0.85f + ((float)rng.NextDouble() * 2f - 1f) * 0.15f;
                float env = Mathf.Exp(-t * 9f);
                d[i] = (lp * 0.8f + Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.6f) * env * 0.9f;
            }
            return Make("Botto", d, false);
        }

        private static AudioClip BuildCrash()
        {
            float secs = 1.2f;
            float[] d = new float[Samples(secs)];
            var rng = new System.Random(5);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                lp = lp * 0.6f + ((float)rng.NextDouble() * 2f - 1f) * 0.4f;
                float noise = lp * Mathf.Exp(-t * 3f) * 0.9f;
                float clang = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Exp(-t * 6f) * 0.4f
                            + Mathf.Sin(2f * Mathf.PI * 340f * t) * Mathf.Exp(-t * 5f) * 0.3f;
                float body = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-t * 4f) * 0.5f;
                d[i] = noise + clang + body;
            }
            return Make("Incidente", d, false);
        }

        private static AudioClip BuildHorn()
        {
            float secs = 0.6f;
            float[] d = new float[Samples(secs)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float a = Mathf.Sin(2f * Mathf.PI * 390f * t);
                float b = Mathf.Sin(2f * Mathf.PI * 490f * t);
                float square = (Mathf.Sign(a) + Mathf.Sign(b)) * 0.12f;
                float env = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((secs - t) / 0.1f);
                d[i] = square * env;
            }
            return Make("Clacson", d, false);
        }

        private static AudioClip BuildWhoosh()
        {
            float secs = 0.45f;
            float[] d = new float[Samples(secs)];
            var rng = new System.Random(9);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float a = 0.5f + 0.45f * (t / secs);
                lp = lp * a + ((float)rng.NextDouble() * 2f - 1f) * (1f - a);
                d[i] = lp * Mathf.Sin(Mathf.PI * t / secs) * 0.9f;
            }
            return Make("Scatto", d, false);
        }

        private static AudioClip BuildScream()
        {
            float secs = 0.6f;
            float[] d = new float[Samples(secs)];
            var rng = new System.Random(11);
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = 700f + 500f * (t / secs) + 30f * Mathf.Sin(2f * Mathf.PI * 9f * t);
                phase += 2f * Mathf.PI * f / Rate;
                float env = Mathf.Sin(Mathf.PI * t / secs);
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.12f;
                d[i] = (Mathf.Sin(phase) * 0.35f + noise) * env;
            }
            return Make("Urlo", d, false);
        }

        private static AudioClip BuildBrake()
        {
            float secs = 0.8f;
            float[] d = new float[Samples(secs)];
            var rng = new System.Random(13);
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = 1100f + 60f * Mathf.Sin(2f * Mathf.PI * 30f * t);
                phase += 2f * Mathf.PI * f / Rate;
                float squeal = Mathf.Sign(Mathf.Sin(phase)) * 0.12f;
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.1f;
                d[i] = (squeal + noise) * Mathf.Exp(-t * 2.5f);
            }
            return Make("Frenata", d, false);
        }

        private static AudioClip BuildJump()
        {
            float secs = 0.25f;
            float[] d = new float[Samples(secs)];
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = 300f + 500f * (t / secs);
                phase += 2f * Mathf.PI * f / Rate;
                d[i] = Mathf.Sin(phase) * 0.3f * Mathf.Sin(Mathf.PI * t / secs);
            }
            return Make("Salto", d, false);
        }

        /// <summary>Motore a due secondi: tutte le armoniche sono multiple intere, quindi il loop è continuo.</summary>
        private static AudioClip BuildEngine()
        {
            float secs = 2f;
            float[] d = new float[Samples(secs)];
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int k = 1; k <= 6; k++)
                    s += Mathf.Sin(2f * Mathf.PI * 38f * k * t) / k;
                d[i] = s * 0.25f;
            }
            return Make("Motore", d, true);
        }

        /// <summary>Crepitio: impulsi casuali su un rombo basso.</summary>
        private static AudioClip BuildFire()
        {
            float secs = 2f;
            float[] d = new float[Samples(secs)];
            var rng = new System.Random(17);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                lp = lp * 0.97f + ((float)rng.NextDouble() * 2f - 1f) * 0.03f;
                float pop = rng.NextDouble() > 0.985 ? (float)rng.NextDouble() * 0.9f : 0f;
                d[i] = lp * 1.5f + pop + Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.05f;
            }
            return Make("Fuoco", d, true);
        }

        /// <summary>Traffico di sottofondo: rombo e rumore lontano, loop di quattro secondi.</summary>
        private static AudioClip BuildTraffic()
        {
            float secs = 4f;
            float[] d = new float[Samples(secs)];
            var rng = new System.Random(19);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                lp = lp * 0.98f + ((float)rng.NextDouble() * 2f - 1f) * 0.02f;
                d[i] = lp * 0.9f + Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.1f;
            }
            return Make("Traffico", d, true);
        }
    }
}
