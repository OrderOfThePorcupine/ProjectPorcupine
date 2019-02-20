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

public class MenuLeft : MonoBehaviour
{
    // This is the parent of the menus.
    private Transform parent;

    public GameObject CurrentlyOpen { get; private set; }

    // Use this for initialization
    public void Start()
    {
        parent = this.gameObject.transform;

        GameObject constructionMenu = AddMenu("ConstructionMenu", "ConstructionMenu", typeof(ConstructionMenu));
        GameObject orderMenu = AddMenu("OrderMenu", "ConstructionMenu", typeof(OrderMenu));

        GameMenuManager.Instance.AddMenuItem("menu_construction", () => OnMenuButtonClicked(constructionMenu), 0);
        GameMenuManager.Instance.AddMenuItem("menu_orders", () => OnMenuButtonClicked(orderMenu), 1);
    }

    public void OpenMenuCurrentMenu()
    {
        CurrentlyOpen.GetComponent<SlideAnimation>().Show();

        WorldController.Instance.SoundController.OnButtonSFX();

        if (CurrentlyOpen.name == "ConstructionMenu" || CurrentlyOpen.name == "OrderMenu")
        {
            WorldController.Instance.SpawnInventoryController.SetUIVisibility(false);
        }
    }

    public void CloseCurrentMenu()
    {
        if (CurrentlyOpen != null)
        {
            CurrentlyOpen.GetComponent<SlideAnimation>().Hide();

            if (CurrentlyOpen.name == "ConstructionMenu" || CurrentlyOpen.name == "OrderMenu")
            {
                WorldController.Instance.SpawnInventoryController.SetUIVisibility(SettingsKeyHolder.DeveloperMode);
                WorldController.Instance.BuildModeController.Building = false;
            }

            WorldController.Instance.SoundController.OnButtonSFX();
        }
    }

    // Use this function to add all the menus.
    private GameObject AddMenu(string menuName, string prefabName, System.Type useComponent)
    {
        GameObject tempGoObj;
        tempGoObj = (GameObject)Instantiate(Resources.Load("UI/MenuLeft/" + prefabName));
        tempGoObj.name = menuName;
        tempGoObj.transform.SetParent(parent, false);

        tempGoObj.AddComponent(useComponent);

        return tempGoObj;
    }

    private void OnMenuButtonClicked(GameObject menu)
    {
        if (CurrentlyOpen == menu)
        {
            CurrentlyOpen = null;
        }
        else
        {
            CurrentlyOpen = menu;
            OpenMenuCurrentMenu();
        }
    }
}
