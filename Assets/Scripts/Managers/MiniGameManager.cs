using Bathhouse.Facilities;
using Bathhouse.MiniGames;
using UnityEngine;

namespace Bathhouse.Managers
{
    public class MiniGameManager : MonoBehaviour
    {
        public static MiniGameManager Instance { get; private set; }

        [Header("Mini Games")]
        public ScrubMiniGameUI scrubMiniGame;
        public TrashMiniGameUI trashMiniGame;
        public DDaeisComing.Minigames.Beverage.BeverageMinigameUI beverageMinigame;
        public SpotlightMiniGameUI towelMiniGame;

        public bool IsAnyMiniGamePlaying
        {
            get
            {
                bool scrubPlaying = scrubMiniGame != null && scrubMiniGame.gamePanel != null && scrubMiniGame.gamePanel.activeInHierarchy;
                bool trashPlaying = trashMiniGame != null && trashMiniGame.gamePanel != null && trashMiniGame.gamePanel.activeInHierarchy;
                bool beveragePlaying = beverageMinigame != null && beverageMinigame.gameObject.activeInHierarchy;
                bool towelPlaying = towelMiniGame != null && towelMiniGame.gamePanel != null && towelMiniGame.gamePanel.activeInHierarchy;
                return scrubPlaying || trashPlaying || beveragePlaying || towelPlaying;
            }
        }

        [Header("Test Settings")]
        public int testDay = 1;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayEnded += ForceStopAllGames;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayEnded -= ForceStopAllGames;
            }
        }

        public void ForceStopAllGames()
        {
            if (scrubMiniGame != null && scrubMiniGame.gamePanel != null && scrubMiniGame.gamePanel.activeInHierarchy)
            {
                scrubMiniGame.gamePanel.SetActive(false);
            }
            if (trashMiniGame != null && trashMiniGame.gamePanel != null && trashMiniGame.gamePanel.activeInHierarchy)
            {
                trashMiniGame.gamePanel.SetActive(false);
            }
            if (beverageMinigame != null && beverageMinigame.gameObject.activeInHierarchy)
            {
                beverageMinigame.CloseMinigame();
            }
            if (towelMiniGame != null && towelMiniGame.gamePanel != null && towelMiniGame.gamePanel.activeInHierarchy)
            {
                towelMiniGame.gamePanel.SetActive(false);
            }
        }

        // ContextMenu 혹은 Button 속성을 이용해 에디터에서 쉽게 테스트 가능
        [Button("Test Start Scrub Game")]
        public void TestStartScrubGame()
        {
            if (scrubMiniGame != null)
            {
                Debug.Log($"[MiniGameManager] Testing Scrub Mini Game for Day {testDay}");
                // 임의의 NPC 객체를 넘기거나 null 넘김
                scrubMiniGame.StartMiniGame(null, testDay,
                    () => Debug.Log("Scrub Game Success Callback!"),
                    () => Debug.Log("Scrub Game Fail Callback!"));
            }
            else
            {
                Debug.LogWarning("[MiniGameManager] Scrub Mini Game is not assigned.");
            }
        }

        [Button("Test Start Trash Game")]
        public void TestStartTrashGame()
        {
            if (trashMiniGame != null)
            {
                Debug.Log($"[MiniGameManager] Testing Trash Mini Game for Day {testDay}");
                trashMiniGame.StartMiniGame(testDay,
                    () => Debug.Log("Trash Game Success Callback!"),
                    () => Debug.Log("Trash Game Fail Callback!"));
            }
            else
            {
                Debug.LogWarning("[MiniGameManager] Trash Mini Game is not assigned.");
            }
        }

        public void ShowTrashBinMinigame(TrashBinController targetBin)
        {
            if (trashMiniGame != null)
            {
                int dayToUse = testDay;
                if (global::GlobalManagers.Data != null && global::GlobalManagers.Data.Current != null)
                {
                    dayToUse = global::GlobalManagers.Data.Current.currentDay;
                }

                // 현재 게이지 수치만큼 쓰레기가 나오도록 설정
                trashMiniGame.SetCustomTrashCount(Mathf.RoundToInt(targetBin.GetState().currentGauge));

                trashMiniGame.StartMiniGame(dayToUse,
                    () =>
                    {
                        targetBin.ReduceGauge();
                        targetBin.SetMinigamePlaying(false);
                    },
                    () =>
                    {
                        targetBin.SetMinigamePlaying(false);
                    });
            }
            else
            {
                Debug.LogWarning("[MiniGameManager] Trash Mini Game is not assigned.");
                targetBin.SetMinigamePlaying(false);
            }
        }

        public void ShowBeverageMinigame(BeverageDispenserFacility targetFacility)
        {
            if (beverageMinigame != null)
            {
                beverageMinigame.OpenMinigame();
            }
            else
            {
                Debug.LogWarning("[MiniGameManager] Beverage Mini Game is not assigned.");
            }
        }

        [Button("Test Start Towel Game")]
        public void TestStartTowelGame()
        {
            if (towelMiniGame != null)
            {
                Debug.Log($"[MiniGameManager] Testing Towel Mini Game for Day {testDay}");
                towelMiniGame.StartMiniGame(testDay,
                    () => Debug.Log("Towel Game Success Callback!"),
                    () => Debug.Log("Towel Game Fail Callback!"));
            }
            else
            {
                Debug.LogWarning("[MiniGameManager] Towel Mini Game is not assigned.");
            }
        }

        public void ShowTowelMinigame(System.Action onSuccess = null, System.Action onFail = null)
        {
            if (towelMiniGame != null)
            {
                int dayToUse = testDay;
                if (global::GlobalManagers.Data != null && global::GlobalManagers.Data.Current != null)
                {
                    dayToUse = global::GlobalManagers.Data.Current.currentDay;
                }

                towelMiniGame.StartMiniGame(dayToUse, onSuccess, onFail);
            }
            else
            {
                Debug.LogWarning("[MiniGameManager] Towel Mini Game is not assigned.");
            }
        }
    }
}
