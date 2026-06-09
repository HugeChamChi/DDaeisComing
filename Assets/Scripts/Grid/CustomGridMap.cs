using UnityEngine;
using System.Collections.Generic;
using Bathhouse.Data;
using Bathhouse.Pathfinding;

namespace Bathhouse.Grid
{
    /// <summary>
    /// JSON 데이터를 기반으로 생성되는 2D Custom 배열 기반 그리드 (타일맵 미사용 버전)
    /// </summary>
    public class CustomGridMap : IGridMap
    {
        private Node[,] _grid;
        private int _gridSizeX;
        private int _gridSizeY;
        private float _nodeSize;
        private Vector3 _worldOrigin;

        public int Width => _gridSizeX;
        public int Height => _gridSizeY;

        public CustomGridMap(MapData mapData, Vector3 worldOrigin)
        {
            _gridSizeX = mapData.width;
            _gridSizeY = mapData.height;
            _nodeSize = mapData.nodeSize;
            _worldOrigin = worldOrigin;

            _grid = new Node[_gridSizeX, _gridSizeY];

            // 1. 기본 그리드 초기화 (모두 Walkable로 임시 설정)
            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    // 원점 기준으로 각 노드의 실제 월드 좌표 계산 (2D 환경이므로 X-Y 평면 기준)
                    Vector3 worldPoint = _worldOrigin + Vector3.right * (x * _nodeSize + _nodeSize / 2f) + Vector3.up * (y * _nodeSize + _nodeSize / 2f);
                    _grid[x, y] = new Node(true, worldPoint, x, y);
                }
            }

            // 2. JSON 파싱 데이터로 Walkability 덮어쓰기
            if (mapData.tiles != null)
            {
                foreach (var t in mapData.tiles)
                {
                    if (t.x >= 0 && t.x < _gridSizeX && t.y >= 0 && t.y < _gridSizeY)
                    {
                        _grid[t.x, t.y].IsWalkable = t.isWalkable;
                    }
                }
            }

            // 3. 시설물이 차지하는 영역(파란색 영역)은 무조건 이동 불가(장애물) 처리
            if (mapData.facilities != null)
            {
                foreach (var facility in mapData.facilities)
                {
                    for (int fx = 0; fx < facility.sizeX; fx++)
                    {
                        for (int fy = 0; fy < facility.sizeY; fy++)
                        {
                            int cx = facility.originX + fx;
                            int cy = facility.originY + fy;
                            if (cx >= 0 && cx < _gridSizeX && cy >= 0 && cy < _gridSizeY)
                            {
                                _grid[cx, cy].IsWalkable = false;
                            }
                        }
                    }
                }
            }
        }

        public Node GetNode(int x, int y)
        {
            if (x >= 0 && x < _gridSizeX && y >= 0 && y < _gridSizeY)
                return _grid[x, y];
            return null;
        }

        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            float percentX = (worldPosition.x - _worldOrigin.x) / (_gridSizeX * _nodeSize);
            float percentY = (worldPosition.y - _worldOrigin.y) / (_gridSizeY * _nodeSize); // 2D 평면이므로 y 사용
            
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            int x = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((_gridSizeY - 1) * percentY);

            return _grid[x, y];
        }

        public List<Node> GetNeighbors(Node node)
        {
            List<Node> neighbors = new List<Node>();

            // 대각선 금지: 십자 이동(상, 하, 좌, 우)만 가능
            int[] dirX = { 0, 0, -1, 1 };
            int[] dirY = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int checkX = node.GridX + dirX[i];
                int checkY = node.GridY + dirY[i];

                if (checkX >= 0 && checkX < _gridSizeX && checkY >= 0 && checkY < _gridSizeY)
                {
                    neighbors.Add(_grid[checkX, checkY]);
                }
            }

            return neighbors;
        }

        public void UpdateNodeWalkability(int x, int y, bool isWalkable)
        {
            if (x >= 0 && x < _gridSizeX && y >= 0 && y < _gridSizeY)
            {
                _grid[x, y].IsWalkable = isWalkable;
            }
        }
    }
}
