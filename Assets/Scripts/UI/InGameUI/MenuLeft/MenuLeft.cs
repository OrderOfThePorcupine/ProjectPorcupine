#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Collections.Generic;
using ProjectPorcupine.UI.Animation;
using UnityEngine;

public class MenuLeft : MonoBehaviour
{
    // This is the parent of the menus.
    private Transform parent;

    private List<GameObject> menus;

    private GameObject currentlyOpen;

    // Use this for initialization
    public void Start()
    {
        parent = gameObject.transform;

        GameObject constructionMenu = AddMenu("ConstructionMenu", typeof(ConstructionMenu));
        GameObject orderMenu = AddMenu("OrderMenu", typeof(OrderMenu));

        AddMenuButton(constructionMenu, "menu_construction", 0);
        AddMenuButton(orderMenu, "menu_orders", 1);

        menus = new List<GameObject>
        {
            constructionMenu,
            orderMenu
        };
    }

    public void OpenMenu(GameObject menu)
    {
        menu.GetComponent<SlideAnimation>().Show();

        WorldController.Instance.SoundController.OnButtonSFX();

        if (menus.Contains(currentlyOpen))
        {
            WorldController.Instance.SpawnInventoryController.SetUIVisibility(false);
        }
    }

    public void CloseMenu()
    {
        if (currentlyOpen != null)
        {
            currentlyOpen.GetComponent<SlideAnimation>().Hide();

            if (menus.Contains(currentlyOpen))
            {
                WorldController.Instance.SpawnInventoryController.SetUIVisibility(SettingsKeyHolder.DeveloperMode);
                BuildModeController.Instance.Building = false;
            }

            WorldController.Instance.SoundController.OnButtonSFX();
        }
    }

    // Use this function to add all the menus.
    private GameObject AddMenu(string menuName, Type useComponent)
    {
        GameObject tempGoObj;
        tempGoObj = (GameObject)Instantiate(Resources.Load("UI/MenuLeft/" + "ConstructionMenu"));
        tempGoObj.name = menuName;
        tempGoObj.transform.SetParent(parent, false);
        tempGoObj.AddComponent(useComponent);

        return tempGoObj;
    }

    private void AddMenuButton(GameObject menu, string key, int position)
    {
        Action onButtonClicked = () => OnButtonClicked(menu);
        GameMenuManager.Instance.AddMenuItem(key, onButtonClicked, position);
    }

    private void OnButtonClicked(GameObject menu)
    {
        if (currentlyOpen && currentlyOpen.name.Equals(menu.name))
        {
            currentlyOpen = null;
        }
        else
        {
            currentlyOpen = menu;
            OpenMenu(menu);
        }
    }
}