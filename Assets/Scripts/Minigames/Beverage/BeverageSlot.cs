using UnityEngine;
using UnityEngine.UI;

namespace DDaeisComing.Minigames.Beverage
{
    public class BeverageSlot : MonoBehaviour
    {
        public bool isFilled = true;
        public RectTransform rectTransform { get; private set; }
        
        [SerializeField] private Image beverageImage;
        [SerializeField] private Sprite emptySprite;
        [SerializeField] private Sprite filledSprite;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetEmpty()
        {
            isFilled = false;
            if (beverageImage != null)
            {
                beverageImage.sprite = emptySprite;
                
                if (emptySprite == null)
                {
                    // Sprite가 없으면 완전 투명하게 만들어 비어 보이게 처리
                    beverageImage.color = new Color(1, 1, 1, 0);
                }
                else
                {
                    // Sprite가 있으면 보이게 처리
                    beverageImage.color = Color.white;
                }
            }
        }

        public void SetFilled()
        {
            isFilled = true;
            if (beverageImage != null && filledSprite != null)
            {
                beverageImage.sprite = filledSprite;
                beverageImage.color = Color.white;
            }
        }
    }
}
