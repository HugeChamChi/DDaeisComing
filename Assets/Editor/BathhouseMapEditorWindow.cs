using UnityEngine;
using UnityEditor;
using Bathhouse.Data;
using System.IO;
using System.Collections.Generic;
using System.Linq;
namespace Bathhouse.Editor
{
    public class BathhouseMapEditorWindow : EditorWindow
    {
        private MapData _mapData;
        private int _gridWidth = 10;
        private int _gridHeight = 10;
        private float _nodeSize = 1f;

        private Vector2 _scrollPosition;
        private string _savePath = "Assets/Resources/DefaultMap.json";

        private enum EditorMode { PaintWalkable, PlaceFacility, SetSpawnPoint }
        private EditorMode _currentMode = EditorMode.PaintWalkable;
        private FacilityData _selectedFacilityData;
        private List<FacilityData> _availableFacilities = new List<FacilityData>();

        private string _draggingFacilityId = null;
        private int _dragOffsetX = 0;
        private int _dragOffsetY = 0;

        [MenuItem("Bathhouse/Map Editor Window")]
        public static void ShowWindow()
        {
            GetWindow<BathhouseMapEditorWindow>("Bathhouse Map Editor");
        }

        private void OnEnable()
        {
            InitializeMap();
            LoadFacilityData();
        }

        private void LoadFacilityData()
        {
            _availableFacilities.Clear();
            string[] guids = AssetDatabase.FindAssets("t:FacilityData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FacilityData data = AssetDatabase.LoadAssetAtPath<FacilityData>(path);
                if (data != null) _availableFacilities.Add(data);
            }
            if (_availableFacilities.Count > 0)
            {
                _selectedFacilityData = _availableFacilities[0];
            }
        }

        private void InitializeMap()
        {
            _mapData = new MapData
            {
                width = _gridWidth,
                height = _gridHeight,
                nodeSize = _nodeSize,
                tiles = new List<GridTileData>(),
                facilities = new List<FacilityPlacementData>()
            };

            for (int y = _gridHeight - 1; y >= 0; y--) // Top to bottom visually
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    _mapData.tiles.Add(new GridTileData { x = x, y = y, isWalkable = true });
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
            
            _gridWidth = EditorGUILayout.IntSlider("Width", _gridWidth, 5, 50);
            _gridHeight = EditorGUILayout.IntSlider("Height", _gridHeight, 5, 50);
            _nodeSize = EditorGUILayout.FloatField("Node Size", _nodeSize);
            
            if (GUILayout.Button("Apply New Size & Reset Grid"))
            {
                InitializeMap();
                GUIUtility.ExitGUI(); // Prevent layout errors during repaint
            }

            GUILayout.Space(10);
            GUILayout.Label("Editor Mode", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_currentMode == EditorMode.PaintWalkable, "Paint Walkable (Wall)", "Button")) _currentMode = EditorMode.PaintWalkable;
            if (GUILayout.Toggle(_currentMode == EditorMode.PlaceFacility, "Place Facility (Buildings)", "Button")) _currentMode = EditorMode.PlaceFacility;
            if (GUILayout.Toggle(_currentMode == EditorMode.SetSpawnPoint, "Set Spawn Point (Entrance)", "Button")) _currentMode = EditorMode.SetSpawnPoint;
            GUILayout.EndHorizontal();

