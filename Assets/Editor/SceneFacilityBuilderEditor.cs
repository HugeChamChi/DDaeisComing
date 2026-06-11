using UnityEngine;
using UnityEditor;
using Bathhouse.Tools;
using Bathhouse.Data;

namespace Bathhouse.EditorScripts
{
    [CustomEditor(typeof(SceneFacilityBuilder))]
    public class SceneFacilityBuilderEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _cachedFacilityEditor;
        
        // 드래그 상태를 추적하기 위한 변수
        private SceneFacilityInfo _draggedFacility = null;
        private int _dragOffsetX = 0;
        private int _dragOffsetY = 0;

        // 커스텀 맵 확장을 위한 변수
        private int _expLeft = 0;
        private int _expRight = 0;
        private int _expTop = 0;
        private int _expBottom = 0;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SceneFacilityBuilder builder = (SceneFacilityBuilder)target;

            GUILayout.Space(10);
            GUILayout.Label("빌더 모드 (Builder Mode)", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(builder.currentMode == BuilderMode.None, "선택 모드", "Button"))
                builder.currentMode = BuilderMode.None;
            if (GUILayout.Toggle(builder.currentMode == BuilderMode.Placement, "배치 모드", "Button"))
                builder.currentMode = BuilderMode.Placement;
            if (GUILayout.Toggle(builder.currentMode == BuilderMode.Erase, "지우개 모드", "Button"))
                builder.currentMode = BuilderMode.Erase;
            if (GUILayout.Toggle(builder.currentMode == BuilderMode.SetWalkable, "길 허용", "Button"))
                builder.currentMode = BuilderMode.SetWalkable;
            if (GUILayout.Toggle(builder.currentMode == BuilderMode.SetUnwalkable, "길 차단", "Button"))
                builder.currentMode = BuilderMode.SetUnwalkable;
            if (GUILayout.Toggle(builder.currentMode == BuilderMode.InsertGrid, "칸 삽입(분할)", "Button"))
                builder.currentMode = BuilderMode.InsertGrid;
            GUILayout.EndHorizontal();

