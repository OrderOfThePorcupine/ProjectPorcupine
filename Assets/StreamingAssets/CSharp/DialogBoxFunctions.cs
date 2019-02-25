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
        GameObject element = GetVerticalBaseElement("Box", 120, 60, TextAnchor.UpperCenter);
        string title = GetStringParam("Title");
        string prompt = GetStringParam("Prompt");
        string[] buttons = GetStringArray("Buttons");

        Text text = CreateText(title, true, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);

        text = CreateText(prompt, true, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);

        foreach (string button in buttons)
        {
            Button obj = CreateButton(button.Trim());
            obj.transform.SetParent(element.transform);
            string copy = button;
            obj.onClick.AddListener(() => {
                result["ExitButton"] = new Parameter(copy);
                CloseDialog();
                GameObject.Destroy(element.transform.parent.parent.gameObject);
            });
        }

        return element;
    }
}
