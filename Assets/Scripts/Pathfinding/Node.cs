using UnityEngine;

namespace Bathhouse.Pathfinding
{
    public class Node
    {
        public bool IsWalkable;
        public Vector3 WorldPosition;
        public int GridX;
        public int GridY;

        // Pathfinding Cost
        public int gCost;
        public int hCost;
        public Node Parent;

        public int fCost => gCost + hCost;

        public Node(bool isWalkable, Vector3 worldPos, int gridX, int gridY)
        {
            IsWalkable = isWalkable;
            WorldPosition = worldPos;
            GridX = gridX;
            GridY = gridY;
        }
    }
}
