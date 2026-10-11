using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TukTuk.Editor
{
    /// <summary>Build di prova per Android: crea Builds/tuk-tuk-test.apk con le scene della Build Settings.</summary>
    public static class BuildTools
    {
        private const string PackageId = "com.tuktuk.race";

        [MenuItem("TukTuk/Build APK di prova")]
        public static void BuildAndroidTest()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string outDir = Path.Combine(root, "Builds");
            Directory.CreateDirectory(outDir);
            string path = Path.Combine(outDir, "tuk-tuk-test.apk");

            var scenes = new List<string>();
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.enabled) scenes.Add(s.path);

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Build APK: {summary.result} -> {path} ({summary.totalSize / (1024 * 1024)} MB, errori: {summary.totalErrors})");
        }
    }
}
