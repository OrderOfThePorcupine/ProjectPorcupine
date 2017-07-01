﻿#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; protected set; }

    // Use this for initialization.
    public void OnEnable()
    {
        new PrototypeManager();
        new FunctionsManager();
    }

    public void Start()
    {
        Instance = this;

        new SpriteManager();
        new AudioManager();

        TimeManager.Instance.IsPaused = true;

        // Create a Background.
        GameObject backgroundGO = new GameObject("Background");
        backgroundGO.AddComponent<SpriteRenderer>().sprite = SpriteManager.GetRandomSprite("Background");

        GameObject canvas = GameObject.Find("Canvas");

        // Create a Title.
        GameObject title = (GameObject)Instantiate(Resources.Load("UI/TitleMainMenu"));
        title.transform.SetParent(canvas.transform, false);
        title.SetActive(true);

        // Display Main Menu.
        GameObject mainMenu = (GameObject)Instantiate(Resources.Load("UI/MainMenu"));
        mainMenu.transform.SetParent(canvas.transform, false);
        mainMenu.SetActive(true);
    }
}