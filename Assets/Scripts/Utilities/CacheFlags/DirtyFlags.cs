using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All possible flags.
/// </summary>
public enum DirtyFlags
{
    PERFORMANCE_HUD_DIRTY_UI,
    SETTINGS_MENU,
    PATHFINDING_RECALCULATE_ROUTES,
    JOBS_PAUSE,
    JOBS_RESUME,
    DEV_CONSOLE_DIRTY_SETTINGS
}
