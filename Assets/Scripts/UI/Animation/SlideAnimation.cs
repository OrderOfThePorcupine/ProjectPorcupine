#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using UnityEngine;

namespace ProjectPorcupine.UI.Animation
{
    public class SlideAnimation : MenuAnimation
    {
        [SerializeField]
        [Tooltip("Slide in direction.")]
        private SlideDirection direction;

        private PositionTween slideIn;
        private PositionTween slideOut;

        public override void Show()
        {
            base.Show();
            slideOut.Stop();
            slideIn.Start();
        }

        public override void Hide()
        {
            slideIn.Stop();
            slideOut.Start();
        }

        private void Start()
        {
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            Vector2 outPosition = OutPosition(rect, direction);
            Vector2 inPosition = rect.anchoredPosition;
            rect.anchoredPosition = outPosition;

            slideIn = new PositionTween(rect, inPosition);
            slideOut = new PositionTween(rect, outPosition, () => gameObject.SetActive(false));
        }

        private Vector2 OutPosition(RectTransform rect, SlideDirection direction)
        {
            float x = rect.anchoredPosition.x;
            float y = rect.anchoredPosition.y;
            float width = rect.sizeDelta.x;
            float height = rect.sizeDelta.y;

            switch (direction)
            {
                case SlideDirection.Right:
                    return new Vector2(x - width, y);
                case SlideDirection.Left:
                    return new Vector2(x + width, y);
                case SlideDirection.Down:
                    return new Vector2(x, y - height);
                case SlideDirection.Up:
                    return new Vector2(x, y + height);
                default:
                    throw new System.InvalidOperationException(string.Format("{0} not found.", direction));
            }
        }
    }
}