#if UNITY_EDITOR
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
        const string FB_OFF_DEFINE = "DO_NOT_FACEBOOK_ADS_SDK";

        static bool IsQADefineEnabled()
        {
            string qa = Environment.GetEnvironmentVariable("QA");
            if (string.IsNullOrEmpty(qa))
                qa = GetArg("-QA") ?? GetArg("QA");

            return !string.IsNullOrEmpty(qa) &&
                (qa.Equals("true", StringComparison.OrdinalIgnoreCase) || qa.Equals("1"));
        }

        private static string GetArg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && args.Length > i + 1)
                {
                    return args[i + 1];
                }
            }
            return null;
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

        static void EnsureDefine(BuildTargetGroup group, string symbol)
        {
            WithBuildTargetGroup(group, () =>
            {
                if (DefineUtil.AddProjectDefineSymbol(symbol))
                {
                    Debug.Log($"[{group}] 필수 심볼 추가: {symbol}");
                }
            });
        }

        static void SetQASymbol(BuildTargetGroup group)
        {
            WithBuildTargetGroup(group, () =>
            {
                bool qaEnabled = IsQADefineEnabled();
                bool changed = qaEnabled
                    ? DefineUtil.AddProjectDefineSymbol(QA_DEFINE)
                    : DefineUtil.RemoveProjectDefineSymbol(QA_DEFINE);

                if (changed)
                {
                    Debug.Log($"[{group}] {QA_DEFINE} 심볼 {(qaEnabled ? "ON" : "OFF")}");
                }
            });
        }

        static int GetBuildNumberOrDefault()
        {
            string num = Environment.GetEnvironmentVariable("BUILD_NUMBER");
            return int.TryParse(num, out int n) ? n : 1;
        }

        [MenuItem("Tools/Build/AndroidAPK")]
        public static void PerformBuildAndroidAPK()
        {
            Debug.Log($"Build command args: {string.Join(" ", Environment.GetCommandLineArgs())}");
            Debug.Log($"Build command env: {string.Join(", ", Environment.GetEnvironmentVariables())}");

            string path = GetArg("-exportPath");

            string[] levels = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = levels,
                locationPathName = $"{path}",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            PlayerSettings.Android.bundleVersionCode = GetBuildNumberOrDefault();
            PlayerSettings.Android.useCustomKeystore = false;

            EnsureDefine(BuildTargetGroup.Android, FB_OFF_DEFINE);
            if (Application.isBatchMode) SetQASymbol(BuildTargetGroup.Android);

            bool prevAppBundle = EditorUserBuildSettings.buildAppBundle;
            EditorUserBuildSettings.buildAppBundle = false;

            BuildReport rpt = null;
            try
            {
                rpt = BuildPipeline.BuildPlayer(buildPlayerOptions);
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = prevAppBundle;
            }

            Debug.Log(rpt.summary.result == BuildResult.Succeeded
                ? $"[Android][APK] Build total Size: {rpt.summary.totalSize} bytes with path {buildPlayerOptions.locationPathName}"
                : $"[Android][APK] Build result: {rpt.summary.result}");

            Debug.Log($"Setting {QA_DEFINE} define: {IsQADefineEnabled()}");
        }

        [MenuItem("Tools/Build/AndroidAAB")]
        public static void PerformBuildAndroidAAB()
        {
            Debug.Log($"Build command args: {string.Join(" ", Environment.GetCommandLineArgs())}");
            Debug.Log($"Build command env: {string.Join(", ", Environment.GetEnvironmentVariables())}");
            
            string path = GetArg("-exportPath");

            string[] levels = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = levels,
                locationPathName = $"{path}",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            PlayerSettings.Android.bundleVersionCode = GetBuildNumberOrDefault();
            PlayerSettings.Android.useCustomKeystore = false;

            EnsureDefine(BuildTargetGroup.Android, FB_OFF_DEFINE);
            if (Application.isBatchMode) SetQASymbol(BuildTargetGroup.Android);

            bool prevAppBundle = EditorUserBuildSettings.buildAppBundle;
            EditorUserBuildSettings.buildAppBundle = true;

            BuildReport rpt = null;
            try
            {
                rpt = BuildPipeline.BuildPlayer(buildPlayerOptions);
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = prevAppBundle;
            }

            Debug.Log(rpt.summary.result == BuildResult.Succeeded
                ? $"[Android][AAB] Build total Size: {rpt.summary.totalSize} bytes with path {buildPlayerOptions.locationPathName}"
                : $"[Android][AAB] Build result: {rpt.summary.result}");

            Debug.Log($"Setting {QA_DEFINE} define: {IsQADefineEnabled()}");
        }
        [MenuItem("Tools/Build/iOS")]
        public static void PerformBuildiOS()
        {
            string path = "Build";
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

            EnsureDefine(BuildTargetGroup.iOS, FB_OFF_DEFINE);
            if (Application.isBatchMode) SetQASymbol(BuildTargetGroup.iOS);

            BuildReport rpt = BuildPipeline.BuildPlayer(buildPlayerOptions);

            Debug.Log(rpt.summary.result == BuildResult.Succeeded
                ? $"[iOS] Build total Size: {rpt.summary.totalSize} bytes with path {buildPlayerOptions.locationPathName}"
                : $"[iOS] Build result: {rpt.summary.result}");

            Debug.Log($"Setting {QA_DEFINE} define: {IsQADefineEnabled()}");
        }
    }
}

#endif