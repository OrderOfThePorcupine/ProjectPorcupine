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
using System.Text;
using ProjectPorcupine.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MouseController
{
    private GameObject cursorParent;
    private GameObject circleCursorPrefab;
    private GameObject furnitureParent;

    private BuildModeController buildModeController;
    private FurnitureSpriteController furnitureSpriteController;
    private UtilitySpriteController utilitySpriteController;
    private ContextMenu contextMenu;
    private MouseCursor mouseCursor;

    private Vector3 lastFramePosition;      // Our last frame position
    private Vector3 dragStartPosition;      // The world-position start of our left-mouse drag operation.

    private Dictionary<TooltipMode, Action<Vector2, MouseCursor>> toolTipHandlers;
    private string tooltip = null;
    private TooltipMode currentTooltipMode = TooltipMode.DEFAULT;

    private int validPostionCount;
    private int invalidPositionCount;

    private float panningThreshold = .015f;
    private Vector3 panningMouseStart = Vector3.zero;
    private MouseMode currentMode = MouseMode.SELECT;
    private List<GameObject> dragPreviewGameObjects;

    /// <summary>
    /// Construct a new mouse controller.
    /// </summary>
    /// <param name="buildModeController"> A reference to the build mode controller. </param>
    /// <param name="furnitureSpriteController"> A reference to the furniture sprite controller. </param>
    /// <param name="utilitySpriteController"> A reference to the utility sprite controller. </param>
    /// <param name="cursorSpriteObject"> A reference to the sprite object to create. </param>
    public MouseController(BuildModeController buildModeController, FurnitureSpriteController furnitureSpriteController, UtilitySpriteController utilitySpriteController, GameObject cursorSpriteObject)
    {
        buildModeController.SetMouseController(this);
        this.buildModeController = buildModeController;
        this.furnitureSpriteController = furnitureSpriteController;
        this.utilitySpriteController = utilitySpriteController;
        circleCursorPrefab = cursorSpriteObject;
        contextMenu = GameObject.FindObjectOfType<ContextMenu>();
        dragPreviewGameObjects = new List<GameObject>();
        cursorParent = new GameObject("Cursor");

        /* Build the canvas for the cursor to sit in */
        Canvas cursor_canvas = cursorParent.AddComponent<Canvas>();
        cursor_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cursor_canvas.worldCamera = Camera.main;
        cursor_canvas.sortingLayerName = "TileUI";
        cursor_canvas.referencePixelsPerUnit = 411.1f;

        RectTransform rt = cursorParent.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 200);

        CanvasScaler cs = cursorParent.AddComponent<CanvasScaler>();
        cs.dynamicPixelsPerUnit = 100.6f;
        cs.referencePixelsPerUnit = 411.1f;

        mouseCursor = new MouseCursor();
        mouseCursor.BuildCursor().transform.SetParent(cursorParent.transform);
        furnitureParent = new GameObject("Furniture Preview Sprites");
        toolTipHandlers = new Dictionary<TooltipMode, Action<Vector2, MouseCursor>>()
        {
            { TooltipMode.DEFAULT, GetTooltipNormalMode },
            { TooltipMode.BUILD, GetTooltipBuildMode },
            { TooltipMode.UI, GetTooltipUIMode },
        };

        TimeManager.Instance.EveryFrame += (time) => Update();
    }

    private enum MouseMode
    {
        SELECT,
        BUILD,
        SPAWN_INVENTORY
    }

    private enum TooltipMode
    {
        DEFAULT,
        BUILD,
        UI
    }

    /// <summary>
    /// Is the player dragging the mouse?.
    /// </summary>
    public bool IsDragging { get; private set; }

    /// <summary>
    /// Is the player panning the mouse?.
    /// </summary>
    public bool IsPanning { get; private set; }

    /// <summary>
    /// Current frame position.
    /// </summary>
    public Vector3 CurrentFramePosition { get; private set; }

    /// <summary>
    /// Current position placing objects.
    /// </summary>
    public Vector3 CurrentPlacingPosition { get; private set; }

    /// <summary>
    /// What is currently selected.
    /// </summary>
    public SelectionInfo Selection { get; private set; }

    /// <summary>
    /// Start UI Mode.
    /// </summary>
    /// <param name="tooltip"> The tooltip to display. </param>
    public void StartUIMode(string tooltip)
    {
        currentTooltipMode = TooltipMode.UI;
        this.tooltip = tooltip;
        mouseCursor.ForceShow = true;
    }

    /// <summary>
    /// Begin building mode.
    /// </summary>
    public void StartBuildMode()
    {
        currentMode = MouseMode.BUILD;
        currentTooltipMode = TooltipMode.BUILD;
        this.tooltip = null;
        mouseCursor.ForceShow = false;
    }

    /// <summary>
    /// Begin spawning mode.
    /// </summary>
    public void StartSpawnMode()
    {
        currentMode = MouseMode.SPAWN_INVENTORY;
        this.tooltip = null;
        mouseCursor.ForceShow = false;
    }

    /// <summary>
    /// Stop UI mode.
    /// </summary>
    public void ClearUIMode()
    {
        currentTooltipMode = currentMode == MouseMode.BUILD ? TooltipMode.BUILD : TooltipMode.DEFAULT;
        this.tooltip = null;
        mouseCursor.ForceShow = false;
    }

    /// <summary>
    /// Stop the mouse from dragging.
    /// </summary>
    /// <param name="changeMode"> Change modes back to defaults. </param>
    public void StopDragging(bool changeMode = false)
    {
        IsDragging = false;
        if (changeMode)
        {
            currentMode = MouseMode.SELECT;
            currentTooltipMode = TooltipMode.DEFAULT;
            tooltip = null;
            mouseCursor.ForceShow = false;
        }
    }

    private void Update()
    {
        UpdateCurrentFramePosition();

        CalculatePlacingPosition();
        CheckModeChanges();
        CheckIfContextMenuActivated();

        mouseCursor.Update();
        UpdateDragging();
        UpdateCameraMovement();
        UpdateSelection();

        // Tooltip handling
        if (toolTipHandlers.ContainsKey(currentTooltipMode))
        {
            toolTipHandlers[currentTooltipMode](CurrentFramePosition, mouseCursor);
        }

        if (SettingsKeyHolder.DeveloperMode)
        {
            UpdateSpawnClicking();
        }

        // Save the mouse position from this frame.
        // We don't use currFramePosition because we may have moved the camera.
        StoreFramePosition();
    }

    private void GetPlacementValidationCounts()
    {
        validPostionCount = invalidPositionCount = 0;

        for (int i = 0; i < dragPreviewGameObjects.Count; i++)
        {
            Tile t1 = GetTileUnderDrag(dragPreviewGameObjects[i].transform.position);
            if (World.Current.FurnitureManager.IsPlacementValid(BuildModeController.Instance.BuildModeType, t1) &&
               (t1.PendingBuildJobs == null || (t1.PendingBuildJobs != null && t1.PendingBuildJobs.Count == 0)))
            {
                validPostionCount++;
            }
            else
            {
                invalidPositionCount++;
            }
        }
    }

    private string GetCurrentBuildRequirements()
    {
        ProjectPorcupine.OrderActions.Build buildOrder = PrototypeManager.Furniture.Get(BuildModeController.Instance.BuildModeType).GetOrderAction<ProjectPorcupine.OrderActions.Build>();
        if (buildOrder != null)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in buildOrder.Inventory)
            {
                string requiredMaterialCount = (item.Amount * validPostionCount).ToString();
                sb.Append(string.Format("{0}x {1}", requiredMaterialCount, LocalizationTable.GetLocalization(item.Type)));
                if (buildOrder.Inventory.Count > 1)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        return "furnitureJobPrototypes is null";
    }

    private Tile GetTileUnderDrag(Vector3 gameObject_Position)
    {
        return WorldController.Instance.GetTileAtWorldCoord(gameObject_Position);
    }

    private void UpdateCurrentFramePosition()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        CurrentFramePosition = new Vector3(mousePos.x, mousePos.y, WorldController.Instance.CameraController.CurrentLayer);
    }

    private void GetTooltipUIMode(Vector2 pos, MouseCursor cursor)
    {
        cursor.Reset();

        if (string.IsNullOrEmpty(tooltip) == false)
        {
            Debug.LogWarning(cursor.ForceShow);
            cursor.DisplayCursorInfo(TextAnchor.MiddleRight, LocalizationTable.GetLocalization(tooltip), MouseCursor.DefaultTint);
        }
    }

    private void GetTooltipNormalMode(Vector2 pos, MouseCursor cursor)
    {
        cursor.Reset();
        Tile t = WorldController.Instance.GetTileAtWorldCoord(pos);
        if (t != null)
        {
            cursor.DisplayCursorInfo(TextAnchor.MiddleRight, string.Format("X:{0} Y:{1} Z:{2}", t.X.ToString(), t.Y.ToString(), t.Z.ToString()), MouseCursor.DefaultTint);
        }
    }

    private void GetTooltipBuildMode(Vector2 pos, MouseCursor cursor)
    {
        cursor.Reset();
        Tile t = WorldController.Instance.GetTileAtWorldCoord(pos);

        // Placing furniture object.
        if (buildModeController.BuildMode == BuildMode.FURNITURE)
        {
            cursor.DisplayCursorInfo(TextAnchor.LowerRight, LocalizationTable.GetLocalization(PrototypeManager.Furniture.Get(buildModeController.BuildModeType).GetName()), MouseCursor.DefaultTint);

            // Dragging and placing multiple furniture.
            if (t != null && IsDragging == true && dragPreviewGameObjects.Count > 1)
            {
                GetPlacementValidationCounts();
                cursor.DisplayCursorInfo(TextAnchor.UpperLeft, validPostionCount.ToString(), Color.green);
                cursor.DisplayCursorInfo(TextAnchor.UpperRight, invalidPositionCount.ToString(), Color.red);
                cursor.DisplayCursorInfo(TextAnchor.LowerLeft, GetCurrentBuildRequirements(), MouseCursor.DefaultTint);
            }
        }
        else if (buildModeController.BuildMode == BuildMode.FLOOR)
        {
            // Placing tiles and dragging.
            if (t != null && IsDragging == true && dragPreviewGameObjects.Count >= 1)
            {
                cursor.DisplayCursorInfo(TextAnchor.UpperLeft, dragPreviewGameObjects.Count.ToString(), MouseCursor.DefaultTint);
                cursor.DisplayCursorInfo(TextAnchor.LowerLeft, LocalizationTable.GetLocalization(buildModeController.GetFloorTile()), MouseCursor.DefaultTint);
            }
        }
    }

    private void CheckModeChanges()
    {
        if (Input.GetKeyUp(KeyCode.Escape) || Input.GetMouseButtonUp(1))
        {
            if (currentMode == MouseMode.BUILD && IsPanning == false)
            {
                StopDragging(true);
            }
            else if (currentMode == MouseMode.SPAWN_INVENTORY && IsPanning == false)
            {
                currentMode = MouseMode.SELECT;
                currentTooltipMode = TooltipMode.DEFAULT;
            }
        }
    }

    private void CheckIfContextMenuActivated()
    {
        if (Input.GetKeyUp(KeyCode.Escape) || Input.GetMouseButtonUp(1))
        {
            // Is the context also supposed to open on ESCAPE? That seems wrong
            if (currentMode == MouseMode.SELECT)
            {
                Tile t = WorldController.Instance.GetTileAtWorldCoord(CurrentFramePosition);
                if (contextMenu != null && t != null)
                {
                    if (IsPanning)
                    {
                        contextMenu.Close();
                    }
                    else if (contextMenu != null)
                    {
                        contextMenu.Open(t);
                    }
                }
            }
        }
    }

    private void StoreFramePosition()
    {
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = WorldController.Instance.CameraController.CurrentLayer;
    }

    private void CalculatePlacingPosition()
    {
        // If we are placing a multitile object we would like to modify the postilion where the mouse grabs it.
        if (currentMode == MouseMode.BUILD
            && buildModeController.BuildMode == BuildMode.FURNITURE
            && PrototypeManager.Furniture.Has(buildModeController.BuildModeType)
            && (PrototypeManager.Furniture.Get(buildModeController.BuildModeType).Width > 1
            || PrototypeManager.Furniture.Get(buildModeController.BuildModeType).Height > 1))
        {
            Furniture furnitureToBuild = PrototypeManager.Furniture.Get(buildModeController.BuildModeType).Clone();
            furnitureToBuild.SetRotation(buildModeController.CurrentPreviewRotation);
            Sprite sprite = furnitureSpriteController.GetSpriteForFurniture(furnitureToBuild.Type);

            // Use the center of the Furniture.
            CurrentPlacingPosition = CurrentFramePosition - ImageUtils.SpritePivotOffset(sprite, buildModeController.CurrentPreviewRotation);
        }
        else
        {
            CurrentPlacingPosition = CurrentFramePosition;
        }
    }

    private void UpdateSelection()
    {
        // This handles us left-clicking on furniture or characters to set a selection.
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Selection = null;
        }

        if (currentMode != MouseMode.SELECT)
        {
            return;
        }

        // If we're over a UI element, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (contextMenu != null)
            {
                contextMenu.Close();
            }

            // We just release the mouse button, so that's our queue to update our selection.
            Tile tileUnderMouse = WorldController.Instance.GetTileAtWorldCoord(CurrentFramePosition);

            if (tileUnderMouse == null)
            {
                // No valid tile under mouse.
                return;
            }

            if (Selection == null || Selection.Tile != tileUnderMouse)
            {
                if (Selection != null)
                {
                    Selection.GetSelectedStuff().IsSelected = false;
                }

                // We have just selected a brand new tile, reset the info.
                Selection = new SelectionInfo(tileUnderMouse);
                Selection.GetSelectedStuff().IsSelected = true;
            }
            else
            {
                // This is the same tile we already have selected, so cycle the subSelection to the next non-null item.
                // Not that the tile sub selection can NEVER be null, so we know we'll always find something.

                // Rebuild the array of possible sub-selection in case characters moved in or out of the tile.
                // [IsSelected] Set our last stuff to be not selected because were selecting the next stuff
                Selection.GetSelectedStuff().IsSelected = false;
                Selection.BuildStuffInTile();
                Selection.SelectNextStuff();
                Selection.GetSelectedStuff().IsSelected = true;
            }
        }
    }

    private void UpdateDragging()
    {
        CleanUpDragPreviews();

        if (currentMode != MouseMode.BUILD)
        {
            return;
        }

        UpdateIsDragging();

        if (IsDragging == false || buildModeController.IsObjectDraggable() == false)
        {
            dragStartPosition = CurrentPlacingPosition;
        }

        DragParameters dragParams = GetDragParameters();

        ShowPreviews(dragParams);

        // End Drag.
        if (IsDragging && Input.GetMouseButtonUp(0))
        {
            IsDragging = false;

            // If we're over a UI element, then bail out from this.
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            BuildOnDraggedTiles(dragParams);
        }
    }

    private void CleanUpDragPreviews()
    {
        while (dragPreviewGameObjects.Count > 0)
        {
            GameObject go = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            SimplePool.Despawn(go);
        }
    }

    private void UpdateIsDragging()
    {
        // TODO Keyboard input does not belong in MouseController. Move to KeyboardController?
        if (IsDragging && (Input.GetMouseButtonUp(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            IsDragging = false;
        }
        else if (IsDragging == false && Input.GetMouseButtonDown(0))
        {
            IsDragging = true;
        }
    }

    private DragParameters GetDragParameters()
    {
        int startX = Mathf.FloorToInt(dragStartPosition.x + 0.5f);
        int endX = Mathf.FloorToInt(CurrentPlacingPosition.x + 0.5f);
        int startY = Mathf.FloorToInt(dragStartPosition.y + 0.5f);
        int endY = Mathf.FloorToInt(CurrentPlacingPosition.y + 0.5f);
        return new DragParameters(startX, endX, startY, endY);
    }

    private void ShowPreviews(DragParameters dragParams)
    {
        for (int x = dragParams.StartX; x <= dragParams.EndX; x++)
        {
            for (int y = dragParams.StartY; y <= dragParams.EndY; y++)
            {
                Tile t = WorldController.Instance.World.GetTileAt(x, y, WorldController.Instance.CameraController.CurrentLayer);
                if (t != null)
                {
                    // Display the building hint on top of this tile position.
                    if (buildModeController.BuildMode == BuildMode.FURNITURE)
                    {
                        Furniture proto = PrototypeManager.Furniture.Get(buildModeController.BuildModeType);
                        if (IsPartOfDrag(t, dragParams, proto.DragType))
                        {
                            ShowFurnitureSpriteAtTile(buildModeController.BuildModeType, t);
                            ShowWorkSpotSpriteAtTile(buildModeController.BuildModeType, t);
                        }
                    }
                    else if (buildModeController.BuildMode == BuildMode.UTILITY)
                    {
                        Utility proto = PrototypeManager.Utility.Get(buildModeController.BuildModeType);
                        if (IsPartOfDrag(t, dragParams, proto.DragType))
                        {
                            ShowUtilitySpriteAtTile(buildModeController.BuildModeType, t);
                        }
                    }
                    else
                    {
                        ShowGenericVisuals(x, y);
                    }
                }
            }
        }
    }

    private void ShowGenericVisuals(int x, int y)
    {
        GameObject go = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, WorldController.Instance.CameraController.CurrentLayer), Quaternion.identity);
        go.transform.SetParent(cursorParent.transform, true);
        go.GetComponent<SpriteRenderer>().sprite = SpriteManager.GetSprite("UI", "CursorCircle");
        dragPreviewGameObjects.Add(go);
    }

    private void BuildOnDraggedTiles(DragParameters dragParams)
    {
        for (int x = dragParams.StartX; x <= dragParams.EndX; x++)
        {
            // Variables for the for-loop over the y-coordinates.
            // These are used to determine whether the loop should run from highest to lowest values or vice-versa.
            // The tiles are thus added in a snake or zig-zag pattern, which makes building more efficient.
            int begin = (x - dragParams.StartX) % 2 == 0 ? dragParams.StartY : dragParams.EndY;
            int stop = (x - dragParams.StartX) % 2 == 0 ? dragParams.EndY + 1 : dragParams.StartY - 1;
            int increment = (x - dragParams.StartX) % 2 == 0 ? 1 : -1;

            for (int y = begin; y != stop; y += increment)
            {
                Tile tile = WorldController.Instance.World.GetTileAt(x, y, WorldController.Instance.CameraController.CurrentLayer);
                if (tile == null)
                {
                    // Trying to build off the map, bail out of this cycle.
                    continue;
                }

                if (buildModeController.BuildMode == BuildMode.FURNITURE)
                {
                    // Check for furniture dragType.
                    Furniture proto = PrototypeManager.Furniture.Get(buildModeController.BuildModeType);

                    if (IsPartOfDrag(tile, dragParams, proto.DragType))
                    {
                        // Call BuildModeController::DoBuild().
                        buildModeController.DoBuild(tile);
                    }
                }
                else if (buildModeController.BuildMode == BuildMode.UTILITY)
                {
                    // Check for furniture dragType.
                    Utility proto = PrototypeManager.Utility.Get(buildModeController.BuildModeType);

                    if (IsPartOfDrag(tile, dragParams, proto.DragType))
                    {
                        // Call BuildModeController::DoBuild().
                        buildModeController.DoBuild(tile);
                    }
                }
                else
                {
                    buildModeController.DoBuild(tile);
                }
            }
        }

        // In devmode, utilities don't build their network, and one of the utilities built needs UpdateGrid called explicitly after all are built.
        if (buildModeController.BuildMode == BuildMode.UTILITY && SettingsKeyHolder.DeveloperMode)
        {
            Tile firstTile = World.Current.GetTileAt(dragParams.RawStartX, dragParams.RawStartY, WorldController.Instance.CameraController.CurrentLayer);
            Utility utility = firstTile.Utilities[PrototypeManager.Utility.Get(buildModeController.BuildModeType).Type];
            utility.UpdateGrid(utility);
        }
    }

    // Checks whether a tile is valid for the drag type, given the drag parameters
    // Returns true if tile should be included, false otherwise
    private bool IsPartOfDrag(Tile tile, DragParameters dragParams, string dragType)
    {
        switch (dragType)
        {
            case "border":
                return tile.X == dragParams.StartX || tile.X == dragParams.EndX || tile.Y == dragParams.StartY || tile.Y == dragParams.EndY;
            case "path":
                bool withinXBounds = dragParams.StartX <= tile.X && tile.X <= dragParams.EndX;
                bool onPath = tile.Y == dragParams.RawStartY || tile.X == dragParams.RawEndX;
                return withinXBounds && onPath;
            default:
                return true;
        }
    }

    private void UpdateSpawnClicking()
    {
        if (currentMode != MouseMode.SPAWN_INVENTORY)
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            Tile t = WorldController.Instance.GetTileAtWorldCoord(CurrentFramePosition);
            WorldController.Instance.SpawnInventoryController.SpawnInventory(t);
        }
    }

    private void UpdateCameraMovement()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            panningMouseStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            panningMouseStart.z = 0;
        }

        if (!IsPanning)
        {
            Vector3 currentMousePosition;
            currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            currentMousePosition.z = 0;

            if (Vector3.Distance(panningMouseStart, currentMousePosition) > panningThreshold * Camera.main.orthographicSize)
            {
                IsPanning = true;
            }
        }

        // Handle screen panning.
        if (IsPanning && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
        {   // Right or Middle Mouse Button.
            Vector3 diff = lastFramePosition - CurrentFramePosition;

            if (diff != Vector3.zero)
            {
                contextMenu.Close();
                Camera.main.transform.Translate(diff);
            }

            if (Input.GetMouseButton(1))
            {
                IsDragging = false;
            }
        }

        if (!Input.GetMouseButton(1) && !Input.GetMouseButton(2))
        {
            IsPanning = false;
        }

        // If we're over a UI element or the settings/options menu is open, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject()
            || GameController.Instance.IsModal)
        {
            return;
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            WorldController.Instance.CameraController.ChangeZoom(Input.GetAxis("Mouse ScrollWheel"));
        }

        UpdateCameraBounds();
    }

    /// <summary>
    /// Make the camera stay within the world boundaries.
    /// </summary>
    private void UpdateCameraBounds()
    {
        Vector3 oldPos = Camera.main.transform.position;

        oldPos.x = Mathf.Clamp(oldPos.x, 0, (float)World.Current.Width - 1);
        oldPos.y = Mathf.Clamp(oldPos.y, 0, (float)World.Current.Height - 1);

        Camera.main.transform.position = oldPos;
    }

    private void ShowFurnitureSpriteAtTile(string furnitureType, Tile tile)
    {
        GameObject go = new GameObject();
        go.transform.SetParent(furnitureParent.transform, true);
        dragPreviewGameObjects.Add(go);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Jobs";
        sr.sprite = furnitureSpriteController.GetSpriteForFurniture(furnitureType);

        if (World.Current.FurnitureManager.IsPlacementValid(furnitureType, tile, buildModeController.CurrentPreviewRotation) &&
            World.Current.FurnitureManager.IsWorkSpotClear(furnitureType, tile) &&
            buildModeController.DoesFurnitureBuildJobOverlapExistingBuildJob(tile, furnitureType, buildModeController.CurrentPreviewRotation) == false)
        {
            sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        }
        else
        {
            sr.color = new Color(1f, 0.5f, 0.5f, 0.25f);
        }

        go.name = furnitureType + "_p_" + tile.X + "_" + tile.Y + "_" + tile.Z;
        go.transform.position = tile.Vector3 + ImageUtils.SpritePivotOffset(sr.sprite, buildModeController.CurrentPreviewRotation);
        go.transform.Rotate(0, 0, buildModeController.CurrentPreviewRotation);
    }

    private void ShowWorkSpotSpriteAtTile(string furnitureType, Tile tile)
    {
        Furniture proto = PrototypeManager.Furniture.Get(furnitureType);

        // if the workspot is inside the furniture, there's no reason to show it separately
        if (proto.Jobs.WorkSpotOffset.x >= 0 && proto.Jobs.WorkSpotOffset.x < proto.Width && proto.Jobs.WorkSpotOffset.y >= 0 && proto.Jobs.WorkSpotOffset.y < proto.Height)
        {
            return;
        }

        GameObject go = new GameObject();
        go.transform.SetParent(furnitureParent.transform, true);
        dragPreviewGameObjects.Add(go);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Jobs";
        sr.sprite = SpriteManager.GetSprite("UI", "WorkSpotIndicator");

        if (World.Current.FurnitureManager.IsPlacementValid(furnitureType, tile) &&
            World.Current.FurnitureManager.IsWorkSpotClear(furnitureType, tile) &&
            buildModeController.DoesFurnitureBuildJobOverlapExistingBuildJob(tile, furnitureType) == false)
        {
            sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        }
        else
        {
            sr.color = new Color(1f, 0.5f, 0.5f, 0.25f);
        }

        go.transform.position = new Vector3(tile.X + proto.Jobs.WorkSpotOffset.x, tile.Y + proto.Jobs.WorkSpotOffset.y, WorldController.Instance.CameraController.CurrentLayer);
    }

    private void ShowUtilitySpriteAtTile(string type, Tile tile)
    {
        GameObject go = new GameObject();
        go.transform.SetParent(furnitureParent.transform, true);
        dragPreviewGameObjects.Add(go);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Jobs";
        sr.sprite = utilitySpriteController.GetSpriteForUtility(type);

        if (World.Current.UtilityManager.IsPlacementValid(type, tile) &&
            buildModeController.DoesSameUtilityTypeAlreadyExist(type, tile) &&
            buildModeController.DoesUtilityBuildJobOverlapExistingBuildJob(type, tile) == false)
        {
            sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        }
        else
        {
            sr.color = new Color(1f, 0.5f, 0.5f, 0.25f);
        }

        go.transform.position = new Vector3(tile.X, tile.Y, WorldController.Instance.CameraController.CurrentLayer);
    }

    public class DragParameters
    {
        public DragParameters(int startX, int endX, int startY, int endY)
        {
            this.RawStartX = startX;
            this.RawEndX = endX;
            this.RawStartY = startY;
            this.RawEndY = endY;

            this.StartX = Mathf.Min(startX, endX);
            this.EndX = Mathf.Max(startX, endX);
            this.StartY = Mathf.Min(startY, endY);
            this.EndY = Mathf.Max(startY, endY);
        }

        public int RawStartX { get; private set; }

        public int RawEndX { get; private set; }

        public int RawStartY { get; private set; }

        public int RawEndY { get; private set; }

        public int StartX { get; private set; }

        public int EndX { get; private set; }

        public int StartY { get; private set; }

        public int EndY { get; private set; }
    }
}
