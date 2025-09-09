// CustomBuildScript.cs
// Jenkins Boolean?환경변수 "QA" 로 QA 전처리기 심벌을 켜거나 끄는 자동 빌드 스크립트
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
        /// Jenkins Boolean 파라미터 "QA" 값(true/false) 조회
        /// </summary>
        static bool IsQADefineEnabled()
        {
            string qa = Environment.GetEnvironmentVariable("QA");
            return !string.IsNullOrEmpty(qa) && (qa.Equals("true", StringComparison.OrdinalIgnoreCase) || qa.Equals("1"));
        }

        /// <summary>
        /// 기존 전처리기 문자열에서 특정 심벌을 추가 및 제거 후 새 문자열 반환
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
        /// QA 심벌을 설정하는 메소드
        /// </summary>
        static void SetQASymbol(BuildTargetGroup group)
        {
            string prevDefs = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            string newDefs  = MergeDefine(prevDefs, QA_DEFINE, IsQADefineEnabled());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, newDefs);
        }

        /// <summary>Jenkins 빌드 넘버 → int 로 변환 (없으면 1)</summary>
        static int GetBuildNumberOrDefault()
        {
            string num = Environment.GetEnvironmentVariable("BUILD_NUMBER");
            return int.TryParse(num, out int n) ? n : 1;
        }

        /*────────────────────────────────────────
         *  Android 빌드
         *────────────────────────────────────────*/
        [MenuItem("Tools/Build/Android")]
        public static void PerformBuildAndroid()
        {
            // 1) 아웃풋 폴더
            string path = "Builds/Android";
            Directory.CreateDirectory(path);

            // 2) 활성 씬 수집
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

            // 4) 번들 버전 코드
            PlayerSettings.Android.bundleVersionCode = GetBuildNumberOrDefault();

            // 5) 빌드 실행
            if(Application.isBatchMode) SetQASymbol(BuildTargetGroup.Android);
            BuildReport rpt = BuildPipeline.BuildPlayer(buildPlayerOptions);

            Debug.Log(rpt.summary.result == BuildResult.Succeeded
                      ? $"[Android] Build 성공: {rpt.summary.totalSize} bytes"
                      : $"[Android] Build 실패: {rpt.summary.result}");

            Debug.Log($"Setting {QA_DEFINE} define: {IsQADefineEnabled()}");
        }

        /*────────────────────────────────────────
         *  iOS 빌드
         *────────────────────────────────────────*/
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
                locationPathName = path, // Xcode 프로젝트 폴더
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            PlayerSettings.iOS.buildNumber = GetBuildNumberOrDefault().ToString();

            if (Application.isBatchMode) SetQASymbol(BuildTargetGroup.iOS);
            BuildReport rpt = BuildPipeline.BuildPlayer(buildPlayerOptions);

            Debug.Log(rpt.summary.result == BuildResult.Succeeded
                      ? $"[iOS] Build 성공: {rpt.summary.totalSize} bytes"
                      : $"[iOS] Build 실패: {rpt.summary.result}");
                      
            Debug.Log($"Setting {QA_DEFINE} define: {IsQADefineEnabled()}");
        }
    }
}
