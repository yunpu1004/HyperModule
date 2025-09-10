using System;

namespace HyperModule
{
    public static class StringFormatUtil
    {
        public static string FormatNumber(long number)
        {
            // 접미사 배열 정의 (더 큰 단위를 추가할 수 있습니다)
            string[] suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No" };
            double absNumber = Math.Abs((double)number);
            int suffixIndex = 0;

            // 숫자의 크기에 따라 적절한 접미사 선택
            while (absNumber >= 1000 && suffixIndex < suffixes.Length - 1)
            {
                absNumber /= 1000;
                suffixIndex++;
            }

            // 숫자가 1000 미만인 경우 소수부 없이 정수부만 표시
            if (suffixIndex == 0)
            {
                return number.ToString();
            }

            // 최대 네 자리 숫자를 유지하기 위한 소수점 자리수 결정
            string format;
            if (absNumber >= 100)
            {
                // 정수부가 3자리 이상인 경우 소수점 없음
                format = "0";
            }
            else if (absNumber >= 10)
            { 
                // 정수부가 2자리인 경우 소수점 이하 1자리
                format = "0.0";
            }
            else
            {
                // 정수부가 1자리인 경우 소수점 이하 2자리
                format = "0.00";
            }

            // 숫자 포맷팅
            string formattedNumber = absNumber.ToString(format);

            // 부호가 음수인 경우 원래 부호를 유지
            if (number < 0)
            {
                formattedNumber = "-" + formattedNumber;
            }

            // 접미사와 결합하여 최종 문자열 반환
            return formattedNumber + suffixes[suffixIndex];
        }
    }
}