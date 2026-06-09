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
        private readonly NPCData _npcData;
        private int _currentStepIndex = 0;

        // [Inject] VContainer를 사용한다면 생성자 주입 활용
        public NPCRouteController(NPCRouteProfileSO routeProfile, NPCData npcData)
        {
            _routeProfile = routeProfile;
            _npcData = npcData;
            _currentStepIndex = 0;
        }

        public void ResetRoute()
        {
            _currentStepIndex = 0;
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
                    return step.structureType;
                }
                else
                {
                    // 선택 동선 판단 (기본 확률 + NPC 만족도 보정치)
                    // baseSatisfactionLevel이 0~1이라고 가정하고, 보정치를 계산합니다.
                    float satisfactionBonus = _npcData.baseSatisfactionLevel * 0.5f; // 임의 가중치 비율
                    float finalProbability = step.baseProbability + satisfactionBonus;
                    float diceRoll = Random.Range(0f, 1f);

                    if (diceRoll <= finalProbability)
                    {
                        return step.structureType;
                    }
                    // 실패 시 건너뛰고 다음 Step 평가
                }
            }

            // 모든 동선 완료 (퇴장 등)
            return FacilityType.None;
        }
    }
}
