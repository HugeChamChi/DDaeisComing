using UnityEngine;
using Bathhouse.Data;
// using Cysharp.Threading.Tasks; // 선호 기술 스택

namespace Bathhouse.NPC
{
    /// <summary>
    /// NPC의 동선을 제어하는 컨트롤러 클래스
    /// NPCRouteProfileSO의 Step들을 평가하여 다음 목적지를 결정합니다.
    /// </summary>
    public class NPCRouteController
    {
        private readonly NPCRouteProfileSO _routeProfile;
        private readonly NPC_Base _npcBase;
        private int _currentStepIndex = 0;
        private FacilityType _lastDeterminedFacility = FacilityType.None;
        private bool _hasVisitedBath = false;

        public NPCRouteController(NPCRouteProfileSO routeProfile, NPC_Base npcBase)
        {
            _routeProfile = routeProfile;
            _npcBase = npcBase;
            _currentStepIndex = 0;
            _lastDeterminedFacility = FacilityType.None;
            _hasVisitedBath = false;
        }

        public void ResetRoute()
        {
            _currentStepIndex = 0;
            _lastDeterminedFacility = FacilityType.None;
            _hasVisitedBath = false;
        }

        /// <summary>
        /// 다음 이동할 시설의 타입을 결정합니다.
        /// 필수 동선은 무조건 반환하며, 선택 동선은 만족도를 가중치로 하여 주사위를 굴립니다.
        /// </summary>
        public FacilityType DetermineNextFacility()
        {
            if (_routeProfile == null || _routeProfile.routeSteps == null || _routeProfile.routeSteps.Count == 0)
                return FacilityType.None;

            while (_currentStepIndex < _routeProfile.routeSteps.Count)
            {
                RouteStep step = _routeProfile.routeSteps[_currentStepIndex];
                _currentStepIndex++; 

                if (step.isMandatory)
                {
                    _lastDeterminedFacility = step.structureType;
                    if (step.structureType == FacilityType.Bath) _hasVisitedBath = true;
                    return step.structureType;
                }
                else
                {
                    if (step.structureType == FacilityType.ScrubArea && !_hasVisitedBath)
                    {
                        Debug.Log($"<color=#FFA500>[동선 규칙]</color> {_npcBase.Data.name} - Bath를 거치지 않아서 <b>{step.structureType}</b> 패스함.");
                        continue;
                    }

                    float satisfactionBonus = _npcBase.CurrentSatisfaction * _npcBase.Data.satisfactionProbabilityWeight; 
                    float finalProbability = step.baseProbability + satisfactionBonus;
                    float diceRoll = Random.Range(0f, 1f);

                    if (diceRoll <= finalProbability)
                    {
                        Debug.Log($"<color=#00FFFF>[동선 확률]</color> {_npcBase.Data.name} - {finalProbability * 100f:F1}% 확률로 <b>{step.structureType}</b> 이용 결정! (주사위 결과: {diceRoll * 100f:F1}%)");
                        _lastDeterminedFacility = step.structureType;
                        if (step.structureType == FacilityType.Bath) _hasVisitedBath = true;
                        return step.structureType;
                    }
                    else
                    {
                        Debug.Log($"<color=#FFA500>[동선 확률]</color> {_npcBase.Data.name} - {finalProbability * 100f:F1}% 확률이었으나 <b>{step.structureType}</b> 패스함. 다음 동선 탐색... (주사위 결과: {diceRoll * 100f:F1}%)");
                    }
                    // 실패 시 건너뛰고 다음 Step 평가
                }
            }

            // 모든 동선 완료 (퇴장 등)
            return FacilityType.None;
        }
    }
}
