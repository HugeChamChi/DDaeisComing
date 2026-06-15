using System.Collections.Generic;
using System;

namespace Bathhouse.Data
{
    [Serializable]
    public class DailyUsageStat
    {
        public string facilityType;
        public int usageCount;
        
        public DailyUsageStat(string type, int count)
        {
            facilityType = type;
            usageCount = count;
        }
    }

    /// <summary>
    /// 게임 전체의 플레이 상태를 저장하는 런타임 데이터 클래스입니다.
    /// Json 직렬화 및 로컬 저장을 위해 사용됩니다.
    /// </summary>
    [Serializable]
    public class GameData
    {
        public int currentDay;
        public int currentGold; // 누적 자산 (보유 골드)
        
        // 당일 정산용 데이터 (하루가 넘어갈 때마다 초기화됨)
        public int todayIncome;
        public int todayExpense;

        // 당일 정산용 통계 데이터
        public int todayCustomers;
        [UnityEngine.SerializeField]
        private List<DailyUsageStat> _todayFacilityUsages;

        // 통계용 누적 데이터
        public int totalCustomersServed;
        public int totalFacilitiesBuilt;

        // 상점 및 업그레이드 세이브 데이터
        public ShopSaveData shopData;

        // JSON 직렬화를 위한 빈 생성자
        public GameData()
        {
            currentDay = 1;
            currentGold = 1000; // 초기 자본
            
            todayIncome = 0;
            todayExpense = 0;
            todayCustomers = 0;
            _todayFacilityUsages = new List<DailyUsageStat>();
            
            totalCustomersServed = 0;
            totalFacilitiesBuilt = 0;

            shopData = new ShopSaveData();
        }

        /// <summary>
        /// 당일 순이익을 계산합니다.
        /// </summary>
        public int GetTodayNetProfit()
        {
            // 수익 - 지출
            return todayIncome - todayExpense;
        }

        /// <summary>
        /// 당일 적자 여부를 확인합니다. (이 값이 true면 폐업 처리)
        /// </summary>
        public bool IsDeficitToday()
        {
            return GetTodayNetProfit() < 0;
        }

        /// <summary>
        /// 당일 순이익을 누적 자산에 반영합니다.
        /// </summary>
        public void ApplyTodayProfit()
        {
            currentGold += GetTodayNetProfit();
        }

        /// <summary>
        /// 다음 날로 넘어갈 때 호출하여 정산 및 Day 증가 처리를 합니다.
        /// </summary>
        public void AdvanceToNextDay()
        {
            // 다음 날짜로 변경
            currentDay++;

            // 당일 정산 기록 초기화
            todayIncome = 0;
            todayExpense = 0;
            todayCustomers = 0;
            _todayFacilityUsages.Clear();
        }

        /// <summary>
        /// 당일 손님이 입장했을 때 호출합니다. (누적 데이터도 함께 증가)
        /// </summary>
        public void AddCustomerCount()
        {
            todayCustomers++;
            totalCustomersServed++;
        }

        /// <summary>
        /// 구조물(시설)을 이용했을 때 호출하여 당일 이용 횟수를 기록합니다.
        /// (예: AddFacilityUsage(FacilityType.Sauna.ToString()))
        /// </summary>
        public void AddFacilityUsage(string facilityName)
        {
            if (_todayFacilityUsages == null)
            {
                _todayFacilityUsages = new List<DailyUsageStat>();
            }

            var stat = _todayFacilityUsages.Find(x => x.facilityType == facilityName);
            if (stat != null)
            {
                stat.usageCount++;
            }
            else
            {
                _todayFacilityUsages.Add(new DailyUsageStat(facilityName, 1));
            }
        }
        
        /// <summary>
        /// 정산 UI 등에서 당일 시설 이용 통계 리스트를 가져올 때 사용합니다.
        /// </summary>
        public List<DailyUsageStat> GetTodayFacilityUsages()
        {
            return _todayFacilityUsages;
        }
    }
}
