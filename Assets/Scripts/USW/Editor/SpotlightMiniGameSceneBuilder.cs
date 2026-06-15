using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Bathhouse.MiniGames;
using Bathhouse.DebugTools;

namespace Bathhouse.EditorTools
{
    /// <summary>
    /// 스포트라이트 미니게임 테스트용 UI를 메뉴 한 번으로 자동 구성한다.
    /// Tools > MiniGames > Build Spotlight Test Setup
    /// </summary>
    public static class SpotlightMiniGameSceneBuilder
    {
        private const string ShaderName   = "Bathhouse/UISpotlight";
        private const string MaterialPath = "Assets/Shaders/M_UISpotlight.mat";
        private const string TowelFolder  = "Assets/Imports/Sprites/Facility/Towel";

        [MenuItem("Tools/MiniGames/Build Spotlight Test Setup")]
        public static void Build()
        {
            // 1. Material 준비 (없으면 셰이더로 생성)
            Material mat = GetOrCreateMaterial();
            if (mat == null) return;

            // 하나의 Undo 그룹으로 묶는다 (Ctrl+Z 한 번에 전부 취소)
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            // 2. EventSystem 보장
            GameObject eventSystemGO = null;
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                eventSystemGO = new GameObject("EventSystem",
                    typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // 3. Canvas (모바일 세로 기준)
            var canvasGO = new GameObject("SpotlightTestCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // 4. GamePanel (전체 스트레치)
            var panel = CreateFullStretch("GamePanel", canvasGO.transform);

            // 5. TowelLayer (수건이 생성될 레이어, 오버레이보다 뒤)
            var towelLayer = CreateFullStretch("TowelLayer", panel.transform);

            // 6. DarkOverlay (어두운 오버레이 + 머티리얼)
            var overlay = CreateFullStretch("DarkOverlay", panel.transform);
            var overlayImg = overlay.gameObject.AddComponent<Image>();
            overlayImg.color = Color.white;
            overlayImg.material = mat;
            overlayImg.raycastTarget = true; // 드래그 입력 받음

            // 7. InfoText (상단 안내)
            var infoGO = new GameObject("InfoText", typeof(RectTransform));
            var infoRT = infoGO.GetComponent<RectTransform>();
            infoRT.SetParent(panel.transform, false);
            infoRT.anchorMin = new Vector2(0.5f, 1f);
            infoRT.anchorMax = new Vector2(0.5f, 1f);
            infoRT.pivot = new Vector2(0.5f, 1f);
            infoRT.anchoredPosition = new Vector2(0, -120);
            infoRT.sizeDelta = new Vector2(600, 100);
            var infoText = infoGO.AddComponent<TextMeshProUGUI>();
            infoText.text = "수건을 찾아라!";
            infoText.fontSize = 48;
            infoText.alignment = TextAlignmentOptions.Center;
            infoText.raycastTarget = false;

            // 8. SpotlightMiniGameUI 부착 + 연결
            var game = panel.gameObject.AddComponent<SpotlightMiniGameUI>();
            game.gamePanel = panel.gameObject;
            game.overlayImage = overlayImg;
            game.towelParent = towelLayer;
            game.infoText = infoText;
            game.towelSprites = LoadTowelSprites();

            // 9. 테스터 오브젝트
            var testerGO = new GameObject("SpotlightTester");
            var tester = testerGO.AddComponent<SpotlightMiniGameTester>();
            tester.miniGame = game;

            // 10. 계층을 모두 만든 뒤, 마지막에 한 번만 Undo 등록 (루트만 등록해도 자식까지 원자적으로 취소됨)
            if (eventSystemGO != null)
                Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create Spotlight Test Setup");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Spotlight Test Setup");
            Undo.RegisterCreatedObjectUndo(testerGO, "Create Spotlight Test Setup");
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeObject = canvasGO;
            Debug.Log("[SpotlightBuilder] 구성 완료! ▶ Play 후 마우스로 누른 채 드래그하세요. " +
                      "(타임아웃이 빠르면 MiniGameData를 비워두거나 baseTimeLimit를 크게)");
        }

        private static Material GetOrCreateMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[SpotlightBuilder] 셰이더 '{ShaderName}' 를 찾지 못했습니다. " +
                               "UISpotlight.shader 가 임포트됐는지 확인하세요.");
                return null;
            }

            var mat = new Material(shader);
            mat.SetColor("_Color", new Color(0f, 0f, 0f, 0.96f));
            AssetDatabase.CreateAsset(mat, MaterialPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static Sprite[] LoadTowelSprites()
        {
            var list = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < 16; i++)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>($"{TowelFolder}/Towel_{i}.png");
                if (sp != null) list.Add(sp);
            }
            if (list.Count == 0)
                Debug.LogWarning($"[SpotlightBuilder] {TowelFolder} 에서 Towel 스프라이트를 못 찾음. 수동 할당 필요.");
            return list.ToArray();
        }

        private static RectTransform CreateFullStretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }
    }
}
