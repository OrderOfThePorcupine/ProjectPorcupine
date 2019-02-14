using System;
using UnityEngine;

public class PositionTween
{
    private RectTransform rect;
    private Vector2 target;
    private float duration;
    private Action onEnded;
    private float elapsedTime;

    public PositionTween(RectTransform rect, Vector2 target, float duration)
    {
        this.rect = rect;
        this.target = target;
        this.duration = duration;
    }

    public PositionTween(RectTransform rect, Vector2 target, float duration, Action onEnded) : this(rect, target, duration)
    {
        this.onEnded = onEnded;
    }

    public void Start()
    {
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
            if (onEnded != null) onEnded();
            rect.anchoredPosition = target;
        }
    }
}
