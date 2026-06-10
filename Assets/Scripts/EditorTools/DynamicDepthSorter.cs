using UnityEngine;
using UnityEngine.Rendering;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Bathhouse.Tools
{
    /// <summary>
    /// 캐릭터나 NPC처럼 실시간으로 이동하는 오브젝트의 Y좌표를 
    /// 지정된 주기(예: 0.1초)마다 검사하여 Sorting Order를 업데이트하는 스크립트입니다.
    /// </summary>
    public class DynamicDepthSorter : MonoBehaviour
    {
        [Tooltip("정렬을 업데이트할 주기 (초). 매 프레임 업데이트 방지용")]
        public float updateInterval = 0.1f;
        
        [Tooltip("정렬 기준이 될 Y좌표 오프셋 (캐릭터의 발밑 좌표를 위해 미세조정이 필요할 때 사용)")]
        public float yOffset = 0f;

        private SortingGroup _sortingGroup;
        private SpriteRenderer[] _renderers;

        public bool IsPaused { get; set; } = false;

        private void Awake()
        {
            _sortingGroup = GetComponent<SortingGroup>();
            if (_sortingGroup == null)
            {
                _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            }
        }

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            // 오브젝트 풀링을 고려하여, 활성화될 때마다 UniTask를 새로 시작합니다.
            _cts = new CancellationTokenSource();
            UpdateDepthRoutineAsync(_cts.Token).Forget();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void ForceSetOrder(int order)
        {
            if (_sortingGroup != null)
            {
                _sortingGroup.sortingOrder = order;
            }
            else if (_renderers != null)
            {
                foreach (var r in _renderers)
                {
                    r.sortingOrder = order;
                }
            }
        }

        private async UniTaskVoid UpdateDepthRoutineAsync(CancellationToken token)
        {
            // NPC가 비활성화되거나 파괴될 때까지 반복
            while (!token.IsCancellationRequested)
            {
                if (!IsPaused)
                {
                    // Y좌표가 음수일 때를 대비하여 기본값을 크게(30000) 잡아 무조건 양수가 나오도록 보정합니다.
                    // Y는 1칸당 -100, X는 1칸당 -1로 계산하여 같은 Y선상에 있을 때 왼쪽(X가 작은 쪽)이 더 앞에 오도록 처리합니다.
                    int order = 30000 
                              - Mathf.RoundToInt((transform.position.y + yOffset) * 100f)
                              - Mathf.RoundToInt(transform.position.x * 1f);

                    if (_sortingGroup != null)
                    {
                        _sortingGroup.sortingOrder = order;
                    }
                    // 그룹이 없고 렌더러들만 있으면 전부 변경
                    else if (_renderers != null)
                    {
                        foreach (var r in _renderers)
                        {
                            r.sortingOrder = order;
                        }
                    }
                }

                // 지정된 시간만큼 대기 (Update 매 프레임 연산 방지)
                await UniTask.Delay(System.TimeSpan.FromSeconds(updateInterval), cancellationToken: token);
            }
        }
    }
}
