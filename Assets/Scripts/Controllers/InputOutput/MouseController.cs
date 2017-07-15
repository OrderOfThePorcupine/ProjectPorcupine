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

[Flags]
public enum MouseHandlerCallbacks
{
    NONE = 0,
    HANDLE_TOOLTIP = 1 << 0,
    HANDLE_CLICK = 1 << 1,
    HANDLE_DRAG = 1 << 2,
    HANDLE_DRAG_VISUAL = 1 << 3,
    HANDLE_PLACING_POSITION = 1 << 4,
}

public interface IMouseHandler
{
    MouseHandlerCallbacks CallbacksEnabled { get; }

    bool SinglePlacementDraggingEnabled { get; }

    void HandleTooltip(Vector2 position, MouseCursor cursor, bool isDragging);

    void HandleClick(Vector2 position, int mouseKey);

    void HandleDrag(MouseController.DragParameters parameters);

    List<GameObject> HandleDragVisual(MouseController.DragParameters parameters, Transform parent);

    Vector3 HandlePlacingPosition(Vector3 position);
}

/// <summary>
/// This class will handle a range of things;
/// - Dragging
/// - Clicking
/// - Moving the mouse
/// Anything else (such as what occurs after a drag stops) is handled by actions.
/// </summary>
public class MouseController
{
    private GameObject cursorParent;

    private ContextMenu contextMenu;
    private MouseCursor mouseCursor;

    private Vector3 lastFramePosition;          // Our last frame position
    private Vector3 dragStartPosition;          // The world-position start of our left-mouse drag operation.

    private IMouseHandler[] mouseHandlers;      // Handles mouse events

    private string tooltip = null; // If not null then will handle tooltip.
    private MouseMode currentMode = MouseMode.DEFAULT;

    private int validPostionCount;
    private int invalidPositionCount;

    private float panningThreshold = 0.015f;
    private Vector3 panningMouseStart = Vector3.zero;

    /// <summary>
    /// Construct a new mouse controller.
    /// </summary>
    public MouseController()
    {
        BuildModeController.Instance.SetMouseController(this);
        contextMenu = GameObject.FindObjectOfType<ContextMenu>();
        DragPreviewGameObjects = new List<GameObject>();
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
        mouseHandlers = new IMouseHandler[]
        {
            new MultiMouseHandler(MouseMode.DEFAULT), // Default
            BuildModeController.Instance, // Build
            new MultiMouseHandler(MouseMode.LIGHT_UI), // Light UI
            new MultiMouseHandler(MouseMode.LIGHT_UI), // Heavy UI
            WorldController.Instance.SpawnInventoryController // Inventory
        };

        TimeManager.Instance.EveryFrame += (time) => Update();
        KeyboardManager.Instance.RegisterInputAction("Escpae", KeyboardMappedInputType.KeyUp, OnEscape);
    }

    public enum MouseMode
    {
        /// <summary>
        /// Default mode is for selecting objects.
        /// Will present information like coords.
        /// TooltipInfo:
        ///     Small information that is around 5-10 characters.
        /// </summary>
        DEFAULT,

        /// <summary>
        /// When build mode is activated.
        /// Tooltip:
        ///     Build information.  Contains a lot of data.
        /// </summary>
        BUILD,

        /// <summary>
        /// Enabled by the <see cref="TooltipComponent"/>.
        /// Tooltip:
        /// Around a sentence.
        /// </summary>
        LIGHT_UI,

        /// <summary>
        /// Enabled by the <see cref="TooltipComponent.UseHeavyTextMode"/>.
        /// Tooltip:
        ///     More then a sentence / a block of text.
        /// </summary>
        HEAVY_UI,

        /// <summary>
        /// When inventory is activated.
        /// Tooltip:
        ///     None.
        /// </summary>
        INVENTORY,
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
    /// All the objects that are currently dragged.
    /// </summary>
    public List<GameObject> DragPreviewGameObjects { get; private set; }

    /// <summary>
    /// Changes the mouse mode.
    /// </summary>
    /// <param name="newMode"> The new mode to change to. </param>
    /// <param name="tooltip"> Tooltip if any. </param>
    /// <param name="forceShow"> If enabled will show mouse in UI mode. </param>
    public void ChangeMouseMode(MouseMode newMode, string tooltip = null)
    {
        this.tooltip = tooltip;
        currentMode = newMode;
        mouseCursor.UIMode = newMode == MouseMode.HEAVY_UI || newMode == MouseMode.LIGHT_UI;
    }

