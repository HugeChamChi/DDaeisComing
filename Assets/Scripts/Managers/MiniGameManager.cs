using System.Collections.Generic;
using UnityEngine;
using Bathhouse.MiniGames;
using DDaeisComing.Attributes;

namespace Bathhouse.Managers
{
    public class MiniGameManager : MonoBehaviour
    {
        [Header("Mini Games")]
        public ScrubMiniGameUI scrubMiniGame;
        public TrashMiniGameUI trashMiniGame;

        [Header("Test Settings")]
        public int testDay = 1;

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
    }
}
