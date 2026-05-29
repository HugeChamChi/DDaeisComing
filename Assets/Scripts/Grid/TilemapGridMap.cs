using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Bathhouse.Pathfinding;

namespace Bathhouse.Grid
{
    /// <summary>
    /// Unity 2D Tilemap 시스템을 읽어와서 구성하는 그리드
    /// IGridMap 인터페이스를 상속하여 PathfindingService에서 그대로 주입받아 사용할 수 있습니다.
    /// </summary>
    public class TilemapGridMap : IGridMap
    {
        private Node[,] _grid;
        private Tilemap _walkableTilemap;
        private Tilemap _obstacleTilemap;
        
        private int _gridSizeX;
        private int _gridSizeY;
        private BoundsInt _bounds;

        public int Width => _gridSizeX;
        public int Height => _gridSizeY;

        public TilemapGridMap(Tilemap walkableTilemap, Tilemap obstacleTilemap)
        {
            _walkableTilemap = walkableTilemap;
            _obstacleTilemap = obstacleTilemap;
            _bounds = _walkableTilemap.cellBounds;
            
            _gridSizeX = _bounds.size.x;
            _gridSizeY = _bounds.size.y;

            _grid = new Node[_gridSizeX, _gridSizeY];

            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    Vector3Int localPlace = new Vector3Int(x + _bounds.xMin, y + _bounds.yMin, 0);
                    Vector3 worldPos = _walkableTilemap.CellToWorld(localPlace) + _walkableTilemap.cellSize / 2f; // 셀 중앙 좌표

                    // 타일맵을 읽어서 갈 수 있는지 확인 (장애물 타일이 없고, 바닥 타일이 있으면 Walkable)
                    bool hasFloor = _walkableTilemap.HasTile(localPlace);
                    bool hasObstacle = _obstacleTilemap != null && _obstacleTilemap.HasTile(localPlace);
                    bool isWalkable = hasFloor && !hasObstacle;

                    _grid[x, y] = new Node(isWalkable, worldPos, x, y);
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
            Vector3Int cellPos = _walkableTilemap.WorldToCell(worldPosition);
            
            int x = cellPos.x - _bounds.xMin;
            int y = cellPos.y - _bounds.yMin;

            // 맵 범위를 벗어나면 null 처리 또는 가장자리 반환
            if (x < 0) x = 0;
            if (x >= _gridSizeX) x = _gridSizeX - 1;
            if (y < 0) y = 0;
            if (y >= _gridSizeY) y = _gridSizeY - 1;

            return _grid[x, y];
        }

        public List<Node> GetNeighbors(Node node)
        {
            List<Node> neighbors = new List<Node>();

            // 대각선 금지
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
