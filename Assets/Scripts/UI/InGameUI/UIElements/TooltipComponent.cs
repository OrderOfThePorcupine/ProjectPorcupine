#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This component can be added to any UI element to enable it to display tooltips.
/// </summary>
public class TooltipComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// The tooltip to display.
    /// </summary>
    public string Tooltip { get; set; }

    /// <summary>
    /// If this is on, then this won't show a tooltip.
    /// </summary>
    public bool Disable { get; set; }

    /// <summary>
    /// If enabled it will use heavy text mode.
    /// Heavy text mode has a background and is made for
    /// text that is longer then a sentence.
    /// </summary>
    public bool UseHeavyTextMode { get; set; }

    /// <summary>
    /// Activated when the pointer enters.
    /// </summary>
    /// <param name="eventData"> Information about the pointer. </param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!Disable && eventData.pointerCurrentRaycast.gameObject != null)
        {
            WorldController.Instance.MouseController.StartUIMode(Tooltip);
        }
    }

    /// <summary>
    /// Activated when the pointer exits.
    /// </summary>
    /// <param name="eventData"> Information about the pointer. </param>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!Disable)
        {
            WorldController.Instance.MouseController.ClearUIMode();
        }
    }
}
