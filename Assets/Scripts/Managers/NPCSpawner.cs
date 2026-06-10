using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
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
        private Coroutine _spawnCoroutine;
        
        // The service injected into each NPC
        private PathfindingService _pathfindingService;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNoMoreCustomers += StopSpawning;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNoMoreCustomers -= StopSpawning;
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
                    
                    if (GameManager.Data != null && GameManager.Data.DailyRecord != null)
                    {
                        GameManager.Data.DailyRecord.AddVisit();
                    }
                },
                actionOnRelease: (npc) => npc.gameObject.SetActive(false),
                actionOnDestroy: (npc) => Destroy(npc.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }

        public void StartSpawning()
        {
            if (_spawnCoroutine != null) return;
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        public void StopSpawning()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);
                _pool.Get();
            }
        }

        private void OnNpcExit(NPC_Base npc)
        {
            _pool.Release(npc);
        }
    }
}
