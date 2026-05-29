using UnityEngine;
using System.Collections.Generic;

namespace Bathhouse.Pathfinding
{
    /// <summary>
    /// SRP: IGridMap을 주입받아 대각선이 제외된 A* 길찾기 알고리즘을 수행합니다.
    /// </summary>
    public class PathfindingService
    {
        private IGridMap _gridMap;

        // DI(의존성 주입)를 통해 어떤 형태의 Grid(Custom, Tilemap)가 오든 유연하게 대처
        public PathfindingService(IGridMap gridMap)
        {
            _gridMap = gridMap;
        }

        public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            Node startNode = _gridMap.NodeFromWorldPoint(startPos);
            Node targetNode = _gridMap.NodeFromWorldPoint(targetPos);

            if (startNode == null || targetNode == null) return null;

            List<Node> openSet = new List<Node>();
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                // 목표 도착
                if (currentNode == targetNode)
                {
                    return RetracePath(startNode, targetNode);
                }

                foreach (Node neighbor in _gridMap.GetNeighbors(currentNode))
                {
                    if (!neighbor.IsWalkable || closedSet.Contains(neighbor))
                        continue;

                    // 이동 비용은 십자 이동이므로 무조건 10으로 통일
                    int moveCostToNeighbor = currentNode.gCost + 10;
                    if (moveCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                    {
                        neighbor.gCost = moveCostToNeighbor;
                        neighbor.hCost = GetDistance(neighbor, targetNode);
                        neighbor.Parent = currentNode;

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            return null; // 길을 찾지 못한 경우
        }

        private List<Vector3> RetracePath(Node startNode, Node endNode)
        {
            List<Vector3> path = new List<Vector3>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode.WorldPosition);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }

        // 대각선 이동을 허용하지 않으므로, 휴리스틱 거리는 맨해튼 거리(Manhattan Distance)를 사용합니다.
        private int GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
            int dstY = Mathf.Abs(nodeA.GridY - nodeB.GridY);

            return 10 * (dstX + dstY);
        }
    }
}
