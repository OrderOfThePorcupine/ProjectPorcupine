using System;
using System.Collections;
using DeveloperConsole;
using ProjectPorcupine.Localization;
using UnityEngine.UI;
using UnityEngine;

public class DialogBoxPrompt : BaseDialogBox
{
    public override GameObject InitializeElement()
    {
        result = new Parameter();
        GameObject element = GetFluidVerticalBaseElement("Box", true, true);
        VerticalLayoutGroup group = element.GetComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(30, 30, 30, 30);
        group.spacing = 0;

        string prompt = GetStringParam("Prompt");
        string[] buttons = GetStringArray("Buttons");

        Text text = CreateText(prompt, false, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);
        text.color = Color.white;
        text.font = Resources.Load<Font>("Fonts/anita-semi-square/Anita semi square");
        text.resizeTextForBestFit = true;

        GameObject horizontal = GetFluidHorizontalBaseElement("Buttons", true, true, allocatedHeight: 40);
        horizontal.transform.SetParent(element.transform);
        HorizontalLayoutGroup button_layout = horizontal.GetComponent<HorizontalLayoutGroup>();
        button_layout.padding = new RectOffset(5, 5, 5, 5);
        button_layout.spacing = 5;

        foreach (string button in buttons)
        {
            Button obj = CreateButton(button.Trim());
            obj.transform.SetParent(horizontal.transform);
            obj.onClick.AddListener(() => {
                string copy = button;
                result.AddParameter(new Parameter("ExitButton", copy));
                GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
            });
        }

        return element;
    }
}