    /// <summary>
    /// Clears the mouse mode.
    /// Will be set to BuildMode if <see cref="BuildModeController.Building"/>.
    /// </summary>
    /// <param name="stopDragging"> Stop dragging?. </param>
    public void ClearMouseMode(bool stopDragging = false)
    {
        this.tooltip = null;
        mouseCursor.UIMode = false;
        currentMode = BuildModeController.Instance.Building ? MouseMode.BUILD : MouseMode.DEFAULT;

        if (stopDragging)
        {
            IsDragging = false;
        }
    }

    /// <summary>
    /// This action is performed when you want the system to treat it as if 'escape' was pressed.
    /// </summary>
    private void OnEscape()
    {
        /* Will do the following;
         * Stop dragging
         * Selection = null
         * If it was caused by a left click and we aren't panning then turn off tooltips (for UI)
         * If the currentMode is default then it'll handle context menu
         * It also reset the current mode if it isn't UI (and if it is inventory then panning == false).
         */

        IsDragging = false;
        Selection = null;

        // Is the context also supposed to open on ESCAPE? That seems wrong
        if (currentMode == MouseMode.DEFAULT)
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

        if (currentMode == MouseMode.BUILD || currentMode == MouseMode.INVENTORY || currentMode == MouseMode.DEFAULT)
        {
            // If it is Build or Default then set to default
            // Else if it is inventory and panning == false then set to default
            if (currentMode != MouseMode.INVENTORY || IsPanning == false)
            {
                currentMode = MouseMode.DEFAULT;
            }
        }
    }

    private void Update()
    {
        bool mouseButton0Up = Input.GetMouseButtonUp(0);
        bool mouseButton1Up = Input.GetMouseButtonUp(1);
        bool mouseButton2Up = Input.GetMouseButtonUp(2);

        IMouseHandler handler = mouseHandlers[(int)currentMode];

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        CurrentFramePosition = new Vector3(mousePos.x, mousePos.y, WorldController.Instance.CameraController.CurrentLayer);

        if ((handler.CallbacksEnabled & MouseHandlerCallbacks.HANDLE_PLACING_POSITION) == MouseHandlerCallbacks.HANDLE_PLACING_POSITION)
        {
            CurrentPlacingPosition = handler.HandlePlacingPosition(CurrentFramePosition); // CalculatePlacingPosition(); // REPLACED BY PositionHandlers
        }
        else
        {
            CurrentPlacingPosition = CurrentFramePosition;
        }

        bool performDragThisFrame = false;
        if ((mouseButton0Up || mouseButton1Up) && IsDragging)
        {
            if (IsPanning == false)
            {
                tooltip = null;
                mouseCursor.UIMode = false;
            }

            OnEscape();

            if (mouseButton0Up)
            {
                // If we are over a UI element then don't perform the drag this frame
                // Also only perform the drag if we are actually ending the drag with left click
                // else its a cancel drag action
                performDragThisFrame = !EventSystem.current.IsPointerOverGameObject();
            }
        }
        else if (IsDragging == false && Input.GetMouseButtonDown(0))
        {
            IsDragging = true;
            dragStartPosition = CurrentPlacingPosition;
        }

        mouseCursor.Update();

        // HANDLE DRAG
        // Clear all the drag objects
        for (int i = 0; i < DragPreviewGameObjects.Count; i++)
        {
            SimplePool.Despawn(DragPreviewGameObjects[i]);
        }

        DragPreviewGameObjects.Clear();

        // If callback for handling drag is enabled then handle the drag
        bool dragEnabled = (handler.CallbacksEnabled & MouseHandlerCallbacks.HANDLE_DRAG) == MouseHandlerCallbacks.HANDLE_DRAG;
        bool dragVisualEnabled = (handler.CallbacksEnabled & MouseHandlerCallbacks.HANDLE_DRAG_VISUAL) == MouseHandlerCallbacks.HANDLE_DRAG;

        if (dragEnabled || dragVisualEnabled)
        {
            if (handler.SinglePlacementDraggingEnabled || IsDragging == false)
            {
                dragStartPosition = CurrentPlacingPosition;
            }

            DragParameters dragParams = new DragParameters(dragStartPosition, CurrentFramePosition);

            if (dragVisualEnabled)
            {
                // HANDLE VISUAL
                DragPreviewGameObjects = handler.HandleDragVisual(dragParams, cursorParent.transform);
            }

            // If we have dragEnabled and we are to perform it on our next frame (which is this frame) perform it
            if (dragEnabled && performDragThisFrame)
            {
                // HANDLE DRAG
                handler.HandleDrag(dragParams);
            }
        }

        UpdateCameraMovement(mouseButton1Up, mouseButton2Up);

        // Tooltip handling
        if ((handler.CallbacksEnabled & MouseHandlerCallbacks.HANDLE_TOOLTIP) == MouseHandlerCallbacks.HANDLE_TOOLTIP)
        {
            handler.HandleTooltip(CurrentFramePosition, mouseCursor, IsDragging);
        }

        // Could include drag clicks
        // Should handle any 'building' that requires dragging in the HandleDrag callback
        if ((handler.CallbacksEnabled & MouseHandlerCallbacks.HANDLE_CLICK) == MouseHandlerCallbacks.HANDLE_CLICK && EventSystem.current.IsPointerOverGameObject() == false)
        {
            if (mouseButton0Up)
            {
                handler.HandleClick(CurrentFramePosition, 0);
            }

            if (mouseButton1Up)
            {
                handler.HandleClick(CurrentFramePosition, 1);
            }

            if (mouseButton2Up)
            {
                handler.HandleClick(CurrentFramePosition, 2);
            }
        }

        // Save the mouse position from this frame.
        // We don't use currentFramePosition because we may have moved the camera.
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.x = mousePos.x;
        lastFramePosition.y = mousePos.y;
        lastFramePosition.z = WorldController.Instance.CameraController.CurrentLayer;
    }

