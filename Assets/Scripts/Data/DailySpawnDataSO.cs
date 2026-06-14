using UnityEngine;

namespace Bathhouse.Data
{
    [CreateAssetMenu(fileName = "DailySpawnData", menuName = "Bathhouse/DailySpawnData")]
    public class DailySpawnDataSO : ScriptableObject
    {
        [Header("Daily Customer Counts")]
        [Tooltip("인덱스 0이 Day 1입니다. 설정된 배열 이후의 일차는 마지막 값을 계속 사용합니다.")]
        public int[] dailyCustomerCounts = new int[]
        {
            15, 16, 17, // 1~3일
            18, 19, 20, // 4~6일
            22, 24, 26, // 7~9일
            28, 30, 32, // 10~12일
            34, 36, 38, // 13~15일
            40, 42, 44, // 16~18일
            46, 48, 50  // 19~21일 (50 캡)
        };

        /// <summary>
        /// 해당 일차의 손님 수를 반환합니다.
        /// </summary>
        /// <param name="day">1부터 시작하는 일차</param>
        public int GetCustomerCount(int day)
        {
            if (day <= 0) day = 1;
            
            int index = day - 1;
            if (dailyCustomerCounts == null || dailyCustomerCounts.Length == 0)
            {
                return 15; // 기본값
            }

            if (index >= dailyCustomerCounts.Length)
            {
                return dailyCustomerCounts[dailyCustomerCounts.Length - 1];
            }

            return dailyCustomerCounts[index];
        }
    }
}
