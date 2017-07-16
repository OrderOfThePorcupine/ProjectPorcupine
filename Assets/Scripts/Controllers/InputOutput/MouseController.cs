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

/// <summary>
/// Flags to indicate which mouse callbacks you handle.
/// This allows you to put <see cref="NotImplementedException"/> so that if they are called its clear that they weren't meant to,
/// rather than just returning instantly.
/// It also speeds up the call since calling virtual methods are slightly more expensive then just normal calls and lets not call things we don't need to.
/// </summary>
[Flags]
public enum MouseHandlerCallbacks
{
    /// <summary>
    /// Note: Using `|` or `&` on this will wipe all settings.
    /// Should be used alone to indicate that you don't handle any callbacks,
    /// In reality could be omitted from this list but is here for completeness.
    /// </summary>
    NONE = 0,

    /// <summary>
    /// Enable this if you are going to handle tooltips.
    /// </summary>
    HANDLE_TOOLTIP = 1 << 0,

    /// <summary>
    /// Enable this if you are going to handle clicks.
    /// </summary>
    HANDLE_CLICK = 1 << 1,

    /// <summary>
    /// Enable this if you are going to handle dragging.
    /// </summary>
    HANDLE_DRAG_FINISHED = 1 << 2,

    /// <summary>
    /// Enable this if you are going to handle the visuals for dragging.
    /// </summary>
    HANDLE_DRAG_VISUAL = 1 << 3,

    /// <summary>
    /// Enable this if you are going to edit the placing position.
    /// Such as adding an offset for rotation.
    /// </summary>
    HANDLE_PLACING_POSITION = 1 << 4,
}

/// <summary>
/// This interface is to enable your ability to handle mouse events.
/// This won't automatically make you handle mouse events you will have to add yourself to the array <see cref="MouseController.mouseHandlers"/> and add an enum mode.
/// This enables later for us to make this effectively all modded, with a bunch of JSON properties.
/// </summary>
/// <remarks> If you aren't going to implement a function don't leave it empty put a <see cref="NotImplementedException"/> inside it so we get proper errors. </remarks>
public interface IMouseHandler
{
    /// <summary>
    /// Return what callbacks you are going to handle to reduce the amount of calls needed.
    /// </summary>
    MouseHandlerCallbacks CallbacksEnabled { get; }

    /// <summary>
    /// Enable this to disable all dragging.
    /// This will result in a single tile drag parameter,
    /// where StartX and StartY equal EndX and EndY (respectively).
    /// </summary>
    bool DisableDragging { get; }

    /// <summary>
    /// Implement this if you are going to handle tooltips.
    /// </summary>
    /// <param name="mousePosition"> The position of the mouse. </param>
    /// <param name="cursor"> The cursor object that allows you to do <see cref="MouseCursor.DisplayCursorInfo(TextAnchor, string, Color, bool)"/>.</param>
    /// <param name="isDragging"> Is the system currently dragging. Equivalent to <see cref="MouseController.IsDragging"/>.</param>
    void HandleTooltip(Vector2 mousePosition, MouseCursor cursor, bool isDragging);

    /// <summary>
    /// Implement this if you are going to handle clicks.
    /// </summary>
    /// <param name="mousePosition"> The position of the mouse. </param>
    /// <param name="mouseKey"> What mouse button was pressed; 0 is left button, 1 is right button, and 2 is the middle (scroll wheel) button. </param>
    void HandleClick(Vector2 mousePosition, int mouseKey);

    /// <summary>
    /// Implement this if you are going to handle dragging the mouse.
    /// This occurs once the user 'confirms the drag' by lifting up the left mouse button.
    /// This won't occur if the user cancels the drag by clicking either the right or middle (scroll wheel) mouse button or escape key.
    /// </summary>
    /// <param name="dragParams"> The drag parameters created from the start drag position and end drag position. </param>
    void HandleDragFinished(MouseController.DragParameters dragParams);

