using System.Collections.Generic;
using Bathhouse.Data;

namespace Bathhouse.Managers
{
    public class DailyRecordModel
    {
        public int totalNPCVisits { get; private set; }
        public Dictionary<FacilityType, int> facilityUsageCounts { get; private set; }
        public int totalIncome { get; private set; }

        public DailyRecordModel()
        {
            facilityUsageCounts = new Dictionary<FacilityType, int>();
        }

        public void AddVisit()
        {
            totalNPCVisits++;
        }

        public void AddFacilityUsage(FacilityType type)
        {
            if (facilityUsageCounts.ContainsKey(type))
            {
                facilityUsageCounts[type]++;
            }
            else
            {
                facilityUsageCounts.Add(type, 1);
            }
        }

        public void AddIncome(int amount)
        {
            totalIncome += amount;
        }

        public void Reset()
        {
            totalNPCVisits = 0;
            facilityUsageCounts.Clear();
            totalIncome = 0;
        }
    }
}
