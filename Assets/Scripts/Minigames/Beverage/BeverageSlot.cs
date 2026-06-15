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
            if (beverageImage != null && emptySprite != null)
            {
                beverageImage.sprite = emptySprite;
                beverageImage.color = new Color(1, 1, 1, 0); // Hide or show empty sprite properly, assuming emptySprite might be null or transparent
                
                if (emptySprite != null)
                {
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
