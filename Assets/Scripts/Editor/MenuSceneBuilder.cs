using TukTuk.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TukTuk.Editor
{
    /// <summary>Crea la scena di apertura (MainMenu) e imposta le scene nelle Build Settings.</summary>
    public static class MenuSceneBuilder
    {
        private const string MenuPath = "Assets/Scenes/MainMenu.unity";
        private const string GamePath = "Assets/Scenes/Prototype.unity";

        [MenuItem("TukTuk/Crea scena menu")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.8f, 0.95f);
            cam.fieldOfView = 50f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(1.4f, 2.6f, -7.5f);
            camGo.transform.LookAt(new Vector3(-1.4f, 1.1f, 2f));

            var sunGo = new GameObject("Sole");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var rootGo = new GameObject("MainMenu");
            var menu = rootGo.AddComponent<MainMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("toonMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TukToon.mat");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MenuPath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(GamePath, true),
            };

            Debug.Log("Scena menu creata: " + MenuPath);
        }
    }
}
