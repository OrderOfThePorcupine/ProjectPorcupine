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
    public class SlideAnimation : MonoBehaviour
    {
        private PositionTween slideIn;
        private PositionTween slideOut;
        private Vector2 outPosition;
        private Vector2 inPosition;

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

        private void Awake()
        {
            RectTransform rect = GetComponent<RectTransform>();
            ///Calculates only left to right position.
            ///TODO: Add All Slide modes(LeftToRight, RightToLeft etc).
            outPosition = new Vector2(rect.anchoredPosition.x - rect.sizeDelta.x, rect.anchoredPosition.y);
            inPosition = rect.anchoredPosition;
            rect.anchoredPosition = outPosition;

            slideIn = new PositionTween(rect, inPosition, 5);
            slideOut = new PositionTween(rect, outPosition, 5, () => gameObject.SetActive(false));
        }
    }
}