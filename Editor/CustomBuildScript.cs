using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HyperModule
{
    public static class CustomBuildScript
    {
        const string QA_DEFINE = "QA";
        static bool IsQADefineEnabled()
        {
            string qa = Environment.GetEnvironmentVariable("QA");
            return !string.IsNullOrEmpty(qa) && (qa.Equals("true", StringComparison.OrdinalIgnoreCase) || qa.Equals("1"));
        }
        static void WithBuildTargetGroup(BuildTargetGroup group, Action action)
        {
            if (action == null) return;

            var previousGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (previousGroup == group)
            {
                action();
                return;
            }

            try
            {
                EditorUserBuildSettings.selectedBuildTargetGroup = group;
                action();
            }
            finally
            {
                EditorUserBuildSettings.selectedBuildTargetGroup = previousGroup;
            }
        }

        static void SetQASymbol(BuildTargetGroup group)
        {
            WithBuildTargetGroup(group, () =>
            {
                if (IsQADefineEnabled())
                    DefineUtil.AddProjectDefineSymbol(QA_DEFINE);
                else
                    DefineUtil.RemoveProjectDefineSymbol(QA_DEFINE);
            });
        }
        static int GetBuildNumberOrDefault()
        {
            string num = Environment.GetEnvironmentVariable("BUILD_NUMBER");
            return int.TryParse(num, out int n) ? n : 1;
        }
        [MenuItem("Tools/Build/Android")]
        public static void PerformBuildAndroid()
        {
            string path = "Builds/Android";
            Directory.CreateDirectory(path);

            string[] levels = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = levels,
                locationPathName = $"{path}/BuiltGame.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            PlayerSettings.Android.bundleVersionCode = GetBuildNumberOrDefault();

            if (Application.isBatchMode) SetQASymbol(BuildTargetGroup.Android);
            BuildReport rpt = BuildPipeline.BuildPlayer(buildPlayerOptions);

            Debug.Log(rpt.summary.result == BuildResult.Succeeded
                      ? $"[Android] Build ¼º°ø: {rpt.summary.totalSize} bytes"
                      : $"[Android] Build ½ÇÆÐ: {rpt.summary.result}");

            Debug.Log($"Setting {QA_DEFINE} define: {IsQADefineEnabled()}");
        }

        [MenuItem("Tools/Build/iOS")]
        public static void PerformBuildiOS()
        {
            string path = "Builds/iOS";
            Directory.CreateDirectory(path);

            string[] levels = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = levels,
                locationPathName = path,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            PlayerSettings.iOS.buildNumber = GetBuildNumberOrDefault().ToString();

            if (Application.isBatchMode) SetQASymbol(BuildTargetGroup.iOS);
            BuildReport rpt = BuildPipeline.BuildPlayer(buildPlayerOptions);

            Debug.Log(rpt.summary.result == BuildResult.Succeeded
                      ? $"[iOS] Build ¼º°ø: {rpt.summary.totalSize} bytes"
                      : $"[iOS] Build ½ÇÆÐ: {rpt.summary.result}");
                      
            Debug.Log($"Setting {QA_DEFINE} define: {IsQADefineEnabled()}");
        }
    }
}