            if (_currentMode == EditorMode.PlaceFacility)
            {
                if (_availableFacilities.Count == 0)
                {
                    if (GUILayout.Button("Load Facility Data")) LoadFacilityData();
                }
                else
                {
                    string[] options = _availableFacilities.Select(f => f.facilityName).ToArray();
                    int currentIndex = _availableFacilities.IndexOf(_selectedFacilityData);
                    if (currentIndex < 0) currentIndex = 0;
                    
                    int newIndex = EditorGUILayout.Popup("Selected Facility", currentIndex, options);
                    if (newIndex >= 0 && newIndex < _availableFacilities.Count) 
                    {
                        _selectedFacilityData = _availableFacilities[newIndex];
                    }
                    
                    if (_selectedFacilityData != null)
                    {
                        GUILayout.Label($"Size: {_selectedFacilityData.width}x{_selectedFacilityData.height}", EditorStyles.miniLabel);
                    }
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Map Canvas (Green: Walkable, Red: Obstacle, Blue: Facility)", EditorStyles.label);
            
            // Grid Canvas: 저장된 맵 데이터의 크기를 기준으로 그립니다.
            if (_mapData != null && _mapData.tiles != null)
            {
                int drawWidth = _mapData.width;
                int drawHeight = _mapData.height;

                float maxCanvasSize = 400f;
                float cellSize = Mathf.Min(maxCanvasSize / drawWidth, maxCanvasSize / drawHeight);
                float canvasWidth = cellSize * drawWidth;
                float canvasHeight = cellSize * drawHeight;
                
                // 지정된 폭과 높이만큼 에디터 창 공간을 정확히 할당받습니다.
                Rect canvasRect = GUILayoutUtility.GetRect(canvasWidth, canvasHeight);
                
                // 바닥 타일 그리기 (클릭 감지는 별도의 Event.current로 처리)
                foreach (var tile in _mapData.tiles)
                {
                    float guiX = canvasRect.x + tile.x * cellSize;
                    float guiY = canvasRect.y + (drawHeight - 1 - tile.y) * cellSize;
                    Rect cellRect = new Rect(guiX, guiY, cellSize, cellSize);
                    
                    GUI.backgroundColor = tile.isWalkable ? Color.green : Color.red;
                    // GUI.skin.button 스타일을 적용하여 뚜렷한 그리드 경계선(테두리)을 복구합니다.
                    GUI.Box(cellRect, "", GUI.skin.button);
                }
                GUI.backgroundColor = Color.white;

                // 시설 렌더링
                if (_mapData.facilities != null)
                {
                    foreach (var facility in _mapData.facilities)
                    {
                        FacilityData fd = _availableFacilities.Find(d => d.facilityType == facility.facilityType);
                        int fw = fd != null ? fd.width : 1;
                        int fh = fd != null ? fd.height : 1;

                        float fx = canvasRect.x + facility.originX * cellSize;
                        float fy = canvasRect.y + (drawHeight - 1 - (facility.originY + fh - 1)) * cellSize;

                        Rect facRect = new Rect(fx, fy, cellSize * fw, cellSize * fh);
                        GUI.backgroundColor = new Color(0.2f, 0.5f, 1f, 0.9f);
                        
                        GUIStyle style = new GUIStyle(GUI.skin.box);
                        style.normal.textColor = Color.white;
                        style.alignment = TextAnchor.MiddleCenter;
                        style.wordWrap = true;
                        
                        GUI.Box(facRect, facility.facilityType.ToString(), style);
                        GUI.backgroundColor = Color.white;

                        // 상호작용 지점 표시
                        if (fd != null && fd.interactionOffsets != null)
                        {
                            foreach (var offset in fd.interactionOffsets)
                            {
                                float interactX = canvasRect.x + (facility.originX + offset.x) * cellSize;
                                float interactY = canvasRect.y + (drawHeight - 1 - (facility.originY + offset.y)) * cellSize;
                                Rect interactRect = new Rect(interactX, interactY, cellSize, cellSize);
                                
                                GUI.color = Color.yellow;
                                GUIStyle highlightStyle = new GUIStyle(GUI.skin.box);
                                highlightStyle.normal.background = Texture2D.whiteTexture;
                                GUI.backgroundColor = new Color(1f, 1f, 0f, 0.5f);
                                GUI.Box(interactRect, "목표", style);
                                GUI.backgroundColor = Color.white;
                                GUI.color = Color.white;
                            }
                        }
                    }
                }

                // 마우스 입력(드래그 앤 드롭 등) 처리
                Event e = Event.current;
                if (canvasRect.Contains(e.mousePosition))
                {
                    float localX = e.mousePosition.x - canvasRect.x;
                    float localY = e.mousePosition.y - canvasRect.y;
                    
                    int cellX = Mathf.FloorToInt(localX / cellSize);
                    int cellY = drawHeight - 1 - Mathf.FloorToInt(localY / cellSize);

                    if (cellX >= 0 && cellX < drawWidth && cellY >= 0 && cellY < drawHeight)
                    {
                        if (_currentMode == EditorMode.PaintWalkable)
                        {
                            if (e.type == EventType.MouseDown && e.button == 0)
                            {
                                var t = _mapData.tiles.Find(tile => tile.x == cellX && tile.y == cellY);
                                if (t != null) t.isWalkable = !t.isWalkable;
                                e.Use();
                            }
                        }
                        else if (_currentMode == EditorMode.PlaceFacility)
                        {
                            var existing = _mapData.facilities.Find(f => 
                            {
                                FacilityData fd = _availableFacilities.Find(d => d.facilityType == f.facilityType);
                                int fw = fd != null ? fd.width : 1;
                                int fh = fd != null ? fd.height : 1;
                                return cellX >= f.originX && cellX < f.originX + fw && cellY >= f.originY && cellY < f.originY + fh;
                            });

                            if (e.type == EventType.MouseDown && e.button == 0)
                            {
                                if (existing != null)
                                {
                                    // 드래그 시작
                                    _draggingFacilityId = existing.instanceId;
                                    _dragOffsetX = cellX - existing.originX;
                                    _dragOffsetY = cellY - existing.originY;
                                }
                                else if (_selectedFacilityData != null)
                                {
                                    // 새 시설 배치
                                    if (cellX + _selectedFacilityData.width <= drawWidth && cellY + _selectedFacilityData.height <= drawHeight)
                                    {
                                        _mapData.facilities.Add(new FacilityPlacementData
                                        {
                                            facilityType = _selectedFacilityData.facilityType,
                                            originX = cellX,
                                            originY = cellY,
                                            sizeX = _selectedFacilityData.width,
                                            sizeY = _selectedFacilityData.height,
                                            instanceId = System.Guid.NewGuid().ToString()
                                        });
                                    }
                                }
                                e.Use();
                            }
                            else if (e.type == EventType.MouseDrag && e.button == 0 && !string.IsNullOrEmpty(_draggingFacilityId))
                            {
                                // 시설 드래그 이동
                                var draggingFac = _mapData.facilities.Find(f => f.instanceId == _draggingFacilityId);
                                if (draggingFac != null)
                                {
                                    int newX = cellX - _dragOffsetX;
                                    int newY = cellY - _dragOffsetY;

                                    // 맵 바깥으로 나가지 않도록 제한
                                    newX = Mathf.Clamp(newX, 0, drawWidth - draggingFac.sizeX);
                                    newY = Mathf.Clamp(newY, 0, drawHeight - draggingFac.sizeY);

                                    draggingFac.originX = newX;
                                    draggingFac.originY = newY;
                                    Repaint();
                                }
                                e.Use();
                            }
                            else if (e.type == EventType.MouseUp && e.button == 0)
                            {
                                // 드래그 종료
                                _draggingFacilityId = null;
                                Repaint();
                            }
                            else if (e.type == EventType.MouseDown && e.button == 1)
                            {
                                // 우클릭 삭제
                                if (existing != null)
                                {
                                    _mapData.facilities.Remove(existing);
                                    e.Use();
                                }
                            }
                        }
                        else if (_currentMode == EditorMode.SetSpawnPoint)
                        {
                            if (e.type == EventType.MouseDown && e.button == 0)
                            {
                                _mapData.spawnX = cellX;
                                _mapData.spawnY = cellY;
                                e.Use();
                                Repaint();
                            }
                        }
                    }
                }

                // 스폰 지점 표시 (주황색 S)
                if (_mapData != null)
                {
                    float spawnX = canvasRect.x + _mapData.spawnX * cellSize;
                    float spawnY = canvasRect.y + (drawHeight - 1 - _mapData.spawnY) * cellSize;
                    Rect spawnRect = new Rect(spawnX, spawnY, cellSize, cellSize);
                    
                    GUIStyle spawnStyle = new GUIStyle(GUI.skin.box);
                    spawnStyle.normal.background = Texture2D.whiteTexture;
                    spawnStyle.normal.textColor = Color.black;
                    spawnStyle.alignment = TextAnchor.MiddleCenter;
                    
                    GUI.backgroundColor = new Color(1f, 0.5f, 0f, 0.9f); // 주황색
                    GUI.Box(spawnRect, "S", spawnStyle);
                    GUI.backgroundColor = Color.white;
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("File Operations", EditorStyles.boldLabel);
            _savePath = EditorGUILayout.TextField("File Path", _savePath);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save JSON"))
            {
                SaveToJson();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("Load JSON"))
            {
                LoadFromJson();
                GUIUtility.ExitGUI();
            }
            GUILayout.EndHorizontal();
        }

        private void SaveToJson()
        {
            if (_mapData == null) return;

            try
            {
                string dir = Path.GetDirectoryName(_savePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonUtility.ToJson(_mapData, true);
                File.WriteAllText(_savePath, json);
                AssetDatabase.Refresh();
                Debug.Log($"Map saved successfully to {_savePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MapEditor] Save failed: {ex.Message}");
            }
        }

        private void LoadFromJson()
        {
            if (!File.Exists(_savePath))
            {
                Debug.LogError($"No JSON file found at {_savePath}");
                return;
            }

            string json = File.ReadAllText(_savePath);
            _mapData = JsonUtility.FromJson<MapData>(json);
            
            // Sync UI variables
            _gridWidth = _mapData.width;
            _gridHeight = _mapData.height;
            _nodeSize = _mapData.nodeSize;
            
            Debug.Log($"Map loaded from {_savePath}");
        }
    }
}
