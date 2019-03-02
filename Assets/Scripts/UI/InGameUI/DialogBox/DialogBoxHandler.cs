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
    private GameObject root;
    private Stack<BaseDialogBox> currentDialogs = new Stack<BaseDialogBox>();

    public bool IsModal
    {
        get
        {
            return currentDialogs.Count > 0;
        }
    }

    public DialogBoxHandler(GameObject parent)
    {
        root = new GameObject("Dialog Boxes", typeof(RectTransform));
        root.transform.SetParent(parent.transform, false);
        root.transform.SetAsLastSibling();
        RectTransform transform = root.GetComponent<RectTransform>();
        transform.anchorMax = new Vector2(1, 1);
        transform.anchorMin = Vector2.zero;
        transform.position = Vector3.zero;
        transform.offsetMax = Vector2.zero;
        transform.offsetMin = Vector2.zero;
    }

    /// <summary>
    /// Closes the top dialog without saving changes.
    /// </summary>
    public void ForceCloseTopDialog()
    {
        BaseDialogBox box = currentDialogs.Pop();
        if (box != null) {
            box.result.AddParameter(new Parameter("ExitStatus", "Force"));
            ChangeInteractabilityOfBox(box, false);
            box.Destroy();
        }
        ChangeInteractabilityOfTopDialog(true);
    }

    /// <summary>
    /// Closes all dialogs without saving changes.
    /// </summary>
    public void ForceCloseAllDialogs()
    {
        while (currentDialogs.Count > 0)
        {
            BaseDialogBox box = currentDialogs.Pop();
            if (box != null) {
                box.result.AddParameter(new Parameter("ExitStatus", "Force"));
                ChangeInteractabilityOfBox(box, false);
                box.Destroy();
            }
        }
    }

    /// <summary>
    /// Closes the top dialog allowing it to use other dialogs and actions
    /// before it closes
    /// </summary>
    public void SoftCloseTopDialog()
    {
        BaseDialogBox box = currentDialogs.Pop();
        if (box != null) {
            box.result.AddParameter(new Parameter("ExitStatus", "Soft"));
            ChangeInteractabilityOfBox(box, false);
            box.Destroy();
        }
        ChangeInteractabilityOfTopDialog(true);
    }

    /// <summary>
    /// Closes all dialogs allowing it to use other dialogs and actions
    /// before it closes
    /// </summary>
    public void SoftCloseAllDialogs()
    {
        while (currentDialogs.Count > 0)
        {
            BaseDialogBox box = currentDialogs.Pop();
            if (box != null) {
                box.result.AddParameter(new Parameter("ExitStatus", "Soft"));
                ChangeInteractabilityOfBox(box, false);
                box.Destroy();
            }
        }
    }

    public void ShowDialogBox(string name, Dictionary<string, object> data = null, BaseDialogBox.OnCloseAction action = null)
    {
        DialogBoxPrototype proto = PrototypeManager.DialogBox.Get(name);
        BaseDialogBox box = FunctionsManager.DialogBox.CreateInstance<BaseDialogBox>(proto.classData.Type, false);
        if (box != null)
        {
            box.OnClose = action;
            box.callerData = data;
            box.prototype = proto;
            box.parameterData = proto.classData.Parameters;
            box.InitializeLUA();
            FinalizeDialogBox(box);
        }
        else
        {
            UnityDebugger.Debugger.LogError("DialogBox", "DialogBox doesn't exist: " + name);
        }
    }

    private void ChangeInteractabilityOfBox(BaseDialogBox box, bool interactability)
    {
        if (box != null)
        {
            box.root.GetComponent<CanvasGroup>().interactable = interactability;
        }
    }

    private void ChangeInteractabilityOfTopDialog(bool interactability)
    {
        if (currentDialogs.Count > 0)
        {
            ChangeInteractabilityOfBox(currentDialogs.Peek(), interactability);
        }
    }

    private void FinalizeDialogBox(BaseDialogBox box)
    {
        GameObject go = box.InitializeElement();
        GameObject resource = Resources.Load<GameObject>("UI/DialogBoxes/DialogBox_" + box.prototype.Background);
        GameObject baseDialog = GameObject.Instantiate(resource, Vector3.one, Quaternion.identity, root.transform);
        box.root = baseDialog;
        RectTransform transform = baseDialog.GetComponent<RectTransform>();
        Vector2 pos = box.prototype.position;
        BoxedDimensions size = box.prototype.size;

        transform.anchorMax = new Vector2(pos.x + size.right, pos.y + size.top);
        transform.anchorMin = new Vector2(pos.x - size.left, pos.y - size.bottom);
        transform.offsetMax = Vector2.zero;
        transform.offsetMin = Vector2.zero;

        go.transform.SetParent(baseDialog.transform, false);
        transform = go.GetComponent<RectTransform>();
        transform.localScale = Vector3.one;
        transform.anchorMax = Vector2.one;
        transform.anchorMin = Vector2.zero;
        transform.offsetMax = Vector2.zero;
        transform.offsetMin = Vector2.zero;

        //transform.anchorMin = new Vector2(pos.x - size.left / 2, pos.y - size.bottom / 2);
        //transform.anchorMax = new Vector2(pos.x + size.right / 2, pos.y + size.top / 2);

        ChangeInteractabilityOfTopDialog(false);
        ChangeInteractabilityOfBox(box, true);
        currentDialogs.Push(box);
    }
}
