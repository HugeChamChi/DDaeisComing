using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Bathhouse.Tools;
using Bathhouse.Test;
using Bathhouse.Managers;

namespace Bathhouse.EditorScripts
{
    public class EditSceneSetup
    {
        [MenuItem("Tools/Bathhouse/Create Edit Scene")]
        public static void CreateEditScene()
        {
            // 새 씬 생성
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // 1. 시설물 관리를 위한 부모 오브젝트
            GameObject facilityParent = new GameObject("Facilities");

            // 2. SceneFacilityBuilder 세팅
            GameObject builderObj = new GameObject("SceneFacilityBuilder");
            SceneFacilityBuilder builder = builderObj.AddComponent<SceneFacilityBuilder>();
            builder.facilityParent = facilityParent.transform;
            
            // 3. Grid 및 Tilemap 세팅 (바닥, 벽체 등)
            GameObject gridObj = new GameObject("Grid");
            UnityEngine.Grid grid = gridObj.AddComponent<UnityEngine.Grid>();
            
            GameObject floorObj = new GameObject("FloorTilemap");
            floorObj.transform.SetParent(gridObj.transform);
            Tilemap floorTilemap = floorObj.AddComponent<Tilemap>();
            floorObj.AddComponent<TilemapRenderer>();
            
            GameObject obstacleObj = new GameObject("ObstacleTilemap");
            obstacleObj.transform.SetParent(gridObj.transform);
            Tilemap obstacleTilemap = obstacleObj.AddComponent<Tilemap>();
            obstacleObj.AddComponent<TilemapRenderer>();

            // 4. NPC Spawner (빈 껍데기 세팅, 인스펙터에서 Prefab/Data 할당 필요)
            GameObject spawnerObj = new GameObject("NPCSpawner");
            NPCSpawner spawner = spawnerObj.AddComponent<NPCSpawner>();

            // 5. SceneTestRunner 세팅
            GameObject runnerObj = new GameObject("SceneTestRunner");
            SceneTestRunner runner = runnerObj.AddComponent<SceneTestRunner>();
            runner.facilityBuilder = builder;
            runner.npcSpawner = spawner;

            // 씬 저장 (Assets/Scenes 폴더가 없으면 생성)
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            
            string scenePath = "Assets/Scenes/EditScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            
            Debug.Log($"[EditSceneSetup] {scenePath} 에 성공적으로 EditScene을 생성하고 기본 세팅을 마쳤습니다.");
        }
    }
}
