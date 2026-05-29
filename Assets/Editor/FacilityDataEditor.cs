using UnityEngine;
using UnityEditor;
using Bathhouse.Data;
using System.Collections.Generic;

namespace Bathhouse.Editor
{
    [CustomEditor(typeof(FacilityData))]
    public class FacilityDataEditor : UnityEditor.Editor
    {
        private enum PaintMode { Interaction, Usage }
        private PaintMode _paintMode = PaintMode.Interaction;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FacilityData data = (FacilityData)target;

            GUILayout.Space(20);
            GUILayout.Label("Grid Position Visualizer", EditorStyles.boldLabel);
            
            _paintMode = (PaintMode)GUILayout.Toolbar((int)_paintMode, new string[] { "Paint Interaction (O)", "Paint Usage Slots (U)" });

            GUILayout.Label("파란색(F): 구조물 본체\n노란색(O): 상호작용 위치\n초록색(U): 머무는 자리\n하늘색(O+U): 상호작용과 머무는 자리가 겹칠 때", EditorStyles.helpBox);

            int padding = 2;
            int gridW = data.width + padding * 2;
            int gridH = data.height + padding * 2;

            float cellSize = 30f;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();

            List<Vector2Int> interOffsets = new List<Vector2Int>(data.interactionOffsets ?? new Vector2Int[0]);
            List<Vector2Int> useOffsets = new List<Vector2Int>(data.usageOffsets ?? new Vector2Int[0]);
            bool changed = false;

            for (int y = gridH - 1; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < gridW; x++)
                {
                    int localX = x - padding;
                    int localY = y - padding;

                    bool isFacility = localX >= 0 && localX < data.width && localY >= 0 && localY < data.height;
                    Vector2Int currentOffset = new Vector2Int(localX, localY);
                    
                    bool isInteraction = interOffsets.Contains(currentOffset);
                    bool isUsage = useOffsets.Contains(currentOffset);

                    Color btnColor = Color.white;
                    string label = "";

                    if (isInteraction && isUsage)
                    {
                        btnColor = Color.cyan;
                        label = "O+U";
                    }
                    else if (isUsage)
                    {
                        btnColor = Color.green;
                        label = "U";
                    }
                    else if (isInteraction)
                    {
                        btnColor = Color.yellow;
                        label = "O";
                    }
                    else if (isFacility)
                    {
                        btnColor = new Color(0.2f, 0.5f, 1f, 0.9f);
                        label = "F";
                    }

                    GUI.backgroundColor = btnColor;
                    if (GUILayout.Button(label, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        if (_paintMode == PaintMode.Interaction)
                        {
                            if (isInteraction) interOffsets.Remove(currentOffset);
                            else interOffsets.Add(currentOffset);
                            changed = true;
                        }
                        else if (_paintMode == PaintMode.Usage)
                        {
                            if (isUsage) useOffsets.Remove(currentOffset);
                            else useOffsets.Add(currentOffset);
                            changed = true;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (changed)
            {
                Undo.RecordObject(data, "Change Visualizer Offsets");
                data.interactionOffsets = interOffsets.ToArray();
                data.usageOffsets = useOffsets.ToArray();
                // 사용 슬롯 개수에 맞춰 최대 수용 인원을 자동 동기화할 수도 있습니다.
                // data.maxCapacity = Mathf.Max(1, data.usageOffsets.Length); 
                EditorUtility.SetDirty(data);
            }
        }
    }
}
