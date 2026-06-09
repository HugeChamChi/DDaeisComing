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

        public System.Collections.Generic.List<Transform> slots = new System.Collections.Generic.List<Transform>();

        public int GridX { get; protected set; }
        public int GridY { get; protected set; }
        [Header("Facility State")]
        [SerializeField] protected int _currentUsers = 0;
        [SerializeField] protected float _currentCleanliness = 100f;

        [Header("Animation Settings")]
        [Tooltip("상호작용 시 구조물 내부의 Slot 위치로 텔레포트할지 여부 (꺼두면 상호작용 위치에서 그대로 이용)")]
        public bool teleportToSlotOnUse = true;
        public bool TeleportToSlotOnUse => teleportToSlotOnUse;

        [Tooltip("상호작용 시 구조물 방향을 바라보게 할지 여부")]
        public bool lookAtFacilityOnInteract = true;
        [Tooltip("고유 Action_XXX 애니메이션을 재생할지 여부")]
        public bool playSpecificActionAnimation = false;

        protected float _nodeSize = 1f;

        // 슬롯별 사용자 정보중인 NPC들을 추적 (배열 인덱스가 곧 '자리(Slot)' 번호)
        protected NPC.NPC_Base[] _occupants;
        protected NPC.NPC_Base[] _reservers; // 미리 자리를 찜해둔 NPC
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
            int capacity = slots.Count > 0 ? slots.Count : _data.maxCapacity;
            _occupants = new NPC.NPC_Base[capacity];
            _reservers = new NPC.NPC_Base[capacity];
            _slotCooldowns = new float[capacity];

            RefreshPosition();
        }

        protected virtual void OnEnable()
        {
            FacilityData.OnDataChanged += HandleDataChanged;
        }

        protected virtual void OnDisable()
        {
            FacilityData.OnDataChanged -= HandleDataChanged;
        }

        private void HandleDataChanged(FacilityData data)
        {
            if (_data == data)
            {
                RefreshPosition();
            }
        }

        /// <summary>
        /// 데이터의 visualOffset과 그리드 좌표를 기반으로 실제 월드 위치를 갱신합니다.
        /// </summary>
        public void RefreshPosition()
        {
            if (_data == null) return;

            // 시설이 차지하는 영역의 중앙(Center) 기준 배치
            float centerX = GridX + (_data.width / 2f);
            float centerY = GridY + (_data.height / 2f);

            Vector3 worldPos = new Vector3(
                centerX * _nodeSize,
                centerY * _nodeSize,
                0f
            );

            worldPos += _data.visualPosOffset;
            transform.position = worldPos;
            transform.localScale = _data.visualScaleOffset;
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
        /// 비어있는 자리(Slot Index)를 찾아서 반환합니다. (사람도 없고, 쿨타임도 끝났고, 예약자도 없는 자리)
        /// </summary>
        public virtual int GetAvailableSlotIndex()
        {
            if (_occupants == null || _slotCooldowns == null) return -1;
            for (int i = 0; i < _occupants.Length; i++)
            {
                if (_occupants[i] == null && _slotCooldowns[i] <= 0 && _reservers[i] == null) return i;
            }
            return -1;
        }

        public virtual bool ReserveSlot(NPC.NPC_Base npc, out int slotIndex)
        {
            slotIndex = GetAvailableSlotIndex();
            if (slotIndex != -1)
            {
                _reservers[slotIndex] = npc;
                return true;
            }
            return false;
        }

        public virtual void CancelReservation(NPC.NPC_Base npc)
        {
            if (_reservers == null) return;
            for (int i = 0; i < _reservers.Length; i++)
            {
                if (_reservers[i] == npc)
                {
                    _reservers[i] = null;
                }
            }
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
            if (slots != null && slots.Count > 0)
            {
                if (slotIndex >= 0 && slotIndex < slots.Count)
                {
                    return slots[slotIndex].position;
                }
            }

            return transform.position;
        }

        /// <summary>
        /// NPC가 시설 사용을 시작할 때 호출됩니다.
        /// </summary>
        public virtual void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _occupants.Length)
            {
                _occupants[slotIndex] = npc;
                // 예약 해제 후 점유로 전환
                if (_reservers != null && _reservers[slotIndex] == npc)
                {
                    _reservers[slotIndex] = null;
                }
            }
            _currentUsers++;

            // 시설 상호작용 애니메이션 및 방향 처리
            var animController = npc.GetComponent<NPC.NPCAnimationController>();
            if (animController != null && _data != null)
            {
                if (lookAtFacilityOnInteract)
                {
                    animController.FaceTarget(transform.position);
                }
                
                if (playSpecificActionAnimation)
                {
                    animController.PlayFacilityAction(_data.facilityType);
                }
            }
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

            // 상호작용 애니메이션 종료
            var animController = npc.GetComponent<NPC.NPCAnimationController>();
            if (animController != null)
            {
                animController.StopFacilityAction();
            }
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

        /// <summary>
        /// 첫 번째 상호작용 지점의 월드 좌표를 반환합니다. (길찾기 목적지용)
        /// </summary>
        public virtual Vector3 GetMainInteractionWorldPosition()
        {
            var gridPos = GetInteractionGridPositions()[0];
            return new Vector3(
                gridPos.x * _nodeSize + (_nodeSize / 2f),
                gridPos.y * _nodeSize + (_nodeSize / 2f),
                0f
            );
        }

        /// <summary>
        /// 모든 상호작용 지점의 월드 좌표 목록을 반환합니다.
        /// </summary>
        public virtual System.Collections.Generic.List<Vector3> GetAllInteractionWorldPositions()
        {
            var positions = new System.Collections.Generic.List<Vector3>();
            var gridPositions = GetInteractionGridPositions();
            foreach (var gridPos in gridPositions)
            {
                positions.Add(new Vector3(
                    gridPos.x * _nodeSize + (_nodeSize / 2f),
                    gridPos.y * _nodeSize + (_nodeSize / 2f),
                    0f
                ));
            }
            return positions;
        }

        public virtual int GetSortingOrder()
        {
            var sg = GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (sg != null) return sg.sortingOrder;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) return sr.sortingOrder;

            var childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (childRenderers != null && childRenderers.Length > 0)
            {
                int maxOrder = int.MinValue;
                foreach (var r in childRenderers)
                {
                    if (r.sortingOrder > maxOrder)
                    {
                        maxOrder = r.sortingOrder;
                    }
                }
                return maxOrder;
            }

            return 0;
        }
    }
}