    private void UpdateCameraMovement(bool mouseButton1Up, bool mouseButton2Up)
    {
        if (mouseButton1Up || mouseButton2Up)
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
        if (IsPanning && (mouseButton1Up || mouseButton2Up))
        {   // Right or Middle Mouse Button.
            Vector3 diff = lastFramePosition - CurrentFramePosition;

            if (diff != Vector3.zero)
            {
                contextMenu.Close();
                Camera.main.transform.Translate(diff);
            }

            if (mouseButton1Up)
            {
                IsDragging = false;
            }
        }

        if (!mouseButton1Up && !mouseButton2Up)
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

        oldPos.x = Mathf.Clamp(oldPos.x, 0, World.Current.Width - 1);
        oldPos.y = Mathf.Clamp(oldPos.y, 0, World.Current.Height - 1);

        Camera.main.transform.position = oldPos;
    }

    public class DragParameters
    {
        /// <summary>
        /// Create using Vector2 the x and y coords are used and are rounded to integers.
        /// </summary>
        /// <param name="start"> The start coord. </param>
        /// <param name="end"> The end coord. </param>
        public DragParameters(Vector2 start, Vector2 end) : this(start.x, end.x, start.y, end.y)
        {
        }

        /// <summary>
        /// Create using Vector3 the x and y coords are used and are rounded to integers.
        /// </summary>
        /// <param name="start"> The start coord. </param>
        /// <param name="end"> The end coord. </param>
        /// <remarks> The z coord is ignored. </remarks>
        public DragParameters(Vector3 start, Vector3 end) : this(start.x, end.x, start.y, end.y)
        {
        }

        /// <summary>
        /// Create using floats, which are rounded to integers.
        /// </summary>
        /// <param name="startX"> The start x coord. </param>
        /// <param name="endX"> The end x coord. </param>
        /// <param name="startY"> The start y coord. </param>
        /// <param name="endY"> The end y coord. </param>
        public DragParameters(float startX, float endX, float startY, float endY) : this(Mathf.RoundToInt(startX), Mathf.RoundToInt(endX), Mathf.RoundToInt(startY), Mathf.RoundToInt(endY))
        {
        }

