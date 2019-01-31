#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Linq;
using ProjectPorcupine.Entities;
using ProjectPorcupine.Localization;
using ProjectPorcupine.Mouse;
using UnityEngine;
using UnityEngine.UI;

public class SelectionInfoTextField : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    private MouseController mc;
    private Text txt;

    // Use this for initialization.
    private void Start()
    {
        mc = WorldController.Instance.MouseController;
        txt = GetComponent<Text>();
    }

    // Update is called once per frame.
    private void Update()
    {
        if (mc.Selection == null || mc.Selection.StuffInTile == false)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        ISelectable actualSelection = mc.Selection.GetSelectedStuff();

        string additionalInfoText = string.Join(Environment.NewLine, actualSelection.GetAdditionalInfo().ToArray());

        if (actualSelection.GetType() == typeof(Character))
        {
            // TODO: Change the hitpoint stuff.
            txt.text =
                actualSelection.GetName() + "\n" +
                actualSelection.GetDescription() + "\n" +
                LocalizationTable.GetLocalization(actualSelection.GetJobDescription()) + "\n" +
                additionalInfoText;
        }
        else
        {
            // TODO: Change the hitpoint stuff.
            txt.text =
                LocalizationTable.GetLocalization(actualSelection.GetName()) + "\n" +
                LocalizationTable.GetLocalization(actualSelection.GetDescription()) + "\n" +
                additionalInfoText;
        }
    }
}
