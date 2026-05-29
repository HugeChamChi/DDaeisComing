using UnityEngine;
using System.Collections.Generic;

namespace Bathhouse.Data
{
    [CreateAssetMenu(fileName = "New NPC Data", menuName = "Bathhouse/NPC Data")]
    public class NPCData : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 2f;

        [Header("Action Durations (Seconds)")]
        public float showerTime = 3f;
        public float bathTime = 5f;
        public float scrubTime = 4f;

        [Header("Preference Weights")]
        [Tooltip("Weights for random facility choice after locker room.")]
        public int weightShower = 20;
        public int weightColdBath = 10;
        public int weightHotBath = 10;
        public int weightSauna = 5;
        public int weightScrubArea = 5;
        public int weightVendingMachine = 5;
    }
}
