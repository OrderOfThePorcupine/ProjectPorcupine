#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using UnityEngine;

public class MenuLeft : MonoBehaviour
{
    // This is the parent of the menus.
    private Transform parent;
    private GameMenu currentlyOpen;

    // Use this for initialization
    public void Start()
    {
        parent = this.gameObject.transform;

        GameMenu constructionMenu = AddMenu("ConstructionMenu", "ConstructionMenu", typeof(ConstructionMenu));
        GameMenu orderMenu = AddMenu("OrderMenu", "ConstructionMenu", typeof(OrderMenu));

        GameMenuManager.Instance.AddMenuItem("menu_construction", () => OnMenuButtonClicked(constructionMenu), 0);
        GameMenuManager.Instance.AddMenuItem("menu_orders", () => OnMenuButtonClicked(orderMenu), 1);
    }

    public void OpenMenu(GameMenu menu)
    {
        // So that we don't have two menus open at the same time
        CloseCurrentMenu();

        menu.Open();

        WorldController.Instance.SoundController.OnButtonSFX();
        
        WorldController.Instance.SpawnInventoryController.SetUIVisibility(false);

        currentlyOpen = menu;
    }

    public void CloseCurrentMenu()
    {
        if (currentlyOpen != null)
        {
            currentlyOpen.Close();
            
            WorldController.Instance.SpawnInventoryController.SetUIVisibility(SettingsKeyHolder.DeveloperMode);
            WorldController.Instance.BuildModeController.Building = false;
        }

        currentlyOpen = null;
    }

    // Use this function to add all the menus.
    private GameMenu AddMenu(string menuName, string prefabName, System.Type useComponent)
    {
        GameObject tempGoObj;
        tempGoObj = (GameObject)Instantiate(Resources.Load("UI/MenuLeft/" + prefabName));
        tempGoObj.name = menuName;
        tempGoObj.transform.SetParent(parent, false);

        tempGoObj.AddComponent(useComponent);

        return tempGoObj.GetComponent<GameMenu>();
    }

    private void OnMenuButtonClicked(GameMenu menu)
    {
        if (currentlyOpen == menu)
        {
            CloseCurrentMenu();
        }
        else
        {
            OpenMenu(menu);
        }
    }
}
