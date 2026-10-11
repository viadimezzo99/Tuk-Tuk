using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Stato di gioco della corsa: crediti, monete, moltiplicatore, incidenti, record e monete salvate.
    /// Nessuna grafica: la logica è separata da HUD e spawner, così si può testare da sola.
    /// </summary>
    public class RunState
    {
        public const string RecordKey = "TukTuk.Record";
        public const string CoinsKey = "TukTuk.Coins";
        public const float CoinValue = 5f;

        private readonly float creditsPerMeter;
        private readonly float cleanMetersForMax;
        private readonly float maxMultiplier;

        private float cleanMeters;
        private float lastDistance;
        private bool saved;

        public RunState(float creditsPerMeter, float cleanMetersForMax, float maxMultiplier)
        {
            this.creditsPerMeter = creditsPerMeter;
            this.cleanMetersForMax = cleanMetersForMax;
            this.maxMultiplier = maxMultiplier;
            Record = PlayerPrefs.GetFloat(RecordKey, 0f);
            CoinsTotal = PlayerPrefs.GetInt(CoinsKey, 0);
        }

        /// <summary>Record di distanza salvato.</summary>
        public float Record { get; private set; }
        /// <summary>Monete raccolte in tutte le corse salvate.</summary>
        public int CoinsTotal { get; private set; }
        /// <summary>Monete raccolte in questa corsa.</summary>
        public int CoinsRun { get; private set; }
        /// <summary>Pedoni investiti in questa corsa.</summary>
        public int RunOverCount { get; private set; }
        /// <summary>Crediti guadagnati in questa corsa.</summary>
        public float Credits { get; private set; }

        /// <summary>Moltiplicatore: cresce con i metri guidati senza incidenti.</summary>
        public float Multiplier =>
            1f + Mathf.Min(maxMultiplier - 1f, cleanMeters / cleanMetersForMax * (maxMultiplier - 1f));

        /// <summary>Da chiamare ogni frame: accumula i crediti da guida e salva a fine corsa.</summary>
        public void Tick(float distance, bool alive)
        {
            float driven = distance - lastDistance;
            lastDistance = distance;

            if (alive && driven > 0f)
            {
                cleanMeters += driven;
                Credits += driven * creditsPerMeter * Multiplier;
            }

            if (!alive && !saved)
            {
                saved = true;
                CoinsTotal += CoinsRun;
                PlayerPrefs.SetInt(CoinsKey, CoinsTotal);
                if (distance > Record)
                {
                    Record = distance;
                    PlayerPrefs.SetFloat(RecordKey, Record);
                    PlayerPrefs.Save();
                }
            }
        }

        public void AddCoin()
        {
            CoinsRun++;
            Credits += CoinValue * Multiplier;
        }

        public void AddInvested()
        {
            RunOverCount++;
        }

        /// <summary>Un incidente azzera il moltiplicatore (ma non i crediti già guadagnati).</summary>
        public void ResetCleanRun()
        {
            cleanMeters = 0f;
        }
    }
}
