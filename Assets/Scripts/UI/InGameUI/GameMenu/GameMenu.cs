#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using ProjectPorcupine.UI.Animation;
using UnityEngine;

public class GameMenu : MonoBehaviour
{
    private new MenuAnimation animation;

    protected virtual void Start()
    {
        animation = GetComponent<MenuAnimation>();
        gameObject.SetActive(false);
    }

    public virtual void Open()
    {
        WorldController.Instance.SoundController.OnButtonSFX();
        animation.Show();
    }

    public virtual void Close()
    {
        WorldController.Instance.SoundController.OnButtonSFX();
        animation.Hide();
    }
}