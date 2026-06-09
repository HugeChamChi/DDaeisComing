using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ApplySortingFix : EditorWindow
{
    [MenuItem("Tools/Fix All Sprite Sorting (자동 수정 실행)")]
    public static void FixAllSprites()
    {
        // 1. 그래픽스 세팅 설정 (Custom Axis Y=1)
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = new Vector3(0, 1, 0);

        // 2. 모든 프리팹 검색
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
                bool modified = false;
                
                foreach (SpriteRenderer sr in renderers)
                {
                    if (sr.sortingOrder != 0 || sr.spriteSortPoint != SpriteSortPoint.Pivot)
                    {
                        sr.sortingOrder = 0;
                        sr.spriteSortPoint = SpriteSortPoint.Pivot;
                        modified = true;
                    }
                }
                
                if (modified)
                {
                    EditorUtility.SetDirty(prefab);
                    count++;
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        
        Debug.Log($"<b>[자동 수정 완료]</b> 총 {count}개의 프리팹에서 Sprite Order를 0으로, Sort Point를 Pivot으로 통일했습니다!\n" +
                  "이제 씬에서 게임을 실행하거나 오브젝트를 배치하면 Y축을 기준으로 완벽하게 앞뒤 정렬이 이루어집니다.\n" +
                  "(만약 특정 이미지의 기준점(Pivot)이 엉뚱한 곳에 있다면, Sprite Editor에서 그 이미지의 Pivot만 Bottom으로 내려주시면 됩니다.)");
    }
}