        /// <summary>
        /// Create using integers, which are directly used to set the class variables.
        /// </summary>
        /// <param name="startX"> The start x coord. </param>
        /// <param name="endX"> The end x coord. </param>
        /// <param name="startY"> The start y coord. </param>
        /// <param name="endY"> The end y coord. </param>
        public DragParameters(int startX, int endX, int startY, int endY)
        {
            this.RawStartX = startX;
            this.RawEndX = endX;
            this.RawStartY = startY;
            this.RawEndY = endY;

            this.StartX = Mathf.Min(RawStartX, RawEndX);
            this.EndX = Mathf.Max(RawStartX, RawEndX);
            this.StartY = Mathf.Min(RawStartY, RawEndY);
            this.EndY = Mathf.Max(RawStartY, RawEndY);
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

    private class MultiMouseHandler : IMouseHandler
    {
        private MouseMode handles;

        public MultiMouseHandler(MouseMode handles)
        {
            this.handles = handles;
        }

        public MouseHandlerCallbacks CallbacksEnabled
        {
            get
            {
                return MouseHandlerCallbacks.HANDLE_TOOLTIP;
            }
        }

        public bool SinglePlacementDraggingEnabled
        {
            get
            {
                return false;
            }
        }

        public void HandleTooltip(Vector2 position, MouseCursor cursor, bool isDragging)
        {
            string tooltip = WorldController.Instance.MouseController.tooltip;

            switch (handles)
            {
                case MouseMode.DEFAULT:
                    cursor.Reset();
                    Tile t = WorldController.Instance.GetTileAtWorldCoord(position);
                    if (t != null)
                    {
                        cursor.DisplayCursorInfo(TextAnchor.MiddleRight, string.Format("X:{0} Y:{1} Z:{2}", t.X.ToString(), t.Y.ToString(), t.Z.ToString()), MouseCursor.DefaultTint);
                    }

                    break;
                case MouseMode.LIGHT_UI:
                    cursor.Reset();

                    if (string.IsNullOrEmpty(tooltip) == false)
                    {
                        Debug.LogWarning(cursor.UIMode);
                        cursor.DisplayCursorInfo(TextAnchor.MiddleRight, LocalizationTable.GetLocalization(tooltip), MouseCursor.DefaultTint);
                    }

                    break;
                case MouseMode.HEAVY_UI:
                    cursor.Reset();

                    if (string.IsNullOrEmpty(tooltip) == false)
                    {
                        Debug.LogWarning(cursor.UIMode);
                        cursor.DisplayCursorInfo(TextAnchor.MiddleRight, LocalizationTable.GetLocalization(tooltip), MouseCursor.DefaultTint);
                    }

                    break;
                default:
                    // Only supports the above variants.
                    throw new NotImplementedException();
            }
        }

        public void HandleClick(Vector2 position, int mouseKey)
        {
            if (handles == MouseMode.DEFAULT)
            {
                // These ifs are separate since we want to error out if the handle != MouseMode.DEFAULT, but we don't want to error out if mouseKey != 0
                if (mouseKey == 0)
                {
                    if (WorldController.Instance.MouseController.contextMenu != null)
                    {
                        WorldController.Instance.MouseController.contextMenu.Close();
                    }

                    // We just release the mouse button, so that's our queue to update our selection.
                    Tile tileUnderMouse = WorldController.Instance.GetTileAtWorldCoord(WorldController.Instance.MouseController.CurrentFramePosition);

                    if (tileUnderMouse == null)
                    {
                        // No valid tile under mouse.
                        return;
                    }

                    SelectionInfo selection = WorldController.Instance.MouseController.Selection;

                    if (selection == null || selection.Tile != tileUnderMouse)
                    {
                        if (selection != null)
                        {
                            selection.GetSelectedStuff().IsSelected = false;
                        }

                        // We have just selected a brand new tile, reset the info.
                        selection = new SelectionInfo(tileUnderMouse);
                        selection.GetSelectedStuff().IsSelected = true;
                    }
                    else
                    {
                        // This is the same tile we already have selected, so cycle the subSelection to the next non-null item.
                        // Note: Tile sub selection can NEVER be null, so we know we'll always find something.

                        // Rebuild the array of possible sub-selection in case characters moved in or out of the tile.
                        // [IsSelected] Set our last stuff to be not selected because were selecting the next stuff
                        selection.GetSelectedStuff().IsSelected = false;
                        selection.BuildStuffInTile();
                        selection.SelectNextStuff();
                        selection.GetSelectedStuff().IsSelected = true;
                    }
                }
            }
            else
            {
                // Not implemented by the UI Handlers
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// NOT IMPLEMENTED BY DEFAULT MOUSE HANDLER.  Will throw on call.
        /// </summary>
        public void HandleDrag(DragParameters parameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// NOT IMPLEMENTED BY DEFAULT MOUSE HANDLER.  Will throw on call.
        /// </summary>
        public List<GameObject> HandleDragVisual(DragParameters parameters, Transform parent)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// NOT IMPLEMENTED BY DEFAULT MOUSE HANDLER.  Will throw on call.
        /// </summary>
        public Vector3 HandlePlacingPosition(Vector3 position)
        {
            throw new NotImplementedException();
        }
    }
}