    /// <summary>
    /// Implement this if you are going to handle the visual side of dragging the mouse.
    /// You do not need to cleanup but you have to return all the gameobjects that you created.
    /// </summary>
    /// <param name="dragParams"> The drag parameters created from the start drag position and end drag position. </param>
    /// <param name="parent"> The parent to attach all your gameobjects to, however you can attach it to any gameobject. </param>
    /// <returns> Return all the gameobjects that you have created. </returns>
    List<GameObject> HandleDragVisual(MouseController.DragParameters dragParams, Transform parent);

    /// <summary>
    /// Implement this if you are going to override the placing position.
    /// Such as if you wanted to offset for rotation.
    /// </summary>
    /// <param name="currentFramePosition"> The current frame position and what would have been set. </param>
    /// <returns> The new placing position. </returns>
    Vector3 HandlePlacingPosition(Vector3 currentFramePosition);
}

/// <summary>
/// This class will handle a range of things;
/// - Dragging
/// - Clicking
/// - Moving the mouse
/// Anything else (such as what occurs after a drag stops) is handled by <see cref="IMouseHandler"/>s.
/// </summary>
public class MouseController
{
    /// <summary>
    /// The threshold for when to start panning.
    /// </summary>
    private const float panningThreshold = 0.015f;

    /// <summary>
    /// The cursor parent where most drag preview objects should be parented to.
    /// </summary>
    private GameObject cursorParent;

    /// <summary>
    /// Reference to the context menu.
    /// </summary>
    private ContextMenu contextMenu;

    /// <summary>
    /// Reference to the mouse cursor.
    /// </summary>
    private MouseCursor mouseCursor;

    /// <summary>
    /// The position of the mouse from the last update.
    /// </summary>
    private Vector3 lastFramePosition;

    /// <summary>
    /// The starting position of the drag.
    /// If <see cref="IMouseHandler.DisableDragging"/> is enabled then this will equal PlacingPosition,
    /// resulting in a singular 'tile' drag.
    /// </summary>
    private Vector3 dragStartPosition;

    /// <summary>
    /// Handlers which handle all the events that the mouse controller catches.
    /// </summary>
    private IMouseHandler[] mouseHandlers;

    /// <summary>
    /// The tooltip for the UI, this is needed for UIModes.
    /// </summary>
    private string uiTooltip = null;

    /// <summary>
    /// The current mouse mode set using <see cref="ChangeMouseMode(MouseMode, string)"/>.
    /// Acts as a ptr to <see cref="mouseHandlers"/> via cast to int.
    /// </summary>
    private MouseMode currentMode = MouseMode.DEFAULT;

