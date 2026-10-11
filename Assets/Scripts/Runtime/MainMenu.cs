using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TukTuk.Gameplay
{
    /// <summary>
    /// Schermata di apertura: un pezzo di Cairo animato a destra (case, sole, palme), il tuk-tuk a sinistra,
    /// il logo in alto e tre pagine: Gioca, Impostazioni (solo audio) e Crediti.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Material toonMaterial;
        [SerializeField] private string gameScene = "Prototype";

        private enum Page { Main, Settings, Credits }

        private static readonly Color Asphalt = new Color(0.22f, 0.22f, 0.24f);
        private static readonly Color Sidewalk = new Color(0.86f, 0.8f, 0.7f);
        private static readonly Color Yellow = new Color(1f, 0.82f, 0.15f);
        private static readonly Color Red = new Color(0.86f, 0.18f, 0.14f);
        private static readonly Color Dark = new Color(0.12f, 0.06f, 0.02f);
        private static readonly Color Orange = new Color(1f, 0.52f, 0.1f);
        private static readonly Color Teal = new Color(0.12f, 0.55f, 0.6f);
        private static readonly Color Plum = new Color(0.5f, 0.25f, 0.6f);
        private static readonly Color Facade1 = new Color(0.93f, 0.82f, 0.62f);
        private static readonly Color Facade2 = new Color(0.85f, 0.55f, 0.38f);
        private static readonly Color Facade3 = new Color(0.96f, 0.9f, 0.75f);
        private static readonly Color Facade4 = new Color(0.7f, 0.5f, 0.35f);
        private static readonly Color Facade5 = new Color(0.55f, 0.75f, 0.8f);
        private static readonly Color Facade6 = new Color(0.9f, 0.7f, 0.4f);
        private static readonly Color WindowColor = new Color(0.2f, 0.26f, 0.32f);

        private Transform tuk;
        private float record;
        private Page page = Page.Main;
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle buttonStyle;
        private GUIStyle headingStyle;
        private bool stylesReady;

        private void Start()
        {
            record = PlayerPrefs.GetFloat(RunState.RecordKey, 0f);
            BuildStage();
        }

        private void BuildStage()
        {
            var stage = new GameObject("Scena").transform;
            stage.SetParent(transform, false);

            // Strada e marciapiedi.
            Prims.Create(toonMaterial, stage, PrimitiveType.Cube, "Asfalto",
                         new Vector3(0f, -0.1f, 14f), new Vector3(14f, 0.2f, 90f), Asphalt);
            Prims.Create(toonMaterial, stage, PrimitiveType.Cube, "MarciapiedeSinistro",
                         new Vector3(-8f, 0.2f, 14f), new Vector3(4f, 0.4f, 90f), Sidewalk);
            Prims.Create(toonMaterial, stage, PrimitiveType.Cube, "MarciapiedeDestro",
                         new Vector3(8f, 0.2f, 14f), new Vector3(4f, 0.4f, 90f), Sidewalk);
            for (int i = 0; i < 12; i++)
            {
                Prims.Create(toonMaterial, stage, PrimitiveType.Cube, "Striscia",
                             new Vector3(0f, 0f, -6f + i * 6f), new Vector3(0.2f, 0.02f, 3f), new Color(1f, 1f, 0.9f));
            }

            // Case colorate ai due lati, con finestre.
            Color[] palette = { Facade1, Facade2, Facade3, Facade4, Facade5, Facade6 };
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 9; i++)
                {
                    float z = -4f + i * 9f + Random(-1f, 1f);
                    float h = Random(5f, 11f);
                    float x = side * 12.6f;
                    Color c = palette[(i + (side > 0 ? 2 : 0)) % palette.Length];
                    Prims.Create(toonMaterial, stage, PrimitiveType.Cube, "Casa",
                                 new Vector3(x, h * 0.5f, z), new Vector3(5f, h, 7f), c);
                    for (int floor = 0; floor < (int)(h / 2.2f); floor++)
                    {
                        for (int w = -1; w <= 1; w++)
                        {
                            Prims.Create(toonMaterial, stage, PrimitiveType.Cube, "Finestra",
                                         new Vector3(side * 9.9f, 1.4f + floor * 2.2f, z + w * 2.1f),
                                         new Vector3(0.1f, 0.9f, 0.9f), WindowColor);
                        }
                    }
                }
            }

            // Sole, nuvole, dune e palme.
            Prims.Create(toonMaterial, stage, PrimitiveType.Sphere, "Sole",
                         new Vector3(-6f, 13f, 60f), Vector3.one * 7f, new Color(1f, 0.9f, 0.4f));
            Cloud(stage, new Vector3(-4f, 15f, 34f));
            Cloud(stage, new Vector3(6f, 17f, 52f));
            Cloud(stage, new Vector3(-10f, 14f, 70f));
            Prims.Create(toonMaterial, stage, PrimitiveType.Sphere, "DunaSinistra",
                         new Vector3(-26f, -2f, 70f), new Vector3(30f, 9f, 18f), new Color(0.9f, 0.74f, 0.48f));
            Prims.Create(toonMaterial, stage, PrimitiveType.Sphere, "DunaDestra",
                         new Vector3(28f, -2f, 80f), new Vector3(34f, 11f, 20f), new Color(0.88f, 0.7f, 0.45f));
            Palm(stage, new Vector3(-11.4f, 0f, 22f));
            Palm(stage, new Vector3(11.4f, 0f, 30f));
            Palm(stage, new Vector3(-11.4f, 0f, 40f));

            // Tuk-tuk a sinistra, il gioco parte da lì.
            var holder = new GameObject("TukTukMenu").transform;
            holder.SetParent(transform, false);
            holder.position = new Vector3(-2.4f, 0f, 0f);
            holder.rotation = Quaternion.Euler(0f, 18f, 0f);
            TukTukModel.Build(holder, toonMaterial);
            tuk = holder;
        }

        private static float Random(float min, float max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        private void Cloud(Transform parent, Vector3 p)
        {
            Prims.Create(toonMaterial, parent, PrimitiveType.Sphere, "Nuvola", p, Vector3.one * 3.2f, Color.white);
            Prims.Create(toonMaterial, parent, PrimitiveType.Sphere, "Nuvola", p + new Vector3(2.4f, -0.4f, 0f), Vector3.one * 2.6f, Color.white);
            Prims.Create(toonMaterial, parent, PrimitiveType.Sphere, "Nuvola", p + new Vector3(-2.2f, -0.5f, 0.6f), Vector3.one * 2.4f, Color.white);
        }

        private void Palm(Transform parent, Vector3 p)
        {
            Prims.Create(toonMaterial, parent, PrimitiveType.Cylinder, "Tronco",
                         p + Vector3.up * 2.2f, new Vector3(0.3f, 2.2f, 0.3f), new Color(0.45f, 0.26f, 0.14f));
            Prims.Create(toonMaterial, parent, PrimitiveType.Sphere, "Chioma",
                         p + Vector3.up * 4.8f, new Vector3(3.2f, 1.1f, 3.2f), new Color(0.2f, 0.62f, 0.3f));
        }

        private void Update()
        {
            if (tuk != null)
            {
                float t = Time.time;
                tuk.localRotation = Quaternion.Euler(0f, 18f + Mathf.Sin(t * 1.6f) * 1.5f, 0f);
                tuk.localPosition = new Vector3(-2.4f, Mathf.Abs(Mathf.Sin(t * 6f)) * 0.04f, 0f);
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && page == Page.Main
                && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                StartRun();
        }

        private void StartRun()
        {
            SceneManager.LoadScene(gameScene);
        }

        private void OnGUI()
        {
            EnsureStyles();
            float k = Screen.height / 800f;
            float cx = Screen.width * 0.7f;
            float panelW = 440f * k;
            float left = cx - panelW * 0.5f;
            float top = Screen.height * 0.08f;

            DrawLogo(cx, top, k);

            // Pannello dietro ai pulsanti.
            GUI.color = new Color(0.1f, 0.06f, 0.03f, 0.55f);
            GUI.Box(new Rect(left - 20f * k, top + 250f * k, panelW + 40f * k, 420f * k), GUIContent.none);
            GUI.color = Color.white;

            switch (page)
            {
                case Page.Main: DrawMain(left, panelW, top, k); break;
                case Page.Settings: DrawSettings(left, panelW, top, k); break;
                case Page.Credits: DrawCredits(left, panelW, top, k); break;
            }
        }

        private void DrawLogo(float cx, float top, float k)
        {
            float bob = Mathf.Sin(Time.time * 1.5f) * 4f * k;
            float w = 560f * k;
            float x = cx - w * 0.5f;
            DrawOutlined(new Rect(x, top + bob, w, 110f * k), "TUK-TUK", titleStyle, Yellow, k);
            DrawOutlined(new Rect(x, top + 96f * k + bob, w, 130f * k), "RACE", titleStyle, Red, k);

            GUI.color = new Color(0.12f, 0.06f, 0.02f, 0.85f);
            GUI.Box(new Rect(cx - 210f * k, top + 230f * k, 420f * k, 34f * k), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(cx - 210f * k, top + 230f * k, 420f * k, 34f * k),
                      "Il tuk-tuk più veloce del Cairo", textStyle);
        }

        private void DrawMain(float left, float panelW, float top, float k)
        {
            float y = top + 290f * k;
            float bh = 70f * k;
            float gap = 16f * k;

            string recordText = record > 0f ? $"Record: {record:0} m" : "Record: ancora da battere";
            GUI.Label(new Rect(left, y, panelW, 36f * k), recordText, headingStyle);
            y += 52f * k;

            if (Button(new Rect(left, y, panelW, bh * 1.15f), "GIOCA", Orange, k)) StartRun();
            y += bh * 1.15f + gap;
            if (Button(new Rect(left, y, panelW, bh), "IMPOSTAZIONI", Teal, k)) page = Page.Settings;
            y += bh + gap;
            if (Button(new Rect(left, y, panelW, bh), "CREDITI", Plum, k)) page = Page.Credits;
        }

        private void DrawSettings(float left, float panelW, float top, float k)
        {
            float y = top + 290f * k;
            float bh = 70f * k;
            GUI.Label(new Rect(left, y, panelW, 44f * k), "IMPOSTAZIONI", headingStyle);
            y += 80f * k;
            string audio = SoundLibrary.Enabled ? "AUDIO: ON" : "AUDIO: OFF";
            if (Button(new Rect(left, y, panelW, bh), audio, Teal, k)) SoundLibrary.SetEnabled(!SoundLibrary.Enabled);
            y += bh + 40f * k;
            if (Button(new Rect(left, y, panelW, bh), "INDIETRO", new Color(0.3f, 0.3f, 0.32f), k)) page = Page.Main;
        }

        private void DrawCredits(float left, float panelW, float top, float k)
        {
            float y = top + 290f * k;
            float bh = 70f * k;
            GUI.Label(new Rect(left, y, panelW, 44f * k), "CREDITI", headingStyle);
            y += 60f * k;
            GUI.Label(new Rect(left, y, panelW, 30f * k), "Tuk-Tuk RACE", textStyle); y += 36f * k;
            GUI.Label(new Rect(left, y, panelW, 30f * k), "Ideato e sviluppato da Alberto", textStyle); y += 36f * k;
            GUI.Label(new Rect(left, y, panelW, 30f * k), "Motore: Unity 6, grafica cartoon", textStyle); y += 36f * k;
            GUI.Label(new Rect(left, y, panelW, 30f * k), "Suoni generati in codice", textStyle); y += 60f * k;
            if (Button(new Rect(left, y, panelW, bh), "INDIETRO", new Color(0.3f, 0.3f, 0.32f), k)) page = Page.Main;
        }

        private bool Button(Rect r, string label, Color color, float k)
        {
            GUI.backgroundColor = color;
            bool pressed = GUI.Button(r, label, buttonStyle);
            GUI.backgroundColor = Color.white;
            return pressed;
        }

        private void DrawOutlined(Rect r, string text, GUIStyle style, Color fill, float k)
        {
            var outline = new GUIStyle(style);
            outline.normal.textColor = Dark;
            float o = 5f * k;
            Vector2[] offsets = { new Vector2(-o, 0), new Vector2(o, 0), new Vector2(0, -o), new Vector2(0, o),
                                  new Vector2(-o, -o), new Vector2(o, -o), new Vector2(-o, o), new Vector2(o, o) };
            foreach (Vector2 off in offsets)
                GUI.Label(new Rect(r.x + off.x, r.y + off.y, r.width, r.height), text, outline);
            var face = new GUIStyle(style);
            face.normal.textColor = fill;
            GUI.Label(r, text, face);
        }

        private void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            float k = Screen.height / 800f;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(110f * k),
            };
            textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(24f * k),
                fontStyle = FontStyle.Bold,
            };
            textStyle.normal.textColor = Color.white;
            headingStyle = new GUIStyle(textStyle) { fontSize = Mathf.RoundToInt(34f * k) };
            headingStyle.normal.textColor = Yellow;
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(30f * k),
                fontStyle = FontStyle.Bold,
            };
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.white;
        }
    }
}
