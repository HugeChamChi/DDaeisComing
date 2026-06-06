using UnityEngine;
using UnityEditor;
using Bathhouse.Tools;
using Bathhouse.Data;

namespace Bathhouse.EditorScripts
{
    [CustomEditor(typeof(SceneFacilityBuilder))]
    public class SceneFacilityBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SceneFacilityBuilder builder = (SceneFacilityBuilder)target;

            GUILayout.Space(10);
            GUILayout.Label("Builder Mode", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(builder.currentMode == BuilderMode.Placement, "Placement", "Button"))
                builder.currentMode = BuilderMode.Placement;
            if (GUILayout.Toggle(builder.currentMode == BuilderMode.Erase, "Erase", "Button"))
                builder.currentMode = BuilderMode.Erase;
            if (GUILayout.Toggle(builder.currentMode == BuilderMode.SetWalkable, "Set Walkable", "Button"))
                builder.currentMode = BuilderMode.SetWalkable;
            if (GUILayout.Toggle(builder.currentMode == BuilderMode.SetUnwalkable, "Set Unwalkable", "Button"))
                builder.currentMode = BuilderMode.SetUnwalkable;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Initialize Tiles"))
            {
                builder.InitializeTiles();
                EditorUtility.SetDirty(builder);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Clear All Facilities"))
            {
                ClearAllFacilities(builder);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Available Facilities", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("아래 버튼을 클릭하여 구조물을 선택한 후, 씬 뷰(Scene View)의 원하는 위치를 좌클릭하여 배치하세요.", MessageType.Info);

            string[] guids = AssetDatabase.FindAssets("t:FacilityData");
            if (guids.Length > 0)
            {
                GUILayout.BeginVertical("box");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    FacilityData fd = AssetDatabase.LoadAssetAtPath<FacilityData>(path);
                    if (fd != null)
                    {
                        if (GUILayout.Button(fd.name))
                        {
                            builder.selectedFacility = fd;
                            builder.currentMode = BuilderMode.Placement; // 자동으로 배치 모드로 전환
                            EditorUtility.SetDirty(builder);
                            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Focus();
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.Label("No FacilityData found in the project.");
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
                    if (data != null)
                    {
                        // 그리드 범위 안에 마우스가 있을 때만 프리뷰 표시
                        if (gridX >= 0 && gridX <= builder.gridWidth - data.width && 
                            gridY >= 0 && gridY <= builder.gridHeight - data.height)
                        {
                            // 프리뷰 박스 (초록색) 그리기
                            Vector3 previewPos = builder.transform.position + new Vector3(
                                gridX * builder.nodeSize,
                                gridY * builder.nodeSize,
                                0f);
                                
                            Vector3 previewSize = new Vector3(
                                data.width * builder.nodeSize,
                                data.height * builder.nodeSize,
                                0f);

                            Handles.color = new Color(0f, 1f, 0f, 0.2f);
                            // 사각형을 채워서 그림
                            Vector3[] verts = new Vector3[]
                            {
                                previewPos,
                                previewPos + new Vector3(previewSize.x, 0, 0),
                                previewPos + new Vector3(previewSize.x, previewSize.y, 0),
                                previewPos + new Vector3(0, previewSize.y, 0)
                            };
                            Handles.DrawSolidRectangleWithOutline(verts, new Color(0, 1, 0, 0.2f), Color.green);

                            // 씬 뷰에서 좌클릭(버튼 0)하면 구조물 생성
                            if (e.type == EventType.MouseDown && e.button == 0 && e.modifiers == EventModifiers.None)
                            {
                                GUIUtility.hotControl = controlID;
                                PlaceFacility(builder, data, gridX, gridY);
                                e.Use(); // 이벤트 소모 (선택 변경 방지)
                            }
                            // 우클릭(버튼 1)하면 구조물 삭제
                            else if (e.type == EventType.MouseDown && e.button == 1 && e.modifiers == EventModifiers.None)
                            {
                                GUIUtility.hotControl = controlID;
                                EraseFacilityAt(builder, gridX, gridY);
                                e.Use();
                            }
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
            }

            // 레이아웃 이벤트일 때 패시브 컨트롤 추가 (클릭 시 다른 오브젝트 선택 방지용)
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlID);
            }
        }

        private void PlaceFacility(SceneFacilityBuilder builder, FacilityData data, int x, int y)
        {
            // 중앙점 계산 (오브젝트의 중심)
            Vector3 center = builder.transform.position + new Vector3(
                x * builder.nodeSize + (data.width * builder.nodeSize) / 2f,
                y * builder.nodeSize + (data.height * builder.nodeSize) / 2f,
                0);

            // 프리팹 인스턴스화
            GameObject go;
            if (data.visualPrefab != null) {
                go = (GameObject)PrefabUtility.InstantiatePrefab(data.visualPrefab);
            } else {
                go = new GameObject(data.facilityName);
            }
            go.transform.position = center + data.visualOffset;

            // SceneFacilityInfo 추가/수정하여 정보 저장
            SceneFacilityInfo info = go.GetComponent<SceneFacilityInfo>();
            if (info == null) info = go.AddComponent<SceneFacilityInfo>();
            
            info.facilityData = data;
            info.gridX = x;
            info.gridY = y;

            if (builder.facilityParent != null)
            {
                go.transform.SetParent(builder.facilityParent);
            }
            else
            {
                go.transform.SetParent(builder.transform);
            }

            // Undo 스택에 등록하여 Ctrl+Z 지원
            Undo.RegisterCreatedObjectUndo(go, $"Place {data.facilityName}");
        }

        private void EraseFacilityAt(SceneFacilityBuilder builder, int x, int y)
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
                        Undo.DestroyObjectImmediate(info.gameObject);
                        break;
                    }
                }
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
    }
}
