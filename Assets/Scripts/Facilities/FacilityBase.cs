using System.Collections.Generic;
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

        public List<Transform> slots = new List<Transform>();

        [Header("Slot vs Area Settings")]
        [Tooltip("체크하면 개별 Transform 슬롯 대신, 지정된 영역(Area) 내에서 랜덤한 위치를 사용합니다.")]
        public bool useRandomArea = false;
        [Tooltip("랜덤 영역의 크기 (useRandomArea가 true일 때만 사용)")]
        public Vector2 randomAreaSize = new Vector2(2f, 2f);
        
        [Tooltip("체크하면 하나의 Transform 슬롯(자리)에 여러 명의 NPC가 동시에 겹쳐서 사용할 수 있습니다. (예: 수건 보관함)")]
        public bool allowSharedSlots = false;

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
        
        // 영역 모드일 때 생성된 랜덤 위치들을 캐싱하는 배열
        protected Vector3[] _areaSlotPositions;

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
            
            // 수용 인원 결정: 공유 슬롯이거나 Area 모드이거나 슬롯이 없으면 data.maxCapacity 사용, 아니면 slots.Count
            int capacity = _data.maxCapacity;
            if (slots.Count > 0 && !useRandomArea && !allowSharedSlots)
            {
                capacity = slots.Count;
            }
            
            _occupants = new NPC.NPC_Base[capacity];
            _reservers = new NPC.NPC_Base[capacity];
            _slotCooldowns = new float[capacity];
            
            if (useRandomArea)
            {
                _areaSlotPositions = new Vector3[capacity];
            }

            RefreshPosition();
        }

        protected virtual void Awake()
        {
            SyncChildSortingOrders();
        }

        protected virtual void OnValidate()
        {
            SyncChildSortingOrders();
        }

        [ContextMenu("Sync Child Sorting Orders")]
        public void SyncChildSortingOrders()
        {
            var mainSr = GetComponent<SpriteRenderer>();
            if (mainSr != null)
            {
                int baseOrder = mainSr.sortingOrder;
                var childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var child in childRenderers)
                {
                    if (child != mainSr)
                    {
                        child.sortingOrder = baseOrder + 1;
                    }
                }
            }
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

                // 만약 Area 모드라면, 예약 시점에 구역 내 랜덤한 목적지 좌표를 생성해 둡니다.
                if (useRandomArea && _areaSlotPositions != null)
                {
                    float rx = UnityEngine.Random.Range(-randomAreaSize.x / 2f, randomAreaSize.x / 2f);
                    float ry = UnityEngine.Random.Range(-randomAreaSize.y / 2f, randomAreaSize.y / 2f);
                    _areaSlotPositions[slotIndex] = transform.position + new Vector3(rx, ry, 0);
                }

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
            if (useRandomArea && _areaSlotPositions != null)
            {
                if (slotIndex >= 0 && slotIndex < _areaSlotPositions.Length)
                {
                    return _areaSlotPositions[slotIndex];
                }
            }
            else if (slots != null && slots.Count > 0)
            {
                int visualSlotIndex = allowSharedSlots ? (slotIndex % slots.Count) : slotIndex;
                if (visualSlotIndex >= 0 && visualSlotIndex < slots.Count)
                {
                    return slots[visualSlotIndex].position;
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

            // 통계 및 수익 반영
            if (_data != null && global::GlobalManagers.Data != null && global::GlobalManagers.Data.DailyRecord != null)
            {
                var dailyRecord = global::GlobalManagers.Data.DailyRecord;
                dailyRecord.AddFacilityUsage(_data.facilityType);
                
                var incomeDataSO = global::GlobalManagers.Data.IncomeDataSO;
                if (incomeDataSO != null)
                {
                    int income = incomeDataSO.GetIncomeData(_data.facilityType).incomeAmount;
                    dailyRecord.AddIncome(income);
                }
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
        public virtual List<Vector2Int> GetInteractionGridPositions()
        {
            var positions = new List<Vector2Int>();
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
        public virtual List<Vector3> GetAllInteractionWorldPositions()
        {
            var positions = new List<Vector3>();
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

            // 최상위뿐만 아니라 하위의 모든 SpriteRenderer를 검사하여 가장 높은 SortingOrder를 반환
            var childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (childRenderers != null && childRenderers.Length > 0)
            {
                int maxOrder = int.MinValue;
                foreach (var r in childRenderers)
                {
                    if (r.sortingOrder > maxOrder) maxOrder = r.sortingOrder;
                }
                return maxOrder;
            }

            return 0;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (useRandomArea)
            {
                // 인스펙터에서 설정한 크기를 에디터 씬 뷰에 초록색 투명 박스로 그려줍니다.
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                Gizmos.DrawCube(transform.position, new Vector3(randomAreaSize.x, randomAreaSize.y, 1f));
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position, new Vector3(randomAreaSize.x, randomAreaSize.y, 1f));
            }
        }
    }
}
