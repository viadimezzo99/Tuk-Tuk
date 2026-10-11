using System.Collections.Generic;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Semafori agli attraversamenti pedonali. Ogni semaforo alterna verde (i veicoli passano)
    /// e rosso (i veicoli si fermano alla linea d'arresto, prima delle strisce).
    /// La fase è sfalsata per ogni semaforo, così non cambiano tutti insieme.
    /// </summary>
    public class TrafficSignals
    {
        public const float GreenTime = 7f;
        public const float RedTime = 5f;
        public const float CycleLength = GreenTime + RedTime;
        /// <summary>Distanza della linea d'arresto prima delle strisce (metri).</summary>
        public const float StopOffset = 6f;

        private class Signal
        {
            public float Z;
            public float Phase;
        }

        private readonly List<Signal> signals = new List<Signal>();

        public void Add(float crossingZ, float phase)
        {
            signals.Add(new Signal { Z = crossingZ, Phase = phase });
        }

        public void Cleanup(float playerZ)
        {
            signals.RemoveAll(s => s.Z < playerZ - 40f);
        }

        /// <summary>Vero se un semaforo con questa fase è rosso al tempo indicato.</summary>
        public static bool IsRed(float phase, float time)
        {
            return (time + phase) % CycleLength >= GreenTime;
        }

        /// <summary>Linea d'arresto del semaforo rosso più vicino davanti al veicolo, entro range metri.</summary>
        public bool NearestRedStop(float vehicleZ, float range, float time, out float stopZ)
        {
            stopZ = 0f;
            float best = range;
            bool found = false;
            foreach (Signal s in signals)
            {
                float stop = s.Z - StopOffset;
                float d = stop - vehicleZ;
                if (d < 0f || d > best) continue;
                if (!IsRed(s.Phase, time)) continue;
                best = d;
                stopZ = stop;
                found = true;
            }
            return found;
        }
    }
}
