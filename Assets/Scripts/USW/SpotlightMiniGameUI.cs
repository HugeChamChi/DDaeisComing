using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace Bathhouse.MiniGames
{
    /// <summary>
    /// 손전등(스포트라이트) 미니게임.
    /// 화면이 어둡고, 손가락으로 누른 채 드래그하면 그 원 안쪽만 밝아진다.
    /// 원 안에 숨겨진 수건이 드러나고, 원이 수건에 닿으면 수건이 사라진다.
    /// 모든 수건을 찾으면 성공. 손을 떼면 전체가 다시 어두워진다.
    /// </summary>
    public class SpotlightMiniGameUI : MiniGameBase,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Spotlight References")]
        [Tooltip("화면을 덮는 어두운 오버레이 이미지 (Bathhouse/UISpotlight 머티리얼 적용). Raycast Target 켜둘 것")]
        public Image overlayImage;

        [Tooltip("수건이 생성될 부모. 비워두면 오버레이의 부모를 사용한다 (수건은 오버레이보다 뒤에 그려져야 함)")]
        public RectTransform towelParent;

        [Tooltip("남은 개수 / 안내 텍스트 (선택)")]
        public TMP_Text infoText;

        [Header("Spotlight Settings")]
        [Tooltip("밝아지는 원의 반지름 (오버레이 높이 대비 비율)")]
        [Range(0.03f, 0.4f)] public float revealRadius = 0.12f;

        [Tooltip("원 가장자리 부드러움")]
        [Range(0f, 0.1f)] public float softness = 0.03f;

        [Header("Towel Settings")]
        [Tooltip("수건 스프라이트 (이 중에서 랜덤으로 선택)")]
        public Sprite[] towelSprites;

        [Tooltip("수건 한 변 크기 (px)")]
        public float towelSize = 120f;

        [Tooltip("기본 수건 개수 (난이도에 따라 증가)")]
        public int baseTowelCount = 3;

        [Tooltip("수건이 사라질 때 페이드 시간")]
        public float fadeDuration = 0.25f;

        private Material _mat;
        private RectTransform _overlayRect;
        private readonly List<RectTransform> _towels = new();
        private int _remaining;
        private bool _isPressing;

        // 셰이더 프로퍼티 ID 캐시
        private static readonly int TouchUVId = Shader.PropertyToID("_TouchUV");
        private static readonly int RadiusId  = Shader.PropertyToID("_Radius");
        private static readonly int SoftId    = Shader.PropertyToID("_Softness");
        private static readonly int AspectId  = Shader.PropertyToID("_Aspect");
        private static readonly int ActiveId  = Shader.PropertyToID("_Active");

        protected override void Start()
        {
            // base.Start()가 gamePanel(=자기 자신일 수 있음)을 비활성화하므로
            // 머티리얼 캐싱을 먼저 해둔다.
            CacheMaterial();
            base.Start();
        }

        private void CacheMaterial()
        {
            if (overlayImage == null) return;
            _overlayRect = overlayImage.rectTransform;

            // 머티리얼 인스턴스화 (프로젝트 원본 에셋 오염 방지)
            _mat = Instantiate(overlayImage.material);
            overlayImage.material = _mat;
        }

        protected override void OnGameStarted()
        {
            if (_mat == null) CacheMaterial();
            if (_mat == null)
            {
                Debug.LogError("[SpotlightMiniGame] overlayImage 또는 머티리얼이 비어있습니다.");
                return;
            }

            _isPressing = false;
            SetActiveUniform(false);
            _mat.SetFloat(RadiusId, revealRadius);
            _mat.SetFloat(SoftId, softness);

            SpawnTowels();
            UpdateInfo();

            Debug.Log($"[SpotlightMiniGame] 시작! 수건 {_remaining}개, 난이도: {_finalDifficultyWeight:F2}");
        }

        private void SpawnTowels()
        {
            foreach (var t in _towels)
                if (t != null) Destroy(t.gameObject);
            _towels.Clear();

            RectTransform parent = towelParent != null ? towelParent : _overlayRect.parent as RectTransform;
            Rect rect = _overlayRect.rect;

            // 난이도에 따라 개수 증가
            int count = baseTowelCount + Mathf.RoundToInt((_finalDifficultyWeight - 1f) * 2f);
            count = Mathf.Max(1, count);

            float margin = towelSize * 0.6f;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Towel_{i}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));

                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.sizeDelta = new Vector2(towelSize, towelSize);

                var img = go.GetComponent<Image>();
                img.raycastTarget = false; // 드래그를 막지 않도록
                if (towelSprites != null && towelSprites.Length > 0)
                    img.sprite = towelSprites[Random.Range(0, towelSprites.Length)];

                float x = Random.Range(rect.xMin + margin, rect.xMax - margin);
                float y = Random.Range(rect.yMin + margin, rect.yMax - margin);
                rt.anchoredPosition = new Vector2(x, y);

                _towels.Add(rt);
            }

            // 오버레이를 맨 앞으로 보내 수건을 덮고,
            // 안내 텍스트는 오버레이보다 더 앞으로 올려 항상 보이게 한다.
            _overlayRect.SetAsLastSibling();
            if (infoText != null && infoText.transform.parent == _overlayRect.parent)
                infoText.transform.SetAsLastSibling();

            _remaining = _towels.Count;
        }

        public void OnPointerDown(PointerEventData e)
        {
            _isPressing = true;
            SetActiveUniform(true);
            UpdateSpotlight(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_isPressing) return;
            UpdateSpotlight(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            _isPressing = false;
            SetActiveUniform(false); // 손 떼면 전체 어둠
        }

        private void SetActiveUniform(bool active)
        {
            if (_mat != null) _mat.SetFloat(ActiveId, active ? 1f : 0f);
        }

        private void UpdateSpotlight(PointerEventData e)
        {
            if (_overlayRect == null || _mat == null) return;

            // 스크린 좌표 → 오버레이 로컬 좌표
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _overlayRect, e.position, e.pressEventCamera, out Vector2 local))
                return;

            Rect rect = _overlayRect.rect;
            float u = (local.x - rect.xMin) / rect.width;
            float v = (local.y - rect.yMin) / rect.height;

            _mat.SetVector(TouchUVId, new Vector4(u, v, 0, 0));
            _mat.SetFloat(AspectId, rect.width / rect.height);

            CheckTowels(local, rect);
        }

        private void CheckTowels(Vector2 touchLocal, Rect rect)
        {
            // 반지름(높이 비율) → 로컬 픽셀
            float radiusPx = revealRadius * rect.height;

            for (int i = _towels.Count - 1; i >= 0; i--)
            {
                var t = _towels[i];
                if (t == null) { _towels.RemoveAt(i); continue; }

                // 수건 위치를 오버레이 로컬 좌표로 변환해 같은 공간에서 비교
                Vector2 towelLocal = _overlayRect.InverseTransformPoint(t.position);

                if (Vector2.Distance(touchLocal, towelLocal) <= radiusPx)
                    RemoveTowel(i, t);
            }
        }

        private void RemoveTowel(int index, RectTransform towel)
        {
            _towels.RemoveAt(index);
            _remaining--;
            UpdateInfo();

            var cg = towel.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.DOFade(0f, fadeDuration).OnComplete(() => Destroy(towel.gameObject));
            else
                Destroy(towel.gameObject);

            if (_remaining <= 0)
                GameSuccess();
        }

        private void UpdateInfo()
        {
            if (infoText != null)
                infoText.text = _remaining > 0 ? $"수건을 찾아라! ({_remaining})" : "클리어!";
        }
    }
}
