#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using UnityEngine;

namespace ProjectPorcupine.UI.Animation
{
    public class PositionTween
    {
        private RectTransform rect;
        private Vector2 target;
        private float duration;
        private Action onEnded;
        private float elapsedTime;

        public PositionTween(RectTransform rect, Vector2 target)
        {
            this.rect = rect;
            this.target = target;
        }

        public PositionTween(RectTransform rect, Vector2 target, Action onEnded) : this(rect, target)
        {
            this.onEnded = onEnded;
        }

        public void Start()
        {
            float distance = Vector2.Distance(rect.anchoredPosition, target);
            float speed = SettingsKeyHolder.UIAnimationSpeed * 100;
            duration = distance / speed;
            elapsedTime = 0;
            TimeManager.Instance.EveryFrame += Update;
        }

        public void Stop()
        {
            TimeManager.Instance.EveryFrame -= Update;
        }

        private void Update(float time)
        {
            if (elapsedTime < duration)
            {
                elapsedTime += time;
                float percentage = Mathf.Clamp01(elapsedTime / duration);
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, target, percentage);
            }
            else
            {
                Stop();

                if (onEnded != null)
                {
                    onEnded();
                }

                rect.anchoredPosition = target;
            }
        }
    }
}