using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace RaidS3Jamstorm.Editor
{
    public static class CiBuild
    {
        public static void BuildMacOSUniversal()
        {
            var outputPath = GetArgument("-outputPath");
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new InvalidOperationException("Missing -outputPath argument.");

            // Unity's architecture value 2 is Universal (x86_64 + ARM64).
            PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 2);

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var report = BuildPipeline.BuildPlayer(
                EditorBuildSettings.scenes,
                outputPath,
                BuildTarget.StandaloneOSX,
                BuildOptions.None);

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"macOS build failed: {report.summary.result}");
        }

        private static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }

            return null;
        }
    }
}
