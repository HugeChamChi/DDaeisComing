using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Setup2DSorting : Editor
{
    [MenuItem("Tools/Setup 2D Sorting (탑다운 정렬 세팅)")]
    public static void Setup()
    {
        // 커스텀 축을 기준으로 정렬하도록 설정 (2D 탑다운의 정석)
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        
        // Y값이 클수록(위쪽에 있을수록) 먼저 렌더링되도록 (즉, 뒤로 가도록)
        GraphicsSettings.transparencySortAxis = new Vector3(0, 1, 0);

        Debug.Log("<b>[성공]</b> 2D 탑다운을 위한 Y축 정렬(Transparency Sort) 세팅이 완료되었습니다! 이제 Update 스크립트 없이도 Y좌표에 따라 자동으로 Sorting됩니다.");
    }
}
