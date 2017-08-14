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
using UnityEngine;

namespace ProjectPorcupine.Mouse
{
    /// <summary>
    /// Flags to indicate which mouse callbacks you handle.
    /// This allows you to put <see cref="InvalidOperationException"/> so that if they are called its clear that they weren't meant to,
    /// rather than just returning instantly.
    /// It also speeds up the call since calling virtual methods are slightly more expensive then just normal calls and lets not call things we don't need to.
    /// </summary>
    [Flags]
    public enum MouseHandlerCallbacks
    {
        /// <summary>
        /// Should be used alone to indicate that you don't handle any callbacks,
        /// </summary>
        /// <remarks>
        /// Using `|` or `&` on this will wipe all settings.
        /// </remarks>
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
    /// <remarks> If you aren't going to implement a function don't leave it empty put a <see cref="InvalidOperationException"/> inside it so we get errors. </remarks>
    public interface IMouseHandler
    {
        /// <summary>
        /// Return true to result in a single tile drag parameter,
        /// where StartX and StartY equal EndX and EndY (respectively).
        /// </summary>
        bool DisableDragging { get; }

        /// <summary>
        /// The callbacks that this mouse handler handles.
        /// </summary>
        MouseHandlerCallbacks CallbacksEnabled { get; }

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
}
