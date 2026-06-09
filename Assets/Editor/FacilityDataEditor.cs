using UnityEngine;
using UnityEditor;
using Bathhouse.Data;
using System.Collections.Generic;

namespace Bathhouse.Editor
{
    [CustomEditor(typeof(FacilityData))]
    public class FacilityDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FacilityData data = (FacilityData)target;

            GUILayout.Space(20);
            GUILayout.Label("Grid Position Visualizer", EditorStyles.boldLabel);
            
            GUILayout.Label("파란색(F): 구조물 본체\n노란색(O): 상호작용 위치 (클릭하여 토글)", EditorStyles.helpBox);

            int padding = 2;
            int gridW = data.width + padding * 2;
            int gridH = data.height + padding * 2;

            float cellSize = 30f;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();

            List<Vector2Int> interOffsets = new List<Vector2Int>(data.interactionOffsets ?? new Vector2Int[0]);
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

                    Color btnColor = Color.white;
                    string label = "";

                    if (isInteraction)
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
                        if (isInteraction) interOffsets.Remove(currentOffset);
                        else interOffsets.Add(currentOffset);
                        changed = true;
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
                EditorUtility.SetDirty(data);
            }
        }
    }
}
