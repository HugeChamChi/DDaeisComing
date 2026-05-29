using System.Collections.Generic;
using UnityEngine;

namespace Bathhouse.Pathfinding
{
    /// <summary>
    /// 추상화된 그리드 인터페이스. Custom 2D Array Grid나 Unity Tilemap 모두 이것을 상속하여 구현합니다.
    /// </summary>
    public interface IGridMap
    {
        Node GetNode(int x, int y);
        Node NodeFromWorldPoint(Vector3 worldPosition);
        List<Node> GetNeighbors(Node node);
        
        int Width { get; }
        int Height { get; }

        // 실시간 타일 변경 (유저가 욕조 크기를 키우거나 장애물 건설 시)
        void UpdateNodeWalkability(int x, int y, bool isWalkable);
    }
}