    /// <summary>
    /// Where the panning started.
    /// </summary>
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
        KeyboardManager.Instance.RegisterInputAction("Escape", KeyboardMappedInputType.KeyUp, OnEscape);
    }

    /// <summary>
    /// A set of mouse modes.
    /// </summary>
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
        ///     Around a sentence.
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
        this.uiTooltip = tooltip;
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
        this.uiTooltip = null;
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

    /// <summary>
    /// Updates all the little components of mouse controller.
    /// </summary>
    private void Update()
    {
        // This prevents having to reobtain these, since the call is expensive enough to warrant this
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
                uiTooltip = null;
                mouseCursor.UIMode = false;
            }

            IsDragging = false;
            Selection = null;

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

        mouseCursor.UpdateCursor();

        // HANDLE DRAG
        // Clear all the drag objects
        for (int i = 0; i < DragPreviewGameObjects.Count; i++)
        {
            SimplePool.Despawn(DragPreviewGameObjects[i]);
        }

        DragPreviewGameObjects.Clear();

        // If callback for handling drag is enabled then handle the drag
        bool dragEnabled = (handler.CallbacksEnabled & MouseHandlerCallbacks.HANDLE_DRAG_FINISHED) == MouseHandlerCallbacks.HANDLE_DRAG_FINISHED;
        bool dragVisualEnabled = (handler.CallbacksEnabled & MouseHandlerCallbacks.HANDLE_DRAG_VISUAL) == MouseHandlerCallbacks.HANDLE_DRAG_VISUAL;

        if (dragEnabled || dragVisualEnabled)
        {
            if (handler.DisableDragging || (IsDragging == false && performDragThisFrame == false))
            {
                dragStartPosition = CurrentPlacingPosition;
            }

            DragParameters dragParams = new DragParameters(dragStartPosition, CurrentPlacingPosition);

            if (dragVisualEnabled)
            {
                // HANDLE VISUAL
                DragPreviewGameObjects = handler.HandleDragVisual(dragParams, cursorParent.transform);
            }

            // If we have dragEnabled and we are to perform it on our next frame (which is this frame) perform it
            if (dragEnabled && performDragThisFrame)
            {
                // HANDLE DRAG
                handler.HandleDragFinished(dragParams);
            }
        }

        UpdateCameraMovement();

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

    /// <summary>
    /// Updates the camera movement.s
    /// </summary>
    private void UpdateCameraMovement()
    {
        bool mouseButton1 = Input.GetMouseButton(1);
        bool mouseButton2 = Input.GetMouseButton(2);

        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            panningMouseStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            panningMouseStart.z = 0;
        }

        if (!IsPanning)
        {
            Vector3 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            currentMousePosition.z = 0;

            if (Vector3.Distance(panningMouseStart, currentMousePosition) > panningThreshold * Camera.main.orthographicSize)
            {
                IsPanning = true;
            }
        }

        // Handle screen panning.
        if (IsPanning && (mouseButton1 || mouseButton2))
        {   // Right or Middle Mouse Button.
            Vector3 diff = lastFramePosition - CurrentFramePosition;

            if (diff != Vector3.zero)
            {
                contextMenu.Close();
                Camera.main.transform.Translate(diff);
            }

            if (mouseButton1)
            {
                IsDragging = false;
            }
        }

        if (!mouseButton1 && !mouseButton2)
        {
            IsPanning = false;
        }

        // If we're over a UI element or the settings/options menu is open, then bail out from this.
        if (EventSystem.current.IsPointerOverGameObject() || GameController.Instance.IsModal)
        {
            return;
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            WorldController.Instance.CameraController.ChangeZoom(Input.GetAxis("Mouse ScrollWheel"));
        }

        // Update camera bounds
        Vector3 oldPos = Camera.main.transform.position;
        oldPos.x = Mathf.Clamp(oldPos.x, 0, World.Current.Width - 1);
        oldPos.y = Mathf.Clamp(oldPos.y, 0, World.Current.Height - 1);
        Camera.main.transform.position = oldPos;
    }

    /// <summary>
    /// These are parameters for dragging the mouse.
    /// </summary>
    public struct DragParameters
    {
        /// <summary>
        /// The raw starting value for x.
        /// This could be lower or higher than <see cref="RawEndX"/>.
        /// </summary>
        public readonly int RawStartX;

        /// <summary>
        /// The raw ending value for x.
        /// This could be lower or higher than <see cref="RawStartX"/>.
        /// </summary>
        public readonly int RawEndX;

        /// <summary>
        /// The raw starting value for y.
        /// This could be lower or higher than <see cref="RawEndY"/>.
        /// </summary>
        public readonly int RawStartY;

        /// <summary>
        /// The raw ending value for y.
        /// This could be lower or higher than <see cref="RawStartY"/>.
        /// </summary>
        public readonly int RawEndY;

        /// <summary>
        /// The actual starting position for x.  Will always be lower than <see cref="EndX"/>.
        /// These will make it so that the smaller value is the 'beginning value',
        /// this means that its more efficient and one can always use an increasing iterator.
        /// </summary>
        public readonly int StartX;

        /// <summary>
        /// The actual ending position for x.  Will always be higher than <see cref="StartX"/>.
        /// These will make it so that the smaller value is the 'beginning value',
        /// this means that its more efficient and one can always use an increasing iterator.
        /// </summary>
        public readonly int EndX;

        /// <summary>
        /// The actual starting position for y.  Will always be lower than <see cref="EndY"/>.
        /// These will make it so that the smaller value is the 'beginning value',
        /// this means that its more efficient and one can always use an increasing iterator.
        /// </summary>
        public readonly int StartY;

        /// <summary>
        /// The actual ending position for y.  Will always be higher than <see cref="StartY"/>.
        /// These will make it so that the smaller value is the 'beginning value',
        /// this means that its more efficient and one can always use an increasing iterator.
        /// </summary>
        public readonly int EndY;

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

            MathUtilities.MinAndMax(startX, endX, out StartX, out EndX);
            MathUtilities.MinAndMax(startY, endY, out StartY, out EndY);
        }
    }

    /// <summary>
    /// Handles cases; <see cref="MouseMode.BUILD"/>, <see cref="MouseMode.LIGHT_UI"/>, <see cref="MouseMode.HEAVY_UI"/>.
    /// </summary>
    private class MultiMouseHandler : IMouseHandler
    {
        private MouseMode modeToHandle; // The mode to handle

        /// <summary>
        /// Create a new handler handling the mode supplied.
        /// </summary>
        /// <param name="modeToHandle"></param>
        public MultiMouseHandler(MouseMode modeToHandle)
        {
            this.modeToHandle = modeToHandle;
        }

        /// <summary>
        /// What callbacks are enabled for this mousemode.
        /// If the <see cref="modeToHandle"/> is <see cref="MouseMode.HEAVY_UI"/> or <see cref="MouseMode.LIGHT_UI"/> then it handles only tooltip.
        /// Else if its <see cref="MouseMode.DEFAULT"/> then it handles tooltip and clicking.
        /// </summary>
        public MouseHandlerCallbacks CallbacksEnabled
        {
            get
            {
                if (modeToHandle == MouseMode.HEAVY_UI || modeToHandle == MouseMode.LIGHT_UI)
                {
                    return MouseHandlerCallbacks.HANDLE_TOOLTIP;
                }
                else if (modeToHandle == MouseMode.DEFAULT)
                {
                    return MouseHandlerCallbacks.HANDLE_TOOLTIP | MouseHandlerCallbacks.HANDLE_CLICK;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Will always return false.
        /// </summary>
        public bool DisableDragging
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Handles the tooltip mode.
        /// </summary>
        public void HandleTooltip(Vector2 position, MouseCursor cursor, bool isDragging)
        {
            string tooltip = WorldController.Instance.MouseController.uiTooltip;

            switch (modeToHandle)
            {
                case MouseMode.DEFAULT:
                    cursor.Reset();
                    Tile t = WorldController.Instance.GetTileAtWorldCoord(position);
                    if (t != null)
                    {
                        cursor.DisplayCursorInfo(TextAnchor.MiddleRight, string.Format("X:{0} Y:{1} Z:{2}", t.X.ToString(), t.Y.ToString(), t.Z.ToString()), MouseCursor.DefaultTint, false);
                    }

                    break;
                case MouseMode.LIGHT_UI:
                    cursor.Reset();

                    if (string.IsNullOrEmpty(tooltip) == false)
                    {
                        cursor.DisplayCursorInfo(TextAnchor.MiddleRight, LocalizationTable.GetLocalization(tooltip), MouseCursor.DefaultTint, false);
                    }

                    break;
                case MouseMode.HEAVY_UI:
                    cursor.Reset();

                    if (string.IsNullOrEmpty(tooltip) == false)
                    {
                        cursor.DisplayCursorInfo(TextAnchor.MiddleRight, LocalizationTable.GetLocalization(tooltip), MouseCursor.DefaultTint, true);
                    }

                    break;
                default:
                    // Only supports the above variants.
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Handles clicking, in this mode it is about selecting objects.
        /// </summary>
        public void HandleClick(Vector2 position, int mouseKey)
        {
            if (modeToHandle == MouseMode.DEFAULT)
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

                    // This will make sure that if we created new selectioninformation it is reflected
                    WorldController.Instance.MouseController.Selection = selection;
                }
                else if (mouseKey == 1)
                {
                    Tile t = WorldController.Instance.GetTileAtWorldCoord(position);
                    if (WorldController.Instance.MouseController.contextMenu != null && t != null)
                    {
                        if (WorldController.Instance.MouseController.IsPanning)
                        {
                            WorldController.Instance.MouseController.contextMenu.Close();
                        }
                        else
                        {
                            WorldController.Instance.MouseController.contextMenu.Open(t);
                        }
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
        public void HandleDragFinished(DragParameters parameters)
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
