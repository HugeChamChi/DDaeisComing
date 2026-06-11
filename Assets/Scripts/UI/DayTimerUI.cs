using UnityEngine;
using TMPro;
using DG.Tweening;
using Bathhouse.Managers;

namespace Bathhouse.UI
{
    public class DayTimerUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtTimer;
        
        [Header("Settings")]
        [Tooltip("몇 초 남았을 때 강조를 시작할지")]
        [SerializeField] private float highlightThreshold = 10f;
        
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.red;

        private bool _isHighlighting = false;
        private Sequence _highlightSequence;

        private void Start()
        {
            if (txtTimer != null)
            {
                txtTimer.color = normalColor;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            float remaining = GameManager.Instance.DayDuration - GameManager.Instance.CurrentDayTime;
            if (remaining < 0f) remaining = 0f;

            if (txtTimer != null)
            {
                int min = Mathf.FloorToInt(remaining / 60f);
                int sec = Mathf.FloorToInt(remaining % 60f);
                
                txtTimer.text = $"{min:00}:{sec:00}";

                if (remaining <= highlightThreshold && remaining > 0f)
                {
                    if (!_isHighlighting)
                    {
                        StartHighlight();
                    }
                }
                else
                {
                    if (_isHighlighting)
                    {
                        StopHighlight();
                    }
                }
            }
        }

        private void StartHighlight()
        {
            _isHighlighting = true;
            if (txtTimer == null) return;

            txtTimer.color = highlightColor;
            
            // DOTween 애니메이션: 텍스트가 심장 박동처럼 커졌다 작아짐을 반복
            _highlightSequence = DOTween.Sequence();
            _highlightSequence.Append(txtTimer.transform.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack))
                              .Append(txtTimer.transform.DOScale(1f, 0.5f).SetEase(Ease.InOutSine))
                              .SetLoops(-1, LoopType.Restart); // 무한 반복
        }

        private void StopHighlight()
        {
            _isHighlighting = false;
            
            if (_highlightSequence != null)
            {
                _highlightSequence.Kill();
                _highlightSequence = null;
            }

            if (txtTimer != null)
            {
                txtTimer.transform.localScale = Vector3.one;
                txtTimer.color = normalColor;
            }
        }

        private void OnDestroy()
        {
            if (_highlightSequence != null)
            {
                _highlightSequence.Kill();
            }
        }
    }
}
