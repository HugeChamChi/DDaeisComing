using UnityEngine;
using Bathhouse.NPC;

namespace Bathhouse.DebugTools
{
    /// <summary>
    /// NPC의 머리 위에 현재 만족도를 실시간으로 표시해주는 디버그 전용 스크립트입니다.
    /// 인스펙터에서 showDebug를 체크/해제하여 표시 여부를 토글할 수 있습니다.
    /// </summary>
    [RequireComponent(typeof(NPC_Base))]
    public class NPC_DebugSatisfactionDisplay : MonoBehaviour
    {
        [Header("Debug Settings")]
        [Tooltip("체크하면 NPC 머리 위에 만족도 텍스트가 표시됩니다.")]
        public bool showDebug = true;
        
        [Tooltip("머리 위로 얼마나 띄울지 결정하는 오프셋")]
        public Vector3 offset = new Vector3(0, 1.8f, 0);

        private NPC_Base _npcBase;
        private GUIStyle _guiStyle;

        private void Awake()
        {
            _npcBase = GetComponent<NPC_Base>();
            
            _guiStyle = new GUIStyle();
            _guiStyle.fontSize = 24;
            _guiStyle.fontStyle = FontStyle.Bold;
            _guiStyle.alignment = TextAnchor.MiddleCenter;
            // 텍스트 외곽선 효과를 위해 여러 번 그리거나 색상을 눈에 띄게 설정
            _guiStyle.normal.textColor = Color.yellow;
        }

        private void OnGUI()
        {
            if (!showDebug || _npcBase == null || _npcBase.Data == null) return;
            if (Camera.main == null) return;

            // NPC의 월드 좌표 + 오프셋을 화면 픽셀 좌표로 변환
            Vector3 worldPos = transform.position + offset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // 카메라 뒤에 있는 경우 렌더링 생략
            if (screenPos.z < 0) return;

            // GUI는 화면 좌상단을 (0,0)으로 사용하므로 Y축 반전 필요
            screenPos.y = Screen.height - screenPos.y;

            string debugText = $"만족도: {(_npcBase.CurrentSatisfaction * 100f):F0}%";

            // 간단한 그림자 효과를 위해 검은색 텍스트를 먼저 그림
            GUIStyle shadowStyle = new GUIStyle(_guiStyle);
            shadowStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(screenPos.x - 50 + 2, screenPos.y - 20 + 2, 100, 40), debugText, shadowStyle);

            // 실제 노란색 텍스트 렌더링
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 20, 100, 40), debugText, _guiStyle);
        }
    }
}
