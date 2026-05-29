using System;
using System.Collections.Generic;

namespace Bathhouse.Data
{
    [Serializable]
    public class MapData
    {
        public int width;
        public int height;
        public float nodeSize;
        
        // 직렬화를 위한 1차원 리스트 (JSON 파싱용)
        public List<GridTileData> tiles = new List<GridTileData>();
        public List<FacilityPlacementData> facilities = new List<FacilityPlacementData>();

        // 맵에서 NPC가 처음 스폰되는 위치
        public int spawnX = 0;
        public int spawnY = 0;
    }

    [Serializable]
    public class GridTileData
    {
        public int x;
        public int y;
        public bool isWalkable;
    }

    [Serializable]
    public class FacilityPlacementData
    {
        public string instanceId;
        public FacilityType facilityType;
        
        // 10x10 같은 다중 타일 시설물을 위한 사이즈 및 원점
        public int originX;
        public int originY;
        public int sizeX;
        public int sizeY;
    }
}
