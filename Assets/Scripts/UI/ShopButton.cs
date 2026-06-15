using UnityEngine;
using UnityEngine.UI;

namespace Bathhouse.UI
{
    /// <summary>
    /// 메인 화면 등에서 상점 패널을 여는 버튼 동작을 제어합니다.
    /// MVP 및 단일 책임 원칙에 따라 UI 활성화 역할만 수행합니다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ShopButton : MonoBehaviour
    {
        [Header("Target UI")]
        [Tooltip("활성화할 ShopUI 게임 오브젝트")]
        [SerializeField] private GameObject shopUIObject;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnShopButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnShopButtonClicked);
            }
        }

        private void OnShopButtonClicked()
        {
            if (shopUIObject != null)
            {
                // 상점이 열릴 때 상태를 최신화
                ShopUI shopUI = shopUIObject.GetComponent<ShopUI>();
                if (shopUI != null && !shopUIObject.activeSelf)
                {
                    shopUI.RefreshAllSlots();
                }

                shopUIObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[ShopButton] Target shopUIObject is not assigned.");
            }
        }
    }
}
