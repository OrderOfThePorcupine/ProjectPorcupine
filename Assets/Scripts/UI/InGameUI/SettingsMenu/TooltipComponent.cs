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

public class TooltipComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string tooltip;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            WorldController.Instance.mouseController.StartUIMode(tooltip);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        WorldController.Instance.mouseController.ClearUIMode();
    }
}
