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
        public Sprite icon;
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

        public IReadOnlyList<FacilityIncomeEntry> Entries => incomeEntries;

        private Dictionary<FacilityType, IncomeData> incomeDataMap;

        private void OnEnable()
        {
            Initialize();
        }

        private void OnValidate()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (incomeDataMap == null)
            {
                incomeDataMap = new Dictionary<FacilityType, IncomeData>();
            }
            else
            {
                incomeDataMap.Clear();
            }

            foreach (var entry in incomeEntries)
            {
                if (!incomeDataMap.ContainsKey(entry.facilityType))
                {
                    incomeDataMap.Add(entry.facilityType, entry.incomeData);
                }
            }
        }

        public bool HasIncomeData(FacilityType type)
        {
            if (incomeDataMap == null || incomeDataMap.Count == 0)
            {
                Initialize();
            }
            return incomeDataMap.ContainsKey(type);
        }

        public IncomeData GetIncomeData(FacilityType type)
        {
            if (incomeDataMap == null || incomeDataMap.Count == 0)
            {
                Initialize();
            }

            if (incomeDataMap.TryGetValue(type, out IncomeData data))
            {
                return data;
            }
            return new IncomeData { incomeAmount = 0, resultText = type.ToString(), icon = null };
        }
    }
}
