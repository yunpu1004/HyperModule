using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HyperModule
{
    public static class StringExtension
    {
        /// <summary>
        /// 문자열 안의 중괄호로 둘러쌓인 {태그}를 찾아 순서를 유지한 번호 자리로 바꾸고,
        /// 태그 이름 배열과 태그 존재 여부(true/false)를 함께 돌려준다.
        /// </summary>
        public static bool GetWrappedText(this string source, out string format, out string[] tags)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // 태그 → 자리 번호 매핑용 딕셔너리
            var indexMap = new Dictionary<string, int>();
            var sb = new StringBuilder();
            int last = 0;

            // {something} 형태만 찾는다. (중첩·이스케이프 처리는 요구 사항에 없으므로 단순 처리)
            foreach (Match m in Regex.Matches(source, @"\{([^{}]+)\}"))
            {
                // 중괄호 앞의 일반 텍스트 그대로 복사
                sb.Append(source, last, m.Index - last);

                string tagName = m.Groups[1].Value;

                // 처음 본 태그면 새 번호 부여
                if (!indexMap.TryGetValue(tagName, out int idx))
                {
                    idx = indexMap.Count;
                    indexMap[tagName] = idx;
                }

                // {0}, {1} … 로 치환
                sb.Append('{').Append(idx).Append('}');
                last = m.Index + m.Length;
            }

            // 마지막 태그 뒤에 남은 일반 텍스트 붙이기
            sb.Append(source, last, source.Length - last);

            format = sb.ToString();
            tags = indexMap.OrderBy(kv => kv.Value).Select(kv => kv.Key).ToArray(); // 등장 순서 유지

            return indexMap.Count > 0;   // 태그가 하나라도 있었는지 true/false
        }
    }
}