using UnityEngine;
using Bathhouse.MiniGames;

namespace Bathhouse.DebugTools
{
    /// <summary>
    /// 스포트라이트 미니게임 단독 테스트용.
    /// Play 누르면 바로 게임을 시작하고, 성공/실패를 콘솔에 찍는다.
    /// R 키로 재시작.
    /// </summary>
    public class SpotlightMiniGameTester : MonoBehaviour
    {
        [Tooltip("테스트할 미니게임")]
        public SpotlightMiniGameUI miniGame;

        [Tooltip("난이도 계산에 쓰는 가짜 일수")]
        public int testDay = 1;

        private bool _started;

        private void Update()
        {
            // 모든 오브젝트의 Start()가 끝난 뒤(첫 Update) 시작해야
            // 미니게임의 base.Start()가 패널을 끄는 것과 충돌하지 않는다.
            if (!_started)
            {
                _started = true;
                StartGame();
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
                StartGame();
        }

        private void StartGame()
        {
            if (miniGame == null)
            {
                Debug.LogError("[Tester] miniGame이 비어있습니다.");
                return;
            }

            Debug.Log("[Tester] 미니게임 시작 (성공하려면 모든 수건을 비추세요)");
            miniGame.StartMiniGame(
                testDay,
                onSuccess: () => Debug.Log("[Tester] ✅ 성공!"),
                onFail:    () => Debug.Log("[Tester] ❌ 실패(타임아웃)"));
        }
    }
}
