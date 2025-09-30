using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

namespace HyperModule
{
    /// <summary>
    /// Unity 6+ 전제, iOS/Android/Windows(Standalone)만 고려하는 단순 Define 유틸리티.
    /// </summary>
    public static class DefineUtil
    {
        /// <summary>
        /// 현재 컴파일 타임 기준 플랫폼 플래그.
        /// </summary>
        public static readonly bool IsIOS =
#if UNITY_IOS
            true;
#else
            false;
#endif

        /// <summary>
        /// 현재 컴파일 타임 기준 플랫폼 플래그.
        /// </summary>
        public static readonly bool IsAndroid =
#if UNITY_ANDROID
            true;
#else
            false;
#endif

        /// <summary>
        /// 현재 컴파일 타임 기준 플랫폼 플래그(Windows Standalone).
        /// </summary>
        public static readonly bool IsWindows =
#if UNITY_STANDALONE_WIN
            true;
#else
            false;
#endif

        /// <summary>
        /// 사람이 읽기 쉬운 플랫폼 이름을 반환합니다.
        /// </summary>
        public static string PlatformName
        {
            get
            {
                if (IsIOS) return "iOS";
                if (IsAndroid) return "Android";
                if (IsWindows) return "Windows";
#if UNITY_EDITOR
                return "Editor";
#else
                return "Unknown";
#endif
            }
        }

        /// <summary>
        /// 현재 프로젝트/플랫폼에서 유효한 Define 심볼 목록을 반환합니다.
        /// - Unity 6+ API만 사용합니다.
        /// - iOS/Android/Windows Standalone만 포함합니다.
        /// </summary>
        public static string[] GetProjectDefineSymbols()
        {
#if UNITY_EDITOR
            var selectedGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var result = new HashSet<string>(StringComparer.Ordinal);

            // PlayerSettings에서 현재 선택된 BuildTargetGroup의 Define 문자열을 읽어옵니다.
            try
            {
                var named = NamedBuildTarget.FromBuildTargetGroup(selectedGroup);
                var defineString = PlayerSettings.GetScriptingDefineSymbols(named);
                if (!string.IsNullOrWhiteSpace(defineString))
                {
                    foreach (var s in defineString.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var t = s.Trim();
                        if (t.Length > 0) result.Add(t);
                    }
                }
            }
            catch (ArgumentException)
            {
                // 잘못된 BuildTargetGroup인 경우 빈 목록 유지
            }

            // 에디터 플래그 추가
            result.Add("UNITY_EDITOR");
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor: result.Add("UNITY_EDITOR_WIN"); break;
                case RuntimePlatform.OSXEditor: result.Add("UNITY_EDITOR_OSX"); break;
                case RuntimePlatform.LinuxEditor: result.Add("UNITY_EDITOR_LINUX"); break;
            }

            // 사용 플랫폼만 추가
            if (selectedGroup == BuildTargetGroup.Android) result.Add("UNITY_ANDROID");
            if (selectedGroup == BuildTargetGroup.iOS) result.Add("UNITY_IOS");
            if (selectedGroup == BuildTargetGroup.Standalone)
            {
                result.Add("UNITY_STANDALONE");
                result.Add("UNITY_STANDALONE_WIN");
            }

            return new List<string>(result).ToArray();
#else
            // 플레이어 빌드에서는 우리가 사용하는 플랫폼 심볼만 반환
            var list = new List<string>();
#if UNITY_IOS
            list.Add("UNITY_IOS");
#endif
#if UNITY_ANDROID
            list.Add("UNITY_ANDROID");
#endif
#if UNITY_STANDALONE_WIN
            list.Add("UNITY_STANDALONE");
            list.Add("UNITY_STANDALONE_WIN");
#endif
            return list.ToArray();
#endif
        }

        /// <summary>
        /// 지정한 Define 심볼이 현재 프로젝트 설정에 존재하는지 확인합니다.
        /// </summary>
        public static bool CheckDefineExist(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            var target = value.Trim();
            var defines = GetProjectDefineSymbols();

            for (int i = 0; i < defines.Length; i++)
            {
                if (string.Equals(defines[i], target, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        /// <summary>
        /// 프로젝트 Scripting Define Symbols에 심볼을 추가합니다.
        /// 이미 존재하면 false, 새로 추가되면 true를 반환합니다.
        /// </summary>
        public static bool AddProjectDefineSymbol(string symbol)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(symbol)) return false;
            symbol = symbol.Trim();

            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var named = NamedBuildTarget.FromBuildTargetGroup(group);
            var defineString = PlayerSettings.GetScriptingDefineSymbols(named);

            var tokens = new List<string>();
            if (!string.IsNullOrWhiteSpace(defineString))
            {
                var parts = defineString.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    var t = parts[i].Trim();
                    if (t.Length > 0) tokens.Add(t);
                }
            }

            if (tokens.Contains(symbol)) return false;
            tokens.Add(symbol);

            var joined = string.Join(";", tokens);
            PlayerSettings.SetScriptingDefineSymbols(named, joined);
            EditorUtility.RequestScriptReload();
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// 프로젝트 Scripting Define Symbols에서 지정한 심볼을 제거합니다.
        /// 존재하지 않으면 false, 제거되면 true를 반환합니다.
        /// </summary>
        public static bool RemoveProjectDefineSymbol(string symbol)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(symbol)) return false;
            symbol = symbol.Trim();

            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var named = NamedBuildTarget.FromBuildTargetGroup(group);
            var defineString = PlayerSettings.GetScriptingDefineSymbols(named);

            var tokens = new List<string>();
            if (!string.IsNullOrWhiteSpace(defineString))
            {
                var parts = defineString.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    var t = parts[i].Trim();
                    if (t.Length > 0) tokens.Add(t);
                }
            }

            int before = tokens.Count;
            tokens.RemoveAll(s => s == symbol);
            if (tokens.Count == before) return false;

            var joined = string.Join(";", tokens);
            PlayerSettings.SetScriptingDefineSymbols(named, joined);
            EditorUtility.RequestScriptReload();
            return true;
#else
            return false;
#endif
        }
    }
}
