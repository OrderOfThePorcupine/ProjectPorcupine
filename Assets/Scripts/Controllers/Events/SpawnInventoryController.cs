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
using System.Linq;
using ProjectPorcupine.Localization;
using ProjectPorcupine.Mouse;
using UnityEngine;
using UnityEngine.UI;

[MoonSharp.Interpreter.MoonSharpUserData]
public class SpawnInventoryController : IMouseHandler
{
    private GameObject spawnUI;

    public SpawnInventoryController()
    {
        CreateSpawnUI();
        CreateInventoryEntries();
    }

    public string InventoryToBuild { get; protected set; }

    public int AmountToCreate { get; protected set; }

    public Inventory CurrentInventory { get; protected set; }

    public bool DisableDragging
    {
        get
        {
            return true;
        }
    }

    public MouseHandlerCallbacks CallbacksEnabled
    {
        get
        {
            return MouseHandlerCallbacks.HANDLE_CLICK | MouseHandlerCallbacks.HANDLE_TOOLTIP | MouseHandlerCallbacks.HANDLE_DRAG_VISUAL;
        }
    }

    public void HideUI()
    {
        spawnUI.SetActive(false);
    }

    public void ShowUI()
    {
        spawnUI.SetActive(true);
    }

    public void SetUIVisibility(bool visibility)
    {
        spawnUI.SetActive(visibility);
    }

    public void SpawnInventory(Tile t)
    {
        // If the user clicks outside the game area t may be null.
        if (t == null)
        {
            return;
        }

        // You can't spawn on occupied tiles
        if (t.Furniture != null)
        {
            return;
        }

        if (t.Inventory == null || t.Inventory.Type == InventoryToBuild)
        {
            World.Current.InventoryManager.PlaceInventory(t, CurrentInventory);
            CurrentInventory = new Inventory(InventoryToBuild, AmountToCreate);
        }
    }

    public void HandleClick(Vector2 position, int mouseKey)
    {
        if (mouseKey == 0 && SettingsKeyHolder.DeveloperMode)
        {
            Tile t = WorldController.Instance.GetTileAtWorldCoord(position);
            SpawnInventory(t);
        }
    }

    public void HandleTooltip(Vector2 position, MouseCursor cursor, bool isDragging)
    {
        cursor.Reset();
        cursor.DisplayCursorInfo(TextAnchor.MiddleRight, AmountToCreate + "x " + InventoryToBuild, MouseCursor.TextColor, false);
    }

    public List<GameObject> HandleDragVisual(MouseController.DragParameters parameters, Transform parent)
    {
        List<GameObject> objects = new List<GameObject>();

        if (parameters.EndX != parameters.StartX)
        {
            // We should NEVER reach here, the disable dragging means that this should NEVER be true
            throw new ArgumentException("Parameters Start X/Y values should Equal End X/Y values, this is taken care by the DisableDragging = true property.");
        }

        Tile t = WorldController.Instance.World.GetTileAt(parameters.StartX, parameters.StartY, WorldController.Instance.CameraController.CurrentLayer);
        if (t != null)
        {
            // Show generic visuals
            GameObject go = new GameObject();
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(parameters.StartX, parameters.StartY);

            if (t.Furniture == null && (t.Inventory == null || t.Inventory.Type == InventoryToBuild) && t.Type != TileType.Empty)
            {
                InventorySpriteController.SetSprite(go, CurrentInventory).color = Color.green; // = new Color(0.5f, 1f, 0.5f, 0.25f);
            }
            else
            {
                InventorySpriteController.SetSprite(go, CurrentInventory).color = Color.red; // new Color(1f, 0.5f, 0.5f, 0.25f);
            }

            objects.Add(go);
        }

        return objects;
    }

    #region InvalidOperations

    /// <summary>
    /// NOT IMPLEMENTED BY SPAWN INVENTORY CONTROLLER.  Will throw on call.
    /// NOT MEANT TO BE CALLED.
    /// </summary>
    public void HandleDragFinished(MouseController.DragParameters parameters)
    {
        throw new InvalidOperationException("Not supported by this class");
    }

