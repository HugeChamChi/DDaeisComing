using UnityEngine;
using Bathhouse.Data;

namespace Bathhouse.Facilities
{
    /// <summary>
    /// 모든 욕탕 구조물(온탕, 냉탕, 락커룸 등)이 상속받는 최상위 베이스 클래스입니다.
    /// </summary>
    public abstract class FacilityBase : MonoBehaviour, IFacility
    {
        [Header("Facility Data")]
        [SerializeField] protected FacilityData _data;
        public FacilityData Data => _data;

        public int GridX { get; protected set; }
        public int GridY { get; protected set; }
        protected float _nodeSize = 1f;

        protected int _currentUsers = 0;
        protected float _currentCleanliness = 100f;

        // 시설을 사용 중인 NPC들을 추적 (배열 인덱스가 곧 '자리(Slot)' 번호)
        protected NPC.NPC_Base[] _occupants;
        protected float[] _slotCooldowns;

        /// <summary>
        /// 인게임 맵 로딩 시 매니저가 호출하여 초기화합니다.
        /// </summary>
        public virtual void Initialize(FacilityData data, int gridX, int gridY, float nodeSize)
        {
            _data = data;
            GridX = gridX;
            GridY = gridY;
            _nodeSize = nodeSize;
            
            _currentUsers = 0;
            _currentCleanliness = 100f;
            
            // 수용 인원만큼 슬롯 배열 할당
            _occupants = new NPC.NPC_Base[_data.maxCapacity];
            _slotCooldowns = new float[_data.maxCapacity];
        }

        protected virtual void Update()
        {
            if (_slotCooldowns == null) return;
            for (int i = 0; i < _slotCooldowns.Length; i++)
            {
                if (_slotCooldowns[i] > 0)
                {
                    _slotCooldowns[i] -= Time.deltaTime;
                    if (_slotCooldowns[i] < 0) _slotCooldowns[i] = 0;
                }
            }
        }

        /// <summary>
        /// 현재 NPC가 이 시설에 입장 가능한지 여부 (수용 인원 꽉 찼는지, 쿨타임 중인지 등)
        /// </summary>
        public virtual bool CanEnter()
        {
            return _data != null && GetAvailableSlotIndex() != -1;
        }

        /// <summary>
        /// 비어있는 자리(Slot Index)를 찾아서 반환합니다. (사람이 없고 쿨타임도 끝난 자리)
        /// </summary>
        public virtual int GetAvailableSlotIndex()
        {
            if (_occupants == null || _slotCooldowns == null) return -1;
            for (int i = 0; i < _occupants.Length; i++)
            {
                if (_occupants[i] == null && _slotCooldowns[i] <= 0) return i;
            }
            return -1;
        }

        public virtual float GetUsageTime()
        {
            return _data != null ? _data.baseUseTime : 2f;
        }

        public virtual FacilityType GetFacilityType()
        {
            return _data != null ? _data.facilityType : FacilityType.None;
        }

        public virtual Vector3 GetUsageWorldPosition(int slotIndex)
        {
            if (_data == null || _data.usageOffsets == null || _data.usageOffsets.Length == 0)
                return transform.position;

            // 슬롯 인덱스에 맞춰서 사용 위치(U 마커) 매핑
            int index = slotIndex % _data.usageOffsets.Length;
            Vector2Int offset = _data.usageOffsets[index];
            
            return new Vector3(
                (GridX + offset.x) * _nodeSize + (_nodeSize / 2f),
                (GridY + offset.y) * _nodeSize + (_nodeSize / 2f),
                0f
            );
        }

        /// <summary>
        /// NPC가 시설 사용을 시작할 때 호출됩니다.
        /// </summary>
        public virtual void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _occupants.Length)
            {
                _occupants[slotIndex] = npc;
            }
            _currentUsers++;
        }

        /// <summary>
        /// NPC가 시설 사용을 마칠 때 호출됩니다.
        /// </summary>
        public virtual void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _occupants.Length)
            {
                _occupants[slotIndex] = null;
                // 쿨타임 설정
                if (_data != null)
                {
                    _slotCooldowns[slotIndex] = _data.usageCooldown;
                }
            }
            _currentUsers--;
            _currentCleanliness -= _data.cleanlinessDropPerUse;
            if (_currentCleanliness < 0) _currentCleanliness = 0;
        }

        /// <summary>
        /// NPC가 시설을 이용하는 동안 매 프레임 호출됩니다.
        /// </summary>
        public virtual void ProgressFacility(NPC.NPC_Base npc, int slotIndex, float deltaTime)
        {
            // 베이스 클래스에서는 기본적으로 아무것도 하지 않습니다.
            // 상속받은 개별 구조물에서 오버라이드하여 지속적인 이펙트나 스탯 변화를 구현합니다.
        }

        /// <summary>
        /// NPC가 이 시설을 이용하기 위해 서야 하는 타일의 '그리드 좌표' 목록을 반환합니다.
        /// (카운터 앞, 욕탕 내부 등)
        /// </summary>
        public virtual System.Collections.Generic.List<Vector2Int> GetInteractionGridPositions()
        {
            var positions = new System.Collections.Generic.List<Vector2Int>();
            if (_data.interactionOffsets != null && _data.interactionOffsets.Length > 0)
            {
                foreach (var offset in _data.interactionOffsets)
                {
                    positions.Add(new Vector2Int(GridX + offset.x, GridY + offset.y));
                }
            }
            else
            {
                positions.Add(new Vector2Int(GridX, GridY)); // Fallback
            }
            return positions;
        }
    }
}
