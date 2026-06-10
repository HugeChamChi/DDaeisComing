using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bathhouse.Data
{
    [Serializable]
    public struct IncomeData
    {
        public int incomeAmount;
        public string resultText;
    }

    [CreateAssetMenu(fileName = "FacilityIncomeData", menuName = "Bathhouse/Facility Income Data")]
    public class FacilityIncomeDataSO : ScriptableObject
    {
        [System.Serializable]
        public struct FacilityIncomeEntry
        {
            public FacilityType facilityType;
            public IncomeData incomeData;
        }

        [SerializeField]
        private List<FacilityIncomeEntry> incomeEntries = new List<FacilityIncomeEntry>();

        private Dictionary<FacilityType, IncomeData> incomeDataMap;

        public void Initialize()
        {
            if (incomeDataMap != null) return;

            incomeDataMap = new Dictionary<FacilityType, IncomeData>();
            foreach (var entry in incomeEntries)
            {
                if (!incomeDataMap.ContainsKey(entry.facilityType))
                {
                    incomeDataMap.Add(entry.facilityType, entry.incomeData);
                }
            }
        }

        public IncomeData GetIncomeData(FacilityType type)
        {
            if (incomeDataMap == null)
            {
                Initialize();
            }

            if (incomeDataMap.TryGetValue(type, out IncomeData data))
            {
                return data;
            }
            return new IncomeData { incomeAmount = 0, resultText = type.ToString() };
        }
    }
}
