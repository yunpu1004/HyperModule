using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ExcelDataReader;


namespace HyperModule
{
    public class ExcelDictionaryLoader
    {
        // .xlsx 파일 호환을 위해 한 번만 등록
        static ExcelDictionaryLoader() 
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Resources 폴더 안의 .bytes 확장자를 가진 엑셀 파일을 읽어 Dictionary&lt;string, Dictionary&lt;string,string&gt;&gt; 형태로 반환합니다.
        /// </summary>
        /// <remarks>
        /// 딕셔너리의 키값은 엑셀 파일의 첫 번째 열에서 가져오고, 내부 딕셔너리의 키값은 첫 번째 행에서 가져옵니다.
        /// </remarks>
        public static Dictionary<string, Dictionary<string, string>> GetDictionary(string resourcePath)
        {
            byte[] bytes = LoadExcelBytes(resourcePath);

            using var stream = new MemoryStream(bytes);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            using var dataSet = reader.AsDataSet();

            return BuildDictionary(dataSet.Tables[0]);
        }

        /// <summary>.bytes 확장자를 가진 엑셀 파일 가져오기</summary>
        private static byte[] LoadExcelBytes(string path)
        {
            string resPath = path.Replace('\\', '/').Replace(".bytes", string.Empty);

            const string resourcesToken = "/Resources/";
            int resourcesIndex = resPath.IndexOf(resourcesToken, System.StringComparison.OrdinalIgnoreCase);
            if (resourcesIndex >= 0) resPath = resPath.Substring(resourcesIndex + resourcesToken.Length);
            else if (resPath.StartsWith("Resources/", System.StringComparison.OrdinalIgnoreCase)) resPath = resPath.Substring("Resources/".Length);

            TextAsset ta = Resources.Load<TextAsset>(resPath);
            if (ta == null) throw new FileNotFoundException($"Resources 경로 \"{resPath}\"(으)로부터 .xlsx(TextAsset)을 찾을 수 없습니다.");

            return ta.bytes;
        }

        /// <summary>DataTable을 사양에 맞춰 Dictionary로 변환</summary>
        private static Dictionary<string, Dictionary<string, string>> BuildDictionary(DataTable table)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(table.Rows.Count - 1);
            var innerKeys = new List<string>();

            /* 1행: 내부 키 수집 --------------------------------------------- */
            for (int c = 1; c < table.Columns.Count; ++c)
            {
                string key = table.Rows[0][c]?.ToString()?.Trim();
                innerKeys.Add(key);
            }

            /* 2행~: 외부 키 + 실제 문자열 ----------------------------------- */
            for (int r = 1; r < table.Rows.Count; ++r)
            {
                string outerKey = table.Rows[r][0]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(outerKey)) continue;

                var innerDict = new Dictionary<string, string>(innerKeys.Count);

                for (int c = 1; c < table.Columns.Count; ++c)
                {
                    string innerKey = innerKeys[c - 1];
                    if (string.IsNullOrEmpty(innerKey)) continue;

                    string raw = table.Rows[r][c]?.ToString() ?? string.Empty;
                    innerDict[innerKey] = raw;
                }
                result[outerKey] = innerDict;
            }
            return result;
        }
    }
}

