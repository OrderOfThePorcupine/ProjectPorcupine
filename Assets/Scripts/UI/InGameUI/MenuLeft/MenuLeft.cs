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

public class MenuLeft : MonoBehaviour
{
    // This is the parent of the menus.
    private Transform parent;

    public GameObject CurrentlyOpen { get; private set; }

    // Use this for initialization
    public void Start()
    {
        parent = gameObject.transform;

        GameObject constructionMenu = AddMenu("ConstructionMenu", "ConstructionMenu", typeof(ConstructionMenu));
        GameObject orderMenu = AddMenu("OrderMenu", "ConstructionMenu", typeof(OrderMenu));

        AddMenuButton(constructionMenu, "menu_construction", 0);
        AddMenuButton(orderMenu, "menu_orders", 1);
    }

    public void OpenCurrentlyMenu()
    {
        CurrentlyOpen.GetComponent<IGameMenu>().Open();

        WorldController.Instance.SoundController.OnButtonSFX();

        if (CurrentlyOpen.name == "ConstructionMenu" || CurrentlyOpen.name == "OrderMenu")
        {
            WorldController.Instance.SpawnInventoryController.SetUIVisibility(false);
        }
    }

    public void CloseCurrentlyMenu()
    {
        if (CurrentlyOpen != null)
        {
            CurrentlyOpen.GetComponent<IGameMenu>().Close();

            if (CurrentlyOpen.name == "ConstructionMenu" || CurrentlyOpen.name == "OrderMenu")
            {
                WorldController.Instance.SpawnInventoryController.SetUIVisibility(SettingsKeyHolder.DeveloperMode);
                WorldController.Instance.BuildModeController.Building = false;
            }

            WorldController.Instance.SoundController.OnButtonSFX();
        }
    }

    // Use this function to add all the menus.
    private GameObject AddMenu(string menuName, string prefabName, Type useComponent)
    {
        GameObject tempGoObj;
        tempGoObj = (GameObject)Instantiate(Resources.Load("UI/MenuLeft/" + prefabName));
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
        if (CurrentlyOpen && CurrentlyOpen.name.Equals(menu.name))
        {
            CurrentlyOpen = null;
        }
        else
        {
            CurrentlyOpen = menu;
            OpenCurrentlyMenu();
        }
    }
}