namespace Bathhouse.Facilities
{
    public interface IFacility
    {
        bool CanEnter();
        int GetAvailableSlotIndex();
        
        /// <summary>
        /// 해당 시설의 자리를 미리 예약합니다.
        /// 성공하면 true와 예약된 슬롯 인덱스를 반환합니다.
        /// </summary>
        bool ReserveSlot(NPC.NPC_Base npc, out int slotIndex);
        
        /// <summary>
        /// 예약했던 시설 이용을 취소합니다. (길찾기 실패 등)
        /// </summary>
        void CancelReservation(NPC.NPC_Base npc);

        /// <summary>
        /// 상호작용 시 Slot 위치로 텔레포트할지 여부를 반환합니다.
        /// </summary>
        bool TeleportToSlotOnUse { get; }
        
        float GetUsageTime();
        Data.FacilityType GetFacilityType();
        UnityEngine.Vector3 GetUsageWorldPosition(int slotIndex);

        void EnterFacility(NPC.NPC_Base npc, int slotIndex);
        
        /// <summary>
        /// NPC가 시설을 이용하는 동안 매 프레임 호출됩니다.
        /// </summary>
        void ProgressFacility(NPC.NPC_Base npc, int slotIndex, float deltaTime);
        
        /// <summary>
        /// 시설의 현재 Base Sorting Order를 반환합니다.
        /// </summary>
        int GetSortingOrder();

        void ExitFacility(NPC.NPC_Base npc, int slotIndex);
    }
}
