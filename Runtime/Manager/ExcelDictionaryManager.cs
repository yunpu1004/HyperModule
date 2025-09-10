using System.Collections.Generic;
using UnityEngine;

namespace HyperModule
{
    /// <summary>
    /// 리소스 폴더 내부의 Excel 파일을 읽어 Dictionary 형태로 저장하고,
    /// 다른 스크립트에서 쉽게 접근할 수 있도록 하는 MonoBehaviour 클래스입니다.
    /// </summary>
    public class ExcelDictionaryManager : MonoBehaviour
    {
        [Header("리소스 폴더 내의 엑셀 파일 경로 목록입니다. \n엑셀 파일의 확장자는 .bytes 로 설정해주세요.")]
        [SerializeField] private List<string> excelFilePaths;
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> excelDictionary = new();

        private void Awake()
        {
            LoadExcelDictionaries();
        }

        private void LoadExcelDictionaries()
        {
            if (excelFilePaths == null || excelFilePaths.Count == 0) return;

            foreach (var filePath in excelFilePaths)
            {
                var dict = ExcelDictionaryLoader.GetDictionary(filePath);
                excelDictionary[filePath] = dict;
            }
        }

        public static bool TryGetDictionary(string filePath, out Dictionary<string, Dictionary<string, string>> dictionary)
        {
            if (excelDictionary.Count == 0)
            {
                QAUtil.LogWarning("Excel dictionaries not loaded. Ensure ExcelDictionaryStorage is initialized or path is correct.");
                dictionary = null;
                return false;
            }

            return excelDictionary.TryGetValue(filePath, out dictionary);
        }
    }
}