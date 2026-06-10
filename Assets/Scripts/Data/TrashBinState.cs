using System;

namespace Bathhouse.Data
{
    /// <summary>
    /// 현재 쓰레기통의 런타임 상태값을 관리하는 클래스입니다.
    /// </summary>
    [Serializable]
    public class TrashBinState
    {
        public float currentGauge;
        public bool isMinigamePlaying;
        
        public TrashBinState(float initialGauge = 0f)
        {
            currentGauge = initialGauge;
            isMinigamePlaying = false;
        }
    }
}
