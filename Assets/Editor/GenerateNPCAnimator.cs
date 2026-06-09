#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class GenerateNPCAnimator : Editor
{
    [MenuItem("Tools/Generate NPC Animator (Play 방식)")]
    public static void Generate()
    {
        string folder = "Assets/Animations";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }

        string path = folder + "/NPC_Animator.controller";
        
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = controller.layers[0].stateMachine;

        // 트랜지션 없이 각 State 들만 독립적으로 생성합니다.

        // 1. Idle 상태군
        var idleDown = sm.AddState("Idle_Down", new Vector3(250, 100, 0));
        var idleUp = sm.AddState("Idle_Up", new Vector3(250, 150, 0));
        var idleLeftRight = sm.AddState("Idle_Left_Right", new Vector3(250, 200, 0));

        // 2. Move 상태군
        var moveDown = sm.AddState("Move_Down", new Vector3(500, 100, 0));
        var moveUp = sm.AddState("Move_Up", new Vector3(500, 150, 0));
        var moveLeftRight = sm.AddState("Move_Left_Right", new Vector3(500, 200, 0));

        // 3. 예시 상호작용 상태
        var actionBathState = sm.AddState("Action_Bath", new Vector3(375, 300, 0));
        var actionSaunaState = sm.AddState("Action_Sauna", new Vector3(375, 350, 0));

        // 기본 상태 설정
        sm.defaultState = idleDown;

        AssetDatabase.SaveAssets();
        Debug.Log("<b>[성공]</b> " + path + " 에 애니메이터 컨트롤러가 생성되었습니다! (선 연결 없는 Play 전용 방식입니다)");
    }
}
#endif
