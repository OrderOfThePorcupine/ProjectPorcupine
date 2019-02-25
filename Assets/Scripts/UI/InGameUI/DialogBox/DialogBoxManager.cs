#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion


using System.Collections.Generic;
using System.Linq;
using ProjectPorcupine.Localization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The controller for dialog boxes.
/// </summary>
public class DialogBoxHandler
{
    private Dictionary<string, BaseDialogBox> dialogBoxes = new Dictionary<string, BaseDialogBox>();

    private GameObject root;
    private GameObject baseDialogTemplate;

    public DialogBoxHandler(GameObject parent, GameObject baseTemplate)
    {
        root = new GameObject("Dialog Boxes");
        root.transform.SetParent(parent.transform);
        root.transform.SetAsLastSibling();
        baseDialogTemplate = baseTemplate;
        LoadDialogs();
    }

    public void ShowDialogBox(string name, Dictionary<string, object> data = null, BaseDialogBox.OnCloseAction action = null)
    {
        if (dialogBoxes.ContainsKey(name))
        {
            dialogBoxes[name].OnClose = action;
            dialogBoxes[name].callerData = data;
            FinalizeDialogBox(dialogBoxes[name]);
        }
        else
        {
            UnityDebugger.Debugger.LogError("DialogBox", "DialogBox doesn't exist: " + name);
        }
    }

    private void FinalizeDialogBox(BaseDialogBox box)
    {
        GameObject go = box.InitializeElement();
        GameObject baseDialog = GameObject.Instantiate(baseDialogTemplate, Vector3.zero, Quaternion.identity);
        GameObject contentChild = baseDialog.transform.GetChild(1).gameObject;
        go.transform.SetParent(contentChild.transform);
        baseDialog.transform.SetParent(root.transform);
        RectTransform transform = baseDialog.GetComponent<RectTransform>();

        // since top/left/right/bottom are responsible for both the size
        // and the position we need to use some relational math.
        // effectively it is; L => -x, R => x, T => -y, B => y (=> is \propTo)
        // i.e. as x gets larger L gets smaller and R gets larger
        // thus shifts towards right while maintaining the size.
        float left = (box.prototype.size.left - (0.5f - box.prototype.position.x)) * Screen.width;
        float right = (box.prototype.size.right + (0.5f - box.prototype.position.x)) * Screen.width;
        float top = (box.prototype.size.top - (0.5f - box.prototype.position.y)) * Screen.height;
        float bottom = (box.prototype.size.bottom + (0.5f - box.prototype.position.y)) * Screen.height;

        transform.offsetMin = new Vector2(left, bottom);
        transform.offsetMax = new Vector2(-right, -top);
        box.wasModal = GameController.Instance.IsModal;
        GameController.Instance.IsModal = true;
    }

    private void LoadDialogs()
    {
        dialogBoxes.Clear();
        List<DialogBoxPrototype> prototypes = PrototypeManager.DialogBox.Values;
        for (int i = 0; i < prototypes.Count; i++)
        {
            DialogBoxPrototype prototype = prototypes[i];
            BaseDialogBox dialog = FunctionsManager.DialogBox.CreateInstance<BaseDialogBox>(prototype.classData.Type, true);
            dialog.InitializeLUA();
            dialog.prototype = prototype;
            dialog.parameterData = prototype.classData.Parameters;
            dialogBoxes.Add(prototype.Type, dialog);
        }
        UnityDebugger.Debugger.Log("DialogBox", "Loaded " + prototypes.Count + " Dialogs");
    }
}