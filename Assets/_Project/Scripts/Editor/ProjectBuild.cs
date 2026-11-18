using System;
using System.Linq;
using SecondCatch.Application;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SecondCatch.EditorTools
{
    /// <summary>Единая точка сборки четырёх сцен v2 для Windows и локальной проверки настроек.</summary>
    public static class ProjectBuild
    {
        [MenuItem("Second Catch/Validate scene list")]
        public static void ValidateScenes()
        {
            var expected = new[] { GameScenes.BOOT, GameScenes.MENU, GameScenes.PORT, GameScenes.SEA }.Select(GameScenes.Path).ToArray();
            var actual = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (!expected.SequenceEqual(actual)) throw new BuildFailedException("Expected Boot, MainMenu, HomePort, HomeShallows in that order.");
            foreach (string path in expected)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) throw new BuildFailedException("Missing scene: " + path);
        }
        public static void BuildWindows()
        {
            ValidateScenes();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = "Build/Windows/SecondCatch.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException(report.summary.result.ToString());
        }
    }
}
