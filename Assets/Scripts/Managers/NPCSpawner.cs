using UnityEngine;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using System.Threading;
using Bathhouse.NPC;
using Bathhouse.Data;
using Bathhouse.Pathfinding;

namespace Bathhouse.Managers
{
    /// <summary>
    /// SRP: Responsible ONLY for spawning NPCs periodically and managing the Object Pool.
    /// Now creates and injects PathfindingService.
    /// </summary>
    public class NPCSpawner : MonoBehaviour
    {
        [SerializeField] private NPC_Base npcPrefab;
        [SerializeField] private NPCData defaultNpcData;
        [SerializeField] private NPCRouteProfileSO defaultRouteProfile;
        [SerializeField] private float spawnInterval = 5f;

        private Vector3 _spawnPosition;

        private ObjectPool<NPC_Base> _pool;
        private CancellationTokenSource _spawnCts;
        
        // The service injected into each NPC
        private PathfindingService _pathfindingService;
        private System.Collections.Generic.HashSet<NPC_Base> _activeNPCs = new System.Collections.Generic.HashSet<NPC_Base>();

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNoMoreCustomers += StopSpawning;
                GameManager.Instance.OnNextDayStarted += OnNextDayStarted;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNoMoreCustomers -= StopSpawning;
                GameManager.Instance.OnNextDayStarted -= OnNextDayStarted;
            }
        }

        public void Initialize(IGridMap gridMap, int spawnX, int spawnY)
        {
            // Initialize the pathfinding service once per spawner
            _pathfindingService = new PathfindingService(gridMap);

            var spawnNode = gridMap.GetNode(spawnX, spawnY);
            if (spawnNode != null)
            {
                _spawnPosition = spawnNode.WorldPosition;
            }
            else
            {
                Debug.LogWarning("[NPCSpawner] 스폰 좌표가 맵 바깥입니다. (0,0,0)을 사용합니다.");
                _spawnPosition = Vector3.zero;
            }

            _pool = new ObjectPool<NPC_Base>(
                createFunc: () => Instantiate(npcPrefab, _spawnPosition, Quaternion.identity, transform),
                actionOnGet: (npc) => 
                {
                    npc.transform.position = _spawnPosition;
                    npc.gameObject.SetActive(true);
                    // Injecting route profile and pathfinder
                    npc.Initialize(defaultNpcData, defaultRouteProfile, _pathfindingService, OnNpcExit);
                    
                    _activeNPCs.Add(npc);

                    if (global::GlobalManagers.Data != null && global::GlobalManagers.Data.DailyRecord != null)
                    {
                        global::GlobalManagers.Data.DailyRecord.AddVisit();
                    }
                },
                actionOnRelease: (npc) => 
                {
                    _activeNPCs.Remove(npc);
                    npc.gameObject.SetActive(false);
                },
                actionOnDestroy: (npc) => Destroy(npc.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }

        private void OnNextDayStarted()
        {
            // 남은 NPC 강제 퇴장(풀 반환)
            var activeArray = new NPC_Base[_activeNPCs.Count];
            _activeNPCs.CopyTo(activeArray);
            foreach (var npc in activeArray)
            {
                if (npc != null && npc.gameObject.activeInHierarchy)
                {
                    _pool.Release(npc);
                }
            }
            _activeNPCs.Clear();

            StartSpawning();
        }

        public void StartSpawning()
        {
            if (_spawnCts != null) return;
            
            _spawnCts = new CancellationTokenSource();
            SpawnRoutine(_spawnCts.Token).Forget();
        }

        public void StopSpawning()
        {
            if (_spawnCts != null)
            {
                _spawnCts.Cancel();
                _spawnCts.Dispose();
                _spawnCts = null;
            }
        }

        private async UniTaskVoid SpawnRoutine(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(spawnInterval), cancellationToken: token);
                    if (token.IsCancellationRequested) break;
                    
                    _pool.Get();
                }
            }
            catch (System.OperationCanceledException)
            {
                // 취소 시 자연스럽게 종료
            }
        }

        private void OnNpcExit(NPC_Base npc)
        {
            _pool.Release(npc);
        }
    }
}
