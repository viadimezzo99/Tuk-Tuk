using UnityEngine;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Interfaccia di corsa: solo disegno. Legge lo stato (RunState) e il tuk-tuk, non modifica nulla.
    /// </summary>
    public static class RunHud
    {
        public static void Draw(RunState s, TukTukController player)
        {
            float k = Hud.K;

            // Centro in alto: distanza grande, record sotto.
            var center = new Rect(Screen.width * 0.5f - 170f * k, 14f * k, 340f * k, 78f * k);
            Hud.Box(center);
            Hud.Label(new Rect(center.x, center.y + 4f * k, center.width, 44f * k),
                      $"{player.Distance:0} m", 36f, TextAnchor.MiddleCenter, Hud.White);
            Hud.Label(new Rect(center.x, center.y + 46f * k, center.width, 26f * k),
                      $"Record {s.Record:0} m", 16f, TextAnchor.MiddleCenter, Hud.Soft);

            // Sinistra in alto: velocità.
            var speed = new Rect(14f * k, 14f * k, 200f * k, 60f * k);
            Hud.Box(speed);
            Hud.Label(new Rect(speed.x + 14f * k, speed.y, speed.width - 20f * k, speed.height),
                      $"{player.EffectiveSpeed * 3.6f:0} km/h", 28f, TextAnchor.MiddleLeft, Hud.White);

            // Destra in alto: monete totali (con quelle di questa corsa).
            var coins = new Rect(Screen.width - 250f * k, 14f * k, 236f * k, 52f * k);
            Hud.Box(coins);
            Hud.Coin(new Rect(coins.x + 12f * k, coins.y + 9f * k, 34f * k, 34f * k));
            Hud.Label(new Rect(coins.x + 56f * k, coins.y, coins.width - 64f * k, coins.height),
                      $"{s.CoinsTotal + s.CoinsRun}", 26f, TextAnchor.MiddleLeft, Hud.Gold);

            // Destra: crediti e moltiplicatore.
            var cred = new Rect(Screen.width - 250f * k, 74f * k, 236f * k, 46f * k);
            Hud.Box(cred);
            Hud.Label(new Rect(cred.x + 14f * k, cred.y, cred.width - 20f * k, cred.height),
                      $"Crediti {s.Credits:0}   x{s.Multiplier:0.0}", 18f, TextAnchor.MiddleLeft, Hud.White);

            // Destra: investiti.
            var hit = new Rect(Screen.width - 250f * k, 128f * k, 236f * k, 34f * k);
            Hud.Box(hit);
            Hud.Label(new Rect(hit.x + 14f * k, hit.y, hit.width - 20f * k, hit.height),
                      $"Investiti {s.RunOverCount}", 15f, TextAnchor.MiddleLeft, Hud.Soft);

            // Sinistra in basso: comandi.
            var help = new Rect(14f * k, Screen.height - 46f * k, 660f * k, 34f * k);
            Hud.Box(help);
            Hud.Label(new Rect(help.x + 14f * k, help.y, help.width - 20f * k, help.height),
                      "Frecce/A-D: cambia corsia   Spazio/W: salto   Tocco: swipe", 14f, TextAnchor.MiddleLeft, Hud.Soft);

            if (player.CrashTextVisible)
            {
                var over = new Rect(Screen.width * 0.5f - 260f * k, Screen.height * 0.5f - 50f * k, 520f * k, 100f * k);
                Hud.Box(over);
                Hud.Label(over, "Incidente!  Premi R per ripartire", 26f, TextAnchor.MiddleCenter, Hud.White);
            }
        }
    }
}
