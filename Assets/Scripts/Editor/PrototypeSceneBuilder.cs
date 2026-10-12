using TukTuk.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TukTuk.EditorTools
{
    /// <summary>
    /// Menu: TukTuk > Crea scena prototipo.
    /// Crea materiali per ogni colore, assegna i modelli del kit Kenney (CC0) e salva la scena.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        private const string ToonMatPath = "Assets/Materials/TukToon.mat";
        private const string KenneyCar = "Assets/Art/Kenney/Car/";
        private const string KenneyCity = "Assets/Art/Kenney/City/";

        private static readonly string[] TreeFiles = { "tree-large", "tree-small" };
        private static readonly string[] CarFiles =
        {
            "taxi", "police", "sedan", "van", "suv", "delivery", "truck",
        };

        private static readonly Color Blue = new Color(0.10f, 0.45f, 0.95f);
        private static readonly Color Yellow = new Color(1f, 0.80f, 0.10f);
        private static readonly Color Dark = new Color(0.12f, 0.12f, 0.12f);
        private static readonly Color Seat = new Color(0.45f, 0.22f, 0.12f);
        private static readonly Color Skin = new Color(0.85f, 0.60f, 0.45f);
        private static readonly Color Shirt = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color Lamp = new Color(1f, 0.96f, 0.60f);
        private static readonly Color Red = new Color(0.90f, 0.20f, 0.20f);
        private static readonly Color Green = new Color(0.15f, 0.65f, 0.35f);

        private static Material toon;

        [MenuItem("TukTuk/Crea scena prototipo")]
        public static void Build()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Materials");
            toon = CreateToonMaterial();
            if (toon == null) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sunGo = new GameObject("Sole");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            // Tuk-tuk: modello cartoon a dimensioni reali, muso verso +Z
            var tuk = new GameObject("TukTuk");
            var controller = tuk.AddComponent<TukTukController>();
            var tukSo = new SerializedObject(controller);
            tukSo.FindProperty("toonMaterial").objectReferenceValue = toon;
            tukSo.ApplyModifiedPropertiesWithoutUndo();

            // Strada infinita con i modelli del kit Kenney
            var roadGo = new GameObject("StradaInfinita");
            var road = roadGo.AddComponent<EndlessRoad>();
            var roadSo = new SerializedObject(road);
            roadSo.FindProperty("player").objectReferenceValue = controller;
            roadSo.FindProperty("toonMaterial").objectReferenceValue = toon;
            AssignPrefabs(roadSo, "treePrefabs", KenneyCity, TreeFiles);
            AssignPrefabs(roadSo, "carPrefabs", KenneyCar, CarFiles);
            AppendPrefabFolder(roadSo, "carPrefabs", "Assets/Art/Generated/Cars");
            AssignBuildingPrefabs(roadSo);
            AssignPrefabFolder(roadSo, "pedestrianPrefabs", "Assets/Art/Generated/People");
            AssignPrefabFolder(roadSo, "cartPrefabs", "Assets/Art/Generated/Stalls");
            AssignPrefabFolder(roadSo, "motorbikePrefabs", "Assets/Art/Generated/Vehicles");
            roadSo.ApplyModifiedPropertiesWithoutUndo();

            // Camera dall'alto che segue il tuk-tuk
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 4.2f, -6.5f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.60f, 0.85f, 1.00f);
            var follow = camGo.AddComponent<CameraFollow>();
            var followSo = new SerializedObject(follow);
            followSo.FindProperty("target").objectReferenceValue = tuk.transform;
            followSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TukTuk] Scena prototipo aggiornata con modelli Kenney: " + ScenePath);
        }

        private static void AssignPrefabs(SerializedObject so, string property, string folder, string[] names)
        {
            SerializedProperty arr = so.FindProperty(property);
            arr.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + names[i] + ".fbx");
                if (prefab == null)
                    Debug.LogWarning("[TukTuk] Modello non trovato: " + folder + names[i] + ".fbx");
                arr.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
            }
        }

        /// <summary>Aggiunge in coda all'array i prefab della cartella, senza cancellare quelli già presenti.</summary>
        private static void AppendPrefabFolder(SerializedObject so, string property, string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            SerializedProperty arr = so.FindProperty(property);
            int start = arr.arraySize;
            arr.arraySize = start + guids.Length;
            for (int k = 0; k < guids.Length; k++)
                arr.GetArrayElementAtIndex(start + k).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[k]));
        }

        /// <summary>Assegna all'array indicato tutti i prefab presenti nella cartella.</summary>
        private static void AssignPrefabFolder(SerializedObject so, string property, string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            SerializedProperty arr = so.FindProperty(property);
            arr.arraySize = guids.Length;
            for (int k = 0; k < guids.Length; k++)
                arr.GetArrayElementAtIndex(k).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[k]));
        }

        private static void AssignBuildingPrefabs(SerializedObject so)
        {
            string[] districts = { "Giza", "Residenziale", "Downtown", "Islamico", "Zamalek" };
            SerializedProperty arr = so.FindProperty("buildingPrefabs");
            arr.arraySize = 30;
            int i = 0;
            foreach (string d in districts)
            {
                for (int v = 1; v <= 6; v++, i++)
                {
                    string path = "Assets/Art/Generated/Buildings/Edificio_" + d + "_" + v + ".prefab";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) Debug.LogWarning("[TukTuk] Edificio non trovato: " + path + ". Esegui TukTuk > Genera modelli 3D.");
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
                }
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static Material CreateToonMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ToonMatPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("TukTuk/Toon");
                if (shader == null)
                {
                    Debug.LogError("[TukTuk] Shader 'TukTuk/Toon' non trovato. Controlla Assets/Shaders/TukTukToon.shader.");
                    return null;
                }
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, ToonMatPath);
            }
            return mat;
        }

        private static Material ColorMaterial(Color color)
        {
            string path = "Assets/Materials/Toon_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(toon);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type,
                                       Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = ColorMaterial(color);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Wheel(Transform parent, Vector3 localPos)
        {
            var go = Part(parent, "Ruota", PrimitiveType.Cylinder, localPos, new Vector3(0.7f, 0.1f, 0.7f), Dark);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static void AddToBuildSettings(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == path))
            {
                list.Insert(0, new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = list.ToArray();
            }
        }
    }
}
