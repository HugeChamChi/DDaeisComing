using UnityEngine;
using Bathhouse.Save;
using Bathhouse.Data;
using Bathhouse.Managers;
using Cysharp.Threading.Tasks;

/// <summary>
/// 전역에서 접근 가능한 정적 매니저 클래스.
/// RuntimeInitializeOnLoadMethod를 통해 씬 로드 전 자동으로 @GlobalManagers 오브젝트를 생성합니다.
/// </summary>
public static class GlobalManagers
{
    public static SaveManager Save { get; private set; }
    public static DataManager Data { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        // 기존에 이미 생성된 것이 있다면 중복 생성 방지
        if (GameObject.Find("@GlobalManagers") != null) return;

        GameObject go = new GameObject("@GlobalManagers");
        Object.DontDestroyOnLoad(go);

        Save = go.AddComponent<SaveManager>();
        Data = go.AddComponent<DataManager>();

        InitAsync().Forget();
    }

    private static async UniTaskVoid InitAsync()
    {
        if (Save != null) await Save.InitAsync();
        if (Data != null) Data.InitializeData();
    }
}