            if (builder.currentMode == BuilderMode.InsertGrid)
            {
                GUILayout.Space(10);
                GUILayout.BeginVertical("helpbox");
                GUILayout.Label("선택한 타일 기준 분할 삽입 설정", EditorStyles.boldLabel);
                builder.insertRightAmount = EditorGUILayout.IntField("기준 칸 우측(+X) 삽입 수", builder.insertRightAmount);
                builder.insertTopAmount = EditorGUILayout.IntField("기준 칸 상단(+Y) 삽입 수", builder.insertTopAmount);
                EditorGUILayout.HelpBox("씬 뷰(Scene View)에서 특정 타일을 클릭하면, 해당 타일을 기준으로 우측 또는 상단에 지정한 수만큼 공간이 삽입되며 그 밖의 구조물들은 전부 밀려납니다.", MessageType.Info);
                GUILayout.EndVertical();
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("타일 초기화"))
            {
                builder.InitializeTiles();
                EditorUtility.SetDirty(builder);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("모든 구조물 지우기"))
            {
                ClearAllFacilities(builder);
            }
            if (GUILayout.Button("전체 구조물 프리팹/위치 재배치"))
            {
                ResyncAllFacilities(builder);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("맵 영역 자동 조절", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("중앙 기준 맵 확장 (+2x2)")) builder.ExpandMapCentered();
            if (GUILayout.Button("중앙 기준 맵 축소 (-2x2)")) builder.ShrinkMapCentered();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginVertical("helpbox");
            GUILayout.Label("특정 방향(칸) 기준 늘리기/줄이기", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("상(Top)", GUILayout.Width(40));
            _expTop = EditorGUILayout.IntField(_expTop, GUILayout.Width(40));
            GUILayout.Label("하(Bottom)", GUILayout.Width(60));
            _expBottom = EditorGUILayout.IntField(_expBottom, GUILayout.Width(40));
            GUILayout.Label("좌(Left)", GUILayout.Width(40));
            _expLeft = EditorGUILayout.IntField(_expLeft, GUILayout.Width(40));
            GUILayout.Label("우(Right)", GUILayout.Width(40));
            _expRight = EditorGUILayout.IntField(_expRight, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("설정한 칸 수만큼 늘리기/줄이기 적용"))
            {
                builder.ExpandMapCustom(_expLeft, _expRight, _expTop, _expBottom);
                _expLeft = 0; _expRight = 0; _expTop = 0; _expBottom = 0; // 초기화
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.Label("설치 가능한 구조물", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("아래 버튼을 클릭하여 구조물을 선택한 후, 씬 뷰(Scene View)의 원하는 위치를 좌클릭하여 배치하세요.", MessageType.Info);

            string[] guids = AssetDatabase.FindAssets("t:FacilityData");
            if (guids.Length > 0)
            {
                float viewWidth = EditorGUIUtility.currentViewWidth - 40f; 
                float buttonSize = 80f;
                int columns = Mathf.Max(1, Mathf.FloorToInt(viewWidth / buttonSize));
                int colCount = 0;

                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    FacilityData fd = AssetDatabase.LoadAssetAtPath<FacilityData>(path);
                    if (fd != null)
                    {
                        if (colCount >= columns)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.Space(5);
                            GUILayout.BeginHorizontal();
                            colCount = 0;
                        }

                        string displayName = string.IsNullOrEmpty(fd.facilityName) ? fd.name : fd.facilityName;
                        Color oldColor = GUI.backgroundColor;
                        bool isSelected = builder.selectedFacility == fd;
                        GUI.backgroundColor = isSelected ? Color.cyan : new Color(0.9f, 0.9f, 0.9f);

                        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                        btnStyle.fontSize = 11;
                        btnStyle.fontStyle = FontStyle.Bold;
                        btnStyle.wordWrap = true;

                        if (GUILayout.Button(displayName, btnStyle, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                        {
                            builder.selectedFacility = fd;
                            builder.currentMode = BuilderMode.Placement;
                            EditorUtility.SetDirty(builder);
                            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Focus();
                        }
                        GUI.backgroundColor = oldColor;
                        colCount++;
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.Label("프로젝트 내에 FacilityData를 찾을 수 없습니다.");
            }

            if (builder.selectedFacility != null)
            {
                GUILayout.Space(20);
                string displayName = string.IsNullOrEmpty(builder.selectedFacility.facilityName) ? builder.selectedFacility.name : builder.selectedFacility.facilityName;
                GUILayout.Label($"[{displayName}] 데이터 상세 설정", EditorStyles.boldLabel);

                GUILayout.BeginVertical("helpbox");
                UnityEditor.Editor.CreateCachedEditor(builder.selectedFacility, null, ref _cachedFacilityEditor);
                if (_cachedFacilityEditor != null)
                {
                    _cachedFacilityEditor.OnInspectorGUI();
                }
                GUILayout.EndVertical();
            }
        }

        private void OnSceneGUI()
        {
            SceneFacilityBuilder builder = (SceneFacilityBuilder)target;

            Event e = Event.current;
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) {
                SceneView.RepaintAll();
            }
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            // 마우스 위치를 월드 좌표로 변환
            Vector3 mousePos = e.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            
            // 2D 씬 평면(Z=0)과의 교점 찾기
            if (ray.direction.z != 0)
            {
                float t = (builder.transform.position.z - ray.origin.z) / ray.direction.z;
                Vector3 worldPos = ray.origin + ray.direction * t;

                // 빌더 원점 기준 상대 좌표
                float relX = worldPos.x - builder.transform.position.x;
                float relY = worldPos.y - builder.transform.position.y;

                // 마우스가 그리드 상에서 어느 칸(그리드 X, Y)에 있는지 계산
                int gridX = Mathf.FloorToInt(relX / builder.nodeSize);
                int gridY = Mathf.FloorToInt(relY / builder.nodeSize);

                FacilityData data = builder.selectedFacility;

                if (builder.currentMode == BuilderMode.Placement)
                {
                    if (e.type == EventType.MouseUp && e.button == 0)
                    {
                        if (_draggedFacility != null)
                        {
                            _draggedFacility = null;
                            GUIUtility.hotControl = 0;
                            e.Use();
                        }
                    }
                    else if (e.type == EventType.MouseDrag && _draggedFacility != null)
                    {
                        int targetX = gridX - _dragOffsetX;
                        int targetY = gridY - _dragOffsetY;

                        targetX = Mathf.Clamp(targetX, 0, builder.gridWidth - _draggedFacility.facilityData.width);
                        targetY = Mathf.Clamp(targetY, 0, builder.gridHeight - _draggedFacility.facilityData.height);

                        bool canMove = true;
                        if (builder.tiles != null)
                        {
                            for (int i = 0; i < _draggedFacility.facilityData.width; i++)
                            {
                                for (int j = 0; j < _draggedFacility.facilityData.height; j++)
                                {
                                    int checkX = targetX + i;
                                    int checkY = targetY + j;
                                    bool isOwnTile = (checkX >= _draggedFacility.gridX && checkX < _draggedFacility.gridX + _draggedFacility.facilityData.width) &&
                                                     (checkY >= _draggedFacility.gridY && checkY < _draggedFacility.gridY + _draggedFacility.facilityData.height);
                                    
                                    if (!isOwnTile)
                                    {
                                        GridTileData tile = builder.tiles.Find(t => t.x == checkX && t.y == checkY);
                                        if (tile == null || !tile.isWalkable)
                                        {
                                            canMove = false;
                                            break;
                                        }
                                    }
                                }
                                if (!canMove) break;
                            }
                        }

                        if (canMove)
                        {
                            if (_draggedFacility.gridX != targetX || _draggedFacility.gridY != targetY)
                            {
                                Undo.RecordObject(_draggedFacility, "Move Facility");
                                _draggedFacility.gridX = targetX;
                                _draggedFacility.gridY = targetY;
                                if (PrefabUtility.IsPartOfPrefabInstance(_draggedFacility))
                                    PrefabUtility.RecordPrefabInstancePropertyModifications(_draggedFacility);
                                else
                                    EditorUtility.SetDirty(_draggedFacility);
                            }
                        }
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDown && e.button == 0 && e.modifiers == EventModifiers.None)
                    {
                        SceneFacilityInfo existing = GetFacilityAt(builder, gridX, gridY);
                        if (existing != null)
                        {
                            GUIUtility.hotControl = controlID;
                            _draggedFacility = existing;
                            _dragOffsetX = gridX - existing.gridX;
                            _dragOffsetY = gridY - existing.gridY;
                            e.Use();
                        }
                        else if (data != null)
                        {
                            if (gridX >= 0 && gridX <= builder.gridWidth - data.width && 
                                gridY >= 0 && gridY <= builder.gridHeight - data.height)
                            {
                                GUIUtility.hotControl = controlID;
                                bool canPlace = true;
                                if (builder.tiles != null)
                                {
                                    for (int i = 0; i < data.width; i++)
                                    {
                                        for (int j = 0; j < data.height; j++)
                                        {
                                            GridTileData tile = builder.tiles.Find(t => t.x == gridX + i && t.y == gridY + j);
                                            if (tile == null || !tile.isWalkable) { canPlace = false; break; }
                                        }
                                        if (!canPlace) break;
                                    }
                                }
                                
                                if (canPlace)
                                {
                                    PlaceFacility(builder, data, gridX, gridY);
                                }
                                else
                                {
                                    Debug.LogWarning("해당 위치에는 이미 다른 구조물이 있거나 배치할 수 없는 타일입니다.");
                                }
                                e.Use();
                            }
                        }
                    }
                    else if (e.type == EventType.MouseDown && e.button == 1 && e.modifiers == EventModifiers.None)
                    {
                        GUIUtility.hotControl = controlID;
                        EraseFacilityAt(builder, gridX, gridY);
                        e.Use();
                    }
                    
                    if (e.type == EventType.Repaint && data != null && _draggedFacility == null)
                    {
                        if (gridX >= 0 && gridX <= builder.gridWidth - data.width && 
                            gridY >= 0 && gridY <= builder.gridHeight - data.height)
                        {
                            bool canPlace = true;
                            if (builder.tiles != null)
                            {
                                for (int i = 0; i < data.width; i++)
                                {
                                    for (int j = 0; j < data.height; j++)
                                    {
                                        GridTileData tile = builder.tiles.Find(t => t.x == gridX + i && t.y == gridY + j);
                                        if (tile == null || !tile.isWalkable) { canPlace = false; break; }
                                    }
                                    if (!canPlace) break;
                                }
                            }

                            Vector3 previewPos = builder.transform.position + new Vector3(
                                gridX * builder.nodeSize, gridY * builder.nodeSize, 0f);
                                
                            Vector3 previewSize = new Vector3(
                                data.width * builder.nodeSize, data.height * builder.nodeSize, 0f);

                            Color faceColor = canPlace ? new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.2f);
                            Color outlineColor = canPlace ? Color.green : Color.red;

                            Handles.color = faceColor;
                            Vector3[] verts = new Vector3[] {
                                previewPos, previewPos + new Vector3(previewSize.x, 0, 0),
                                previewPos + new Vector3(previewSize.x, previewSize.y, 0), previewPos + new Vector3(0, previewSize.y, 0)
                            };
                            Handles.DrawSolidRectangleWithOutline(verts, faceColor, outlineColor);
                        }
                    }
                }
                else if (builder.currentMode == BuilderMode.Erase)
                {
                    if (gridX >= 0 && gridX < builder.gridWidth && gridY >= 0 && gridY < builder.gridHeight)
                    {
                        Vector3 previewPos = builder.transform.position + new Vector3(
                            gridX * builder.nodeSize,
                            gridY * builder.nodeSize,
                            0f);
                        Vector3 previewSize = new Vector3(builder.nodeSize, builder.nodeSize, 0f);

                        Handles.color = new Color(1f, 0f, 0f, 0.2f);
                        Vector3[] verts = new Vector3[]
                        {
                            previewPos,
                            previewPos + new Vector3(previewSize.x, 0, 0),
                            previewPos + new Vector3(previewSize.x, previewSize.y, 0),
                            previewPos + new Vector3(0, previewSize.y, 0)
                        };
                        Handles.DrawSolidRectangleWithOutline(verts, new Color(1, 0, 0, 0.2f), Color.red);

                        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                        {
                            GUIUtility.hotControl = controlID;
                            EraseFacilityAt(builder, gridX, gridY);
                            e.Use();
                        }
                    }
                }
                else if (builder.currentMode == BuilderMode.SetWalkable || builder.currentMode == BuilderMode.SetUnwalkable)
                {
                    if (gridX >= 0 && gridX < builder.gridWidth && gridY >= 0 && gridY < builder.gridHeight)
                    {
                        Vector3 previewPos = builder.transform.position + new Vector3(
                            gridX * builder.nodeSize,
                            gridY * builder.nodeSize,
                            0f);
                        Vector3 previewSize = new Vector3(builder.nodeSize, builder.nodeSize, 0f);

                        Color previewColor = builder.currentMode == BuilderMode.SetWalkable ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
                        
                        Vector3[] verts = new Vector3[]
                        {
                            previewPos,
                            previewPos + new Vector3(previewSize.x, 0, 0),
                            previewPos + new Vector3(previewSize.x, previewSize.y, 0),
                            previewPos + new Vector3(0, previewSize.y, 0)
                        };
                        Handles.DrawSolidRectangleWithOutline(verts, previewColor, Color.white);

                        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                        {
                            bool setWalkable = builder.currentMode == BuilderMode.SetWalkable;
                            GridTileData tile = builder.tiles.Find(t => t.x == gridX && t.y == gridY);
                            if (tile != null)
                            {
                                if (tile.isWalkable != setWalkable)
                                {
                                    tile.isWalkable = setWalkable;
                                    EditorUtility.SetDirty(builder);
                                }
                            }
                            else if (builder.tiles != null)
                            {
                                // 타일이 없으면 생성
                                builder.tiles.Add(new GridTileData { x = gridX, y = gridY, isWalkable = setWalkable });
                                EditorUtility.SetDirty(builder);
                            }
                            e.Use();
                        }
                    }
                }
                else if (builder.currentMode == BuilderMode.InsertGrid)
                {
                    if (gridX >= 0 && gridX < builder.gridWidth && gridY >= 0 && gridY < builder.gridHeight)
                    {
                        // 분할선 시각적 표시
                        Handles.color = new Color(0f, 1f, 1f, 1f); // 굵은 청록색 선
                        Vector3 splitOrigin = builder.transform.position + new Vector3((gridX + 1) * builder.nodeSize, (gridY + 1) * builder.nodeSize, 0f);
                        
                        if (builder.insertRightAmount != 0)
                            Handles.DrawLine(splitOrigin - new Vector3(0, builder.gridHeight * builder.nodeSize, 0), splitOrigin + new Vector3(0, builder.gridHeight * builder.nodeSize, 0), 3f);
                        if (builder.insertTopAmount != 0)
                            Handles.DrawLine(splitOrigin - new Vector3(builder.gridWidth * builder.nodeSize, 0, 0), splitOrigin + new Vector3(builder.gridWidth * builder.nodeSize, 0, 0), 3f);

                        // 클릭 시 삽입 실행
                        if (e.type == EventType.MouseDown && e.button == 0)
                        {
                            GUIUtility.hotControl = controlID;
                            builder.InsertGrid(gridX, gridY, builder.insertRightAmount, builder.insertTopAmount);
                            e.Use();
                        }
                    }
                }
            }

            // 선택 모드(None)가 아닐 때만 씬의 다른 오브젝트 선택을 막음
            if (builder.currentMode != BuilderMode.None && e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlID);
            }
        }

        private void PlaceFacility(SceneFacilityBuilder builder, FacilityData data, int x, int y)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            Transform mainParent = builder.facilityParent != null ? builder.facilityParent : builder.transform;
            string groupName = data.facilityType.ToString() + "_Parent";
            Transform groupParent = mainParent.Find(groupName);

            if (groupParent == null)
            {
                GameObject groupGo = new GameObject(groupName);
                groupParent = groupGo.transform;
                groupParent.SetParent(mainParent);
                groupParent.localPosition = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(groupGo, $"Create {groupName}");
            }

            // 중앙점 계산 (오브젝트의 중심)
            Vector3 center = builder.transform.position + new Vector3(
                x * builder.nodeSize + (data.width * builder.nodeSize) / 2f,
                y * builder.nodeSize + (data.height * builder.nodeSize) / 2f,
                0);

            // 프리팹 인스턴스화
            GameObject go;
            if (data.visualPrefab != null) {
                // 부모를 지정하여 생성하면 별도의 SetTransformParent 없이 Undo 가능
                go = (GameObject)PrefabUtility.InstantiatePrefab(data.visualPrefab, groupParent);
            } else {
                go = new GameObject(data.facilityName);
                go.transform.SetParent(groupParent);
            }
            
            // Undo 스택에 생성 등록
            Undo.RegisterCreatedObjectUndo(go, $"Place {data.facilityName}");

            go.transform.position = center + data.visualPosOffset;
            go.transform.localScale = data.visualScaleOffset;

            // SceneFacilityInfo 추가/수정하여 정보 저장
            SceneFacilityInfo info = go.GetComponent<SceneFacilityInfo>();
            if (info == null) 
            {
                info = Undo.AddComponent<SceneFacilityInfo>(go);
            }
            else
            {
                Undo.RecordObject(info, "Set Facility Data");
            }
            
            info.facilityData = data;
            info.gridX = x;
            info.gridY = y;
            
            if (PrefabUtility.IsPartOfPrefabInstance(info))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(info);
            }
            else
            {
                EditorUtility.SetDirty(info);
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        private SceneFacilityInfo GetFacilityAt(SceneFacilityBuilder builder, int x, int y)
        {
            Transform parent = builder.facilityParent != null ? builder.facilityParent : builder.transform;
            SceneFacilityInfo[] infos = parent.GetComponentsInChildren<SceneFacilityInfo>();
            foreach (var info in infos)
            {
                if (info.facilityData != null)
                {
                    if (x >= info.gridX && x < info.gridX + info.facilityData.width &&
                        y >= info.gridY && y < info.gridY + info.facilityData.height)
                    {
                        return info;
                    }
                }
            }
            return null;
        }

        private void EraseFacilityAt(SceneFacilityBuilder builder, int x, int y)
        {
            SceneFacilityInfo info = GetFacilityAt(builder, x, y);
            if (info != null)
            {
                Undo.DestroyObjectImmediate(info.gameObject);
            }
        }

        private void ClearAllFacilities(SceneFacilityBuilder builder)
        {
            Transform parent = builder.facilityParent != null ? builder.facilityParent : builder.transform;
            SceneFacilityInfo[] infos = parent.GetComponentsInChildren<SceneFacilityInfo>();
            if (infos.Length > 0)
            {
                if (EditorUtility.DisplayDialog("Clear All Facilities", "현재 배치된 모든 구조물을 지우시겠습니까?", "Yes", "No"))
                {
                    foreach (var info in infos)
                    {
                        Undo.DestroyObjectImmediate(info.gameObject);
                    }
                }
            }
        }

        private void ResyncAllFacilities(SceneFacilityBuilder builder)
        {
            if (!EditorUtility.DisplayDialog("재배치 동기화", "씬의 모든 구조물을 지우고, 현재의 데이터와 위치에 맞게 최신 프리팹으로 다시 배치하시겠습니까?", "네, 진행합니다", "취소"))
                return;

            var placed = FindObjectsOfType<SceneFacilityInfo>();
            var backups = new System.Collections.Generic.List<(FacilityData data, int x, int y)>();
            
            foreach (var info in placed)
            {
                if (info.facilityData != null)
                {
                    backups.Add((info.facilityData, info.gridX, info.gridY));
                }
                Undo.DestroyObjectImmediate(info.gameObject);
            }

            foreach (var backup in backups)
            {
                PlaceFacility(builder, backup.data, backup.x, backup.y);
            }

            EditorUtility.SetDirty(builder);
            Debug.Log($"총 {backups.Count}개의 구조물을 최신 프리팹 데이터와 위치 오프셋을 적용하여 재배치했습니다.");
        }
    }
}
