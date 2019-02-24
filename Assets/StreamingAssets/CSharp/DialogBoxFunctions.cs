using System;
using System.Collections;
using DeveloperConsole;
using ProjectPorcupine.Localization;
using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// A generic toggle.
/// </summary>
public class DialogBoxOptions : BaseDialogBox
{
    public override GameObject InitializeElement()
    {
        GameObject element = GetVerticalBaseElement("Box", 120, 60, TextAnchor.MiddleCenter);

        Text text = CreateText(option.name, true);
        text.transform.SetParent(element.transform);

        toggleElement = CreateToggle(type);
        toggleElement.transform.SetParent(element.transform);

        isOn = getValue();
        toggleElement.isOn = isOn;

        toggleElement.onValueChanged.AddListener(
            (bool v) =>
            {
                if (v != isOn)
                {
                    valueChanged = true;
                    isOn = v;
                }
            });

        LayoutElement layout = toggleElement.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;

        RectTransform rTransform = toggleElement.GetComponent<RectTransform>();
        rTransform.sizeDelta = type == "Switch" ? new Vector2(60, 30) : new Vector2(40, 40);
        rTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rTransform.localPosition = new Vector3(45, 0, 0);

        return element;
    }
}