    /// <summary>
    /// NOT IMPLEMENTED BY SPAWN INVENTORY CONTROLLER.  Will throw on call.
    /// NOT MEANT TO BE CALLED.
    /// </summary>
    public Vector3 HandlePlacingPosition(Vector3 position)
    {
        throw new InvalidOperationException("Not supported by this class");
    }
    #endregion

    private void CreateSpawnUI()
    {
        spawnUI = new GameObject()
        {
            name = "Spawn Inventory UI",
            layer = LayerMask.NameToLayer("UI")
        };

        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        spawnUI.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = spawnUI.AddComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0, 0.5f);
        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(0, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0, 0);

        spawnUI.AddComponent<Image>();

        VerticalLayoutGroup vlg = spawnUI.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 0;

        ContentSizeFitter csf = spawnUI.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.MinSize;
        csf.verticalFit = ContentSizeFitter.FitMode.MinSize;
    }

    private void CreateInventoryEntries()
    {
        foreach (Inventory inventory in PrototypeManager.Inventory.Values.OrderByDescending(inv => inv.Category))
        {
            GameObject inventorySlot_go = new GameObject()
            {
                name = "Slot - " + inventory.Type,
                layer = LayerMask.NameToLayer("UI")
            };

            inventorySlot_go.transform.SetParent(spawnUI.transform);

            HorizontalLayoutGroup hlg = inventorySlot_go.AddComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 2;

            inventorySlot_go.AddComponent<Image>();

            string localName = inventory.LocalizationName;

            GameObject textComponent = CreateTextComponent(inventorySlot_go, localName, TextAnchor.MiddleLeft);
            TextLocalizer textLocalizer = textComponent.AddComponent<TextLocalizer>();
            textLocalizer.formatValues = new string[0];
            CreateButtonComponents(inventorySlot_go, inventory, new int[] { 1, 20, 50 });

            LayoutElement layoutElement = inventorySlot_go.AddComponent<LayoutElement>();
            layoutElement.minWidth = 160;
            layoutElement.minHeight = 20;
        }
    }

    private void CreateButtonComponents(GameObject go, Inventory inventory, int[] amounts)
    {
        foreach (int amount in amounts)
        {
            GameObject button_go = new GameObject()
            {
                name = "Button",
                layer = LayerMask.NameToLayer("UI")
            };

            button_go.AddComponent<Image>();

            RectTransform rectTransform = button_go.GetComponent<RectTransform>();
            rectTransform.SetParent(go.transform);

            Button button = button_go.AddComponent<Button>();
            CreateTextComponent(button_go, amount.ToString(), TextAnchor.MiddleCenter);

            LayoutElement layoutElement = button_go.AddComponent<LayoutElement>();
            layoutElement.minWidth = 20;
            layoutElement.minHeight = 20;

            int localAmount = amount;

            button.onClick.AddListener(
                () => OnButtonClick(inventory.Type, localAmount));
        }
    }

    private GameObject CreateTextComponent(GameObject go, string invName, TextAnchor textAnchor)
    {
        GameObject text_go = new GameObject()
        {
            name = "Text",
            layer = LayerMask.NameToLayer("UI")
        };

        RectTransform rectTransform = text_go.AddComponent<RectTransform>();
        rectTransform.SetParent(go.transform);
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchorMin = Vector2.zero;

        Text text = text_go.AddComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        text.alignment = textAnchor;
        text.color = Color.black;
        text.text = invName;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        LayoutElement layoutElement = text_go.AddComponent<LayoutElement>();
        layoutElement.minWidth = 100;
        layoutElement.minHeight = 20;

        return text_go;
    }

    private void OnButtonClick(string invName, int amount)
    {
        InventoryToBuild = invName;
        AmountToCreate = amount;
        CurrentInventory = new Inventory(InventoryToBuild, AmountToCreate);
        WorldController.Instance.MouseController.ChangeMouseMode(MouseMode.INVENTORY);
    }
}