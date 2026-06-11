using Bathhouse.Data;

namespace Bathhouse.Save
{
    public interface ISavable
    {
        // 로드된 데이터(GameData)를 개별 컴포넌트나 매니저에 적용
        void LoadData(GameData data);
        
        // 현재 상태를 GameData에 반영하여 저장 준비
        void SaveData(ref GameData data);
    }
}
