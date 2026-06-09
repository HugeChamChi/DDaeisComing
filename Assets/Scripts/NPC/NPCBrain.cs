using UnityEngine;
using Bathhouse.Data;

namespace Bathhouse.NPC
{
    /// <summary>
    /// SRP: Responsible ONLY for decision making based on current status and constraints.
    /// </summary>
    public class NPCBrain
    {
        private NPCData _data;

        public bool hasVisitedCounter;
        public bool hasVisitedLocker;
        public bool hasBoughtItems;
        public bool hasShowered;
        public int bathCount;

        public const int MaxBathsBeforeExit = 3;

        public NPCBrain(NPCData data)
        {
            _data = data;
        }

        public void ResetMemory()
        {
            hasVisitedCounter = false;
            hasVisitedLocker = false;
            hasBoughtItems = false;
            hasShowered = false;
            bathCount = 0;
        }

        /// <summary>
        /// Determines the next facility the NPC must visit.
        /// </summary>
        public FacilityType DetermineNextFacility()
        {
            // 필수 입장 시퀀스: 들어오자마자 락커룸부터 간다 (결제는 나갈 때)
            if (!hasVisitedLocker) return FacilityType.LockerRoom;
            
            // 행동 횟수를 채웠다면 퇴장(카운터에서 정산)
            if (bathCount >= MaxBathsBeforeExit)
            {
                return FacilityType.Counter;
            }

            // 그 외에는 NPC 성향(가중치)에 따라 완전 자율 행동
            return ChooseBathActivity();
        }

        private FacilityType ChooseBathActivity()
        {
            int totalWeight = _data.weightShower + _data.weightBath + _data.weightSauna + _data.weightScrubArea + _data.weightBeverageDispenser;
            int rand = Random.Range(0, totalWeight);

            if (rand < _data.weightShower) return FacilityType.Shower;
            rand -= _data.weightShower;

            if (rand < _data.weightBath) return FacilityType.Bath;
            rand -= _data.weightBath;

            if (rand < _data.weightSauna) return FacilityType.Sauna;
            rand -= _data.weightSauna;

            if (rand < _data.weightBeverageDispenser) return FacilityType.BeverageDispenser;
            
            return FacilityType.ScrubArea;
        }
    }
}
