#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using Animation;
using UnityEngine;

public class TraderShipController : MonoBehaviour
{
    public bool TradeCompleted { get; set; }

    public Vector3 LeavingCoordinates { get; private set; }

    public Vector3 LandingCoordinates { get; private set; }

    public float Speed { get; private set; }

    public float DestinationReachedThreshold { get; private set; }

    public bool DestinationReached { get; private set; }

    public Trader Trader { get; private set; }

    public SpritenameAnimation AnimationIdle { get; private set; }

    public SpritenameAnimation AnimationFlying { get; private set; }

    public SpriteRenderer Renderer { get; private set; }

    public void Init(Vector3 leavingCoords, Vector3 landingCoords, float speed, Trader trader, SpritenameAnimation animationIdle, SpritenameAnimation animationFlying, SpriteRenderer renderer, float destinationReachedThreshold = 0.1f)
    {
        this.LeavingCoordinates = leavingCoords;
        this.LandingCoordinates = landingCoords;
        this.Speed = speed;
        this.DestinationReachedThreshold = destinationReachedThreshold;
        this.Trader = trader;
        this.AnimationIdle = animationIdle;
        this.AnimationFlying = animationFlying;
        this.Renderer = renderer;
    }

    public void FixedUpdate()
    {
        if (GameController.Instance.IsPaused)
        {
            return;
        }

        Vector3 destination = LandingCoordinates;

        if (DestinationReached && !TradeCompleted)
        {
            return;
        }

        if (TradeCompleted)
        {
            destination = LeavingCoordinates;
        }

        float distance = Vector3.Distance(transform.position, destination);

        if (distance > DestinationReachedThreshold * TimeManager.Instance.TimeScale)
        {
            // rotate the model
            Vector3 vectorToTarget = destination - transform.position;
            float angle = (Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg) - 90;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * Speed * TimeManager.Instance.TimeScale);

            // Direction to the next waypoint
            Vector3 dir = (destination - transform.position).normalized;
            dir *= Speed * Time.fixedDeltaTime * TimeManager.Instance.TimeScale;

            transform.position = transform.position + dir;
            AnimationFlying.Update(Time.fixedDeltaTime);
            ShowSprite(AnimationFlying.CurrentFrameName);
        }
        else
        {
            DestinationReached = true;
            if (TradeCompleted)
            {
                Destroy(this.gameObject);
            }
            else
            {
                GameController.Instance.CurrentSystem.TradeController.ShowTradeDialogBox(this);
                AnimationIdle.Update(Time.fixedDeltaTime);
                ShowSprite(AnimationIdle.CurrentFrameName);
            }
        }
    }

    private void ShowSprite(string spriteName)
    {
        if (Renderer != null)
        {
            Renderer.sprite = SpriteManager.GetSprite("Trader", spriteName);
        }
    }
}