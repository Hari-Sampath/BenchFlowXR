#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BenchFlow.Editor
{
    /// <summary>Builds the current prototype as an Android APK configured for Meta Quest.</summary>
    internal static class QuestBuildMenu
    {
        const string OutputPath = "Builds/Quest/BenchFlowXR.apk";

        [MenuItem("BenchFlow/Build Quest APK")]
        static void BuildQuestApk()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("BenchFlow XR: Stop Play mode before building the Quest APK.");
                return;
            }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("BenchFlow XR: Android Build Support is not loaded. Restart Unity after installing it in Unity Hub.");
                return;
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("BenchFlow XR: Add the prototype scene to the Build Profile scene list first.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"BenchFlow XR: Quest APK built at {OutputPath} ({report.summary.totalSize / (1024f * 1024f):0.0} MB).");
            else
                Debug.LogError($"BenchFlow XR: Quest APK build {report.summary.result}: {report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings.");
        }
    }
}
#endif
