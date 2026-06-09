using UnityEngine;
using Bathhouse.Data;
using Bathhouse.Utils;

namespace Bathhouse.Managers
{
    /// <summary>
    /// 게임 내 상호작용 설정(SO 데이터 등)과 글로벌 만족도를 중앙에서 관리하는 매니저
    /// </summary>
    public class InteractionManager : MonoSingleton_NotDontDestroyOnLoad<InteractionManager>
    {
        [Header("Interaction Settings")]
        [SerializeField] private InteractionRewardSO _globalRewardData;
        [SerializeField] private Canvas _bubbleCanvas; // 통합 말풍선 캔버스
        [SerializeField] private Bathhouse.UI.NPCInteractionSpeechBubbleUI _speechBubblePrefab;
        [SerializeField] private Vector3 _bubbleOffset = new Vector3(0, 1.5f, 0);
        
        [Header("Global Satisfaction")]
        [SerializeField] private int _globalSatisfaction = 0;

        private System.Collections.Generic.Queue<Bathhouse.UI.NPCInteractionSpeechBubbleUI> _bubblePool = new System.Collections.Generic.Queue<Bathhouse.UI.NPCInteractionSpeechBubbleUI>();

        public InteractionRewardSO GlobalRewardData => _globalRewardData;
        public int GlobalSatisfaction => _globalSatisfaction;

        protected override void Init()
        {
            base.Init();
            
            // 인스펙터에서 할당되지 않았다면 Resources에서 로드 시도
            if (_globalRewardData == null)
            {
                _globalRewardData = Resources.Load<InteractionRewardSO>("InteractionRewardData");
                if (_globalRewardData == null)
                {
                    Debug.LogWarning("[InteractionManager] InteractionRewardSO가 연결되지 않았습니다. 인스펙터에서 할당하거나 Resources/InteractionRewardData 경로에 에셋을 생성해주세요.");
                }
            }
        }

        /// <summary>
        /// 글로벌 만족도 수치를 추가/차감합니다.
        /// </summary>
        public void AddSatisfaction(int amount)
        {
            _globalSatisfaction += amount;
            // TODO: UI 업데이트 이벤트 등을 여기서 발생시킬 수 있습니다.
            Debug.Log($"[InteractionManager] 글로벌 만족도 변경: {amount} (현재 총합: {_globalSatisfaction})");
        }

        public Bathhouse.UI.NPCInteractionSpeechBubbleUI GetBubble(Transform targetTransform, System.Action onClick, Bathhouse.Data.InteractionBubbleType type = Bathhouse.Data.InteractionBubbleType.Counter)
        {
            if (_speechBubblePrefab == null || _bubbleCanvas == null)
            {
                Debug.LogWarning("[InteractionManager] SpeechBubblePrefab 또는 BubbleCanvas가 설정되지 않았습니다.");
                return null;
            }

            Bathhouse.UI.NPCInteractionSpeechBubbleUI bubble;
            if (_bubblePool.Count > 0)
            {
                bubble = _bubblePool.Dequeue();
            }
            else
            {
                bubble = Instantiate(_speechBubblePrefab);
            }

            bubble.transform.SetParent(_bubbleCanvas.transform, false);
            Sprite icon = _globalRewardData != null ? _globalRewardData.GetIcon(type) : null;
            bubble.Init(targetTransform, _bubbleOffset, onClick, icon);
            bubble.Show();
            return bubble;
        }

        public void ReturnBubble(Bathhouse.UI.NPCInteractionSpeechBubbleUI bubble)
        {
            if (bubble == null) return;
            
            if (!bubble.gameObject.activeSelf) return; // 이미 비활성화되어 풀에 들어갔다면 무시
            
            bubble.Hide();
            // 재사용 대기 상태일 때도 캔버스 자식으로 유지
            _bubblePool.Enqueue(bubble);
        }
    }
}
