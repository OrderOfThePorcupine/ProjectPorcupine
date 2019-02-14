using UnityEngine;

namespace ProjectPorcupine.UI.Animation
{
    public class SlideAnimation : MonoBehaviour
    {
        [SerializeField] private float duration;
        private PositionTween slideIn;
        private PositionTween slideOut;
        private Vector2 outPosition;
        private Vector2 inPosition;

        private void Awake()
        {
            RectTransform rect = GetComponent<RectTransform>();
            //Calculates only left to right position.
            //TODO: Add All Slide modes(LeftToRight, RightToLeft etc).
            outPosition = new Vector2(rect.anchoredPosition.x - rect.sizeDelta.x, rect.anchoredPosition.y);
            inPosition = rect.anchoredPosition;
            rect.anchoredPosition = outPosition;

            slideIn = new PositionTween(rect, inPosition, duration);
            slideOut = new PositionTween(rect, outPosition, duration, () => gameObject.SetActive(false));
        }

        public void Show()
        {
            gameObject.SetActive(true);
            slideOut.Stop();
            slideIn.Start();
        }

        public void Hide()
        {
            slideIn.Stop();
            slideOut.Start();
        }
    }
}