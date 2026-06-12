using UnityEngine;
using Bathhouse.Managers;
using UnityEngine.EventSystems;

namespace Bathhouse.Facilities
{
    public class DroppedTowel : MonoBehaviour, IPointerClickHandler
    {
        private bool isPlayingMinigame = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (GameManager.Interaction != null)
            {
                GameManager.Interaction.AddSatisfaction(5); // 성공 보상
            }

            // 타월 보관함 0개로 초기화
            TowelReturnFacility facility = FindFirstObjectByType<TowelReturnFacility>();
            if (facility != null)
            {
                facility.EmptyStorage();
            }

            Debug.Log("[DroppedTowel] 버려진 수건을 치웠습니다.");
            Destroy(gameObject);
        }
    }
}
