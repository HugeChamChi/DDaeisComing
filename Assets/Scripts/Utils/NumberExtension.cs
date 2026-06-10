using UnityEngine;

namespace Bathhouse.Utils
{
    public static class NumberExtension
    {
        /// <summary>
        /// 숫자를 1,000 단위 콤마(,)가 포함된 문자열로 변환합니다. (예: 18,000)
        /// </summary>
        public static string ToComma(this int value)
        {
            return value.ToString("N0");
        }

        /// <summary>
        /// 콤마(,)가 포함된 숫자 뒤에 명, 원 등의 단위를 붙여서 반환합니다. (예: 18,000명)
        /// </summary>
        public static string ToComma(this int value, string unit)
        {
            return $"{value:N0}{unit}";
        }

        public static string ToComma(this float value)
        {
            return value.ToString("N0");
        }

        public static string ToComma(this float value, string unit)
        {
            return $"{value:N0}{unit}";
        }
    }
}
