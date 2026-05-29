namespace Bathhouse.Facilities
{
    public interface IFacility
    {
        bool CanEnter();
        int GetAvailableSlotIndex();
        
        float GetUsageTime();
        Data.FacilityType GetFacilityType();
        UnityEngine.Vector3 GetUsageWorldPosition(int slotIndex);

        void EnterFacility(NPC.NPC_Base npc, int slotIndex);
        
        /// <summary>
        /// NPC가 시설을 이용하는 동안 매 프레임 호출됩니다.
        /// </summary>
        void ProgressFacility(NPC.NPC_Base npc, int slotIndex, float deltaTime);
        
        void ExitFacility(NPC.NPC_Base npc, int slotIndex);
    }
}
