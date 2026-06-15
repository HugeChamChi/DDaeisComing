using UnityEngine;
using TMPro;

namespace Bathhouse.UI
{
    /// <summary>
    /// 현재 보유한 골드를 표시해주는 범용 텍스트 컴포넌트입니다.
    /// 로비, 상점 등 골드 표시가 필요한 어떤 TextMeshProUGUI에든 부착하여 사용할 수 있습니다.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class CurrentGoldDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _txtGold;
        private int _lastDisplayedGold = -1;

        private void Awake()
        {
            _txtGold = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            ForceUpdateGold();
        }

        private void Update()
        {
            // 매 프레임 검사하지만 값이 다를 때만 텍스트를 생성하므로 오버헤드가 적습니다.
            if (global::GlobalManagers.Data != null && global::GlobalManagers.Data.Current != null)
            {
                int currentGold = global::GlobalManagers.Data.Current.currentGold;
                if (currentGold != _lastDisplayedGold)
                {
                    _lastDisplayedGold = currentGold;
                    _txtGold.text = _lastDisplayedGold.ToString(); 
                }
            }
        }

        public void ForceUpdateGold()
        {
            if (global::GlobalManagers.Data != null && global::GlobalManagers.Data.Current != null)
            {
                _lastDisplayedGold = global::GlobalManagers.Data.Current.currentGold;
                if (_txtGold != null)
                {
                    _txtGold.text = _lastDisplayedGold.ToString();
                }
            }
        }
    }
}
