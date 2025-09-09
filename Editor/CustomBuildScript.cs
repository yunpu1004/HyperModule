// CustomBuildScript.cs
// Jenkins Boolean?ȯ�溯�� "QA" �� QA ��ó���� �ɹ��� �Ѱų� ���� �ڵ� ���� ��ũ��Ʈ
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Jenkins Boolean �Ķ���� "QA" ��(true/false) ��ȸ
        /// </summary>
        static bool IsQADefineEnabled()
        {
            string qa = Environment.GetEnvironmentVariable("QA");
            return !string.IsNullOrEmpty(qa) && (qa.Equals("true", StringComparison.OrdinalIgnoreCase) || qa.Equals("1"));
        }

        /// <summary>
        /// ���� ��ó���� ���ڿ����� Ư�� �ɹ��� �߰� �� ���� �� �� ���ڿ� ��ȯ
        /// </summary>
        static string MergeDefine(string original, string symbol, bool enable)
        {
            var set = new HashSet<string>(original.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)));

            if (enable)
                set.Add(symbol);
            else
                set.Remove(symbol);

            return string.Join(";", set);
        }

        /// <summary>
        /// QA �ɹ��� �����ϴ� �޼ҵ�
        /// </summary>
        static void SetQASymbol(BuildTargetGroup group)
        {
            string prevDefs = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            string newDefs  = MergeDefine(prevDefs, QA_DEFINE, IsQADefineEnabled());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, newDefs);
        }

        /// <summary>Jenkins ���� �ѹ� �� int �� ��ȯ (������ 1)</summary>
        static int GetBuildNumberOrDefault()
        {
            string num = Environment.GetEnvironmentVariable("BUILD_NUMBER");
            return int.TryParse(num, out int n) ? n : 1;
        }

        /*��������������������������������������������������������������������������������
         *  Android ����
         *��������������������������������������������������������������������������������*/
        [MenuItem("Tools/Build/Android")]
        public static void PerformBuildAndroid()
        {
            // 1) �ƿ�ǲ ����
            string path = "Builds/Android";
            Directory.CreateDirectory(path);

            // 2) Ȱ�� �� ����
            string[] levels = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            // 3) BuildPlayerOptions
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = levels,
                locationPathName = $"{path}/BuiltGame.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            // 4) ���� ���� �ڵ�
            PlayerSettings.Android.bundleVersionCode = GetBuildNumberOrDefault();

            // 5) ���� ����
            if(Application.isBatchMode) SetQASymbol(BuildTargetGroup.Android);
            BuildReport rpt = BuildPipeline.BuildPlayer(buildPlayerOptions);

            Debug.Log(rpt.summary.result == BuildResult.Succeeded
                      ? $"[Android] Build ����: {rpt.summary.totalSize} bytes"
                      : $"[Android] Build ����: {rpt.summary.result}");

            Debug.Log($"Setting {QA_DEFINE} define: {IsQADefineEnabled()}");
        }

        /*��������������������������������������������������������������������������������
         *  iOS ����
         *��������������������������������������������������������������������������������*/
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
                locationPathName = path, // Xcode ������Ʈ ����
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            PlayerSettings.iOS.buildNumber = GetBuildNumberOrDefault().ToString();

            if (Application.isBatchMode) SetQASymbol(BuildTargetGroup.iOS);
            BuildReport rpt = BuildPipeline.BuildPlayer(buildPlayerOptions);

            Debug.Log(rpt.summary.result == BuildResult.Succeeded
                      ? $"[iOS] Build ����: {rpt.summary.totalSize} bytes"
                      : $"[iOS] Build ����: {rpt.summary.result}");
                      
            Debug.Log($"Setting {QA_DEFINE} define: {IsQADefineEnabled()}");
        }
    }
}
