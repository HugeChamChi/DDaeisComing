using UnityEngine;

namespace Bathhouse.Data
{
    [CreateAssetMenu(fileName = "RentData", menuName = "Bathhouse/Data/RentData")]
    public class RentDataSO : ScriptableObject
    {
        [Tooltip("월세 납부 주기 (일)")]
        public int rentCycleDays = 3;

        [Tooltip("인덱스 0이 1번째 납부(3일차), 인덱스 1이 2번째 납부(6일차) ...")]
        public int[] rentAmounts = new int[] 
        { 
            7000, 10000, 13000, 16000, 19000, 22000, 26000, 30000, 34000, 40000 
        };

        /// <summary>
        /// 해당 날짜가 월세 납부일인지 확인합니다.
        /// </summary>
        public bool IsRentDay(int day)
        {
            return day % rentCycleDays == 0;
        }

        /// <summary>
        /// 해당 날짜의 월세 금액을 반환합니다.
        /// </summary>
        public int GetRentAmount(int day)
        {
            if (!IsRentDay(day)) return 0;
            
            int cycleIndex = (day / rentCycleDays) - 1;
            if (cycleIndex < 0) return 0;
            
            // 배열 길이를 넘어서는 경우 마지막 금액 유지
            if (cycleIndex >= rentAmounts.Length) 
            {
                return rentAmounts[rentAmounts.Length - 1];
            }
            
            return rentAmounts[cycleIndex];
        }
    }
}
