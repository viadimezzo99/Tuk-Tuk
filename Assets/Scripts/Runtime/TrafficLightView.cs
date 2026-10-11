using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>Lampade del semaforo: rosso o verde, in base alla fase (vedi TrafficSignals).</summary>
    public class TrafficLightView : MonoBehaviour
    {
        private static readonly Color RedOn = new Color(1f, 0.15f, 0.12f);
        private static readonly Color RedOff = new Color(0.35f, 0.12f, 0.12f);
        private static readonly Color GreenOn = new Color(0.2f, 0.95f, 0.35f);
        private static readonly Color GreenOff = new Color(0.15f, 0.35f, 0.2f);

        private Material toon;
        private float phase;
        private Renderer redLamp;
        private Renderer greenLamp;
        private bool? shownRed;

        public void Init(Material toon, float phase, Renderer redLamp, Renderer greenLamp)
        {
            this.toon = toon;
            this.phase = phase;
            this.redLamp = redLamp;
            this.greenLamp = greenLamp;
        }

        private void Update()
        {
            bool red = TrafficSignals.IsRed(phase, Time.time);
            if (shownRed == red) return;
            shownRed = red;

            ToonUtil.Paint(redLamp, toon, red ? RedOn : RedOff, null, default);
            ToonUtil.Paint(greenLamp, toon, red ? GreenOff : GreenOn, null, default);
        }
    }
}
