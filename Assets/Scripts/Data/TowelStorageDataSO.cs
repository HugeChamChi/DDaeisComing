using UnityEngine;

namespace Bathhouse.Data
{
    [CreateAssetMenu(fileName = "TowelStorageData", menuName = "Bathhouse/Facilities/TowelStorageData", order = 1)]
    public class TowelStorageDataSO : ScriptableObject
    {
        [Tooltip("수건이 0~9개 남았을 때 보여줄 스프라이트 배열 (인덱스 0 = 0개, 인덱스 9 = 9개)")]
        public Sprite[] towelCountSprites = new Sprite[10];

        public int maxTowelCount = 9;
    }
}
