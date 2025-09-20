using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperModule
{
    /// <summary>
    /// Resources 폴더의 Excel(.bytes) 파일들을 읽어 Dictionary 캐시에 보관하는 정적 매니저입니다.
    /// </summary>
    public static class ExcelDictionaryManager
    {
        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> excelDictionary = new();

        public static LoadState loadState { get; private set; } = LoadState.Unloaded;

        /// <summary>
        /// ProjectSettings(Settings/ProjectSettings)의 excelFilePaths를 사용하여 초기화합니다.
        /// </summary>
        public static void Init()
        {
            if (loadState == LoadState.Loading)
            {
                QAUtil.LogWarning("[ExcelDictionaryManager] Init already in progress.");
                return;
            }

            loadState = LoadState.Loading;
            excelDictionary.Clear();

            try
            {
                var settings = Resources.Load<ProjectSettings>("Settings/ProjectSettings");
                if (settings == null)
                {
                    QAUtil.LogError("[ExcelDictionaryManager] ProjectSettings is not found in Resources/Settings/");
                    loadState = LoadState.Unloaded;
                    return;
                }

                var paths = settings.excelFilePaths?
                    .Select(s => s?.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToArray();

                if (paths == null || paths.Length == 0)
                {
                    return;
                }

                int loadedCount = 0;
                foreach (var path in paths)
                {
                    try
                    {
                        var dict = ExcelDictionaryLoader.GetDictionary(path);
                        if (dict != null)
                        {
                            excelDictionary[path] = dict;
                            loadedCount++;
                        }
                    }
                    catch (Exception e)
                    {
                        QAUtil.LogWarning($"[ExcelDictionaryManager] Failed to load '{path}': {e.Message}");
                    }
                }

                loadState = loadedCount > 0 ? LoadState.Loaded : LoadState.Unloaded;
                if (loadedCount > 0)
                    QAUtil.Log($"[ExcelDictionaryManager] Loaded {loadedCount} dictionaries.");
                else
                    QAUtil.LogWarning("[ExcelDictionaryManager] No excel dictionaries loaded.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                loadState = LoadState.Unloaded;
            }
        }

        public static bool TryGetDictionary(string filePath, out Dictionary<string, Dictionary<string, string>> dictionary)
        {
            if (excelDictionary.Count == 0)
            {
                QAUtil.LogWarning("Excel dictionaries not loaded. Ensure ExcelDictionaryManager is initialized or path is correct.");
                dictionary = null;
                return false;
            }

            return excelDictionary.TryGetValue(filePath, out dictionary);
        }

        public static void Clear()
        {
            excelDictionary.Clear();
            loadState = LoadState.Unloaded;
        }
    }
}
