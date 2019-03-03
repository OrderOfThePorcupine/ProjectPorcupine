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
public class DialogBoxManager : MonoBehaviour
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

    /// <summary>
    /// Create a new handler.
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static DialogBoxManager CreateDialogBoxManager(GameObject parent)
    {
        GameObject root = new GameObject("Dialog Boxes", typeof(RectTransform));
        root.transform.SetParent(parent.transform, false);
        root.transform.SetAsLastSibling();
        RectTransform transform = root.GetComponent<RectTransform>();
        transform.anchorMax = new Vector2(1, 1);
        transform.anchorMin = Vector2.zero;
        transform.position = Vector3.zero;
        transform.offsetMax = Vector2.zero;
        transform.offsetMin = Vector2.zero;
        DialogBoxManager dialogBoxManager = root.AddComponent<DialogBoxManager>();
        dialogBoxManager.root = root;
        return dialogBoxManager;
    }

    public static DialogBoxManager FindInstance()
    {
        if (MainMenuController.Instance != null)
        {
            return MainMenuController.Instance.DialogBoxManager;
        }
        else if (WorldController.Instance.DialogBoxManager != null)
        {
            return WorldController.Instance.DialogBoxManager;
        }
        else
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Couldn't find DialogBoxManager");
            return null;
        }
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

    /// <summary>
    /// Shows a specific dialog box with custom data and action upon exit.
    /// </summary>
    /// <param name="name">The name of the dialog box to open</param>
    /// <param name="data">Custom data to pass in</param>
    /// <param name="action">Upon exit of the dialog box this function is called</param>
    /// <param name="closeAfterSeconds">Close the top dialog box after this amount of time</param>
    /// <remarks>
    ///     "closeAfterSeconds" relies that the top dialog after that amount
    ///     of time will be equal to the one spawned by this function.
    /// </remarks>

    public void ShowDialogBox(string name, Dictionary<string, object> data = null, BaseDialogBox.OnCloseAction action = null, float? closeAfterSeconds = null)
    {
        DialogBoxPrototype proto = PrototypeManager.DialogBox.Get(name);
        if (proto == null)
        {
            UnityDebugger.Debugger.LogError("DialogBox", "DialogBoxPrototype doesn't exist: " + name);
        }

        BaseDialogBox box = FunctionsManager.DialogBox.CreateInstance<BaseDialogBox>(proto.classData.Type, false);
        if (box == null)
        {
            UnityDebugger.Debugger.LogError("DialogBox", "DialogBox doesn't exist: " + name);
        }

        box.OnClose = action;
        box.callerData = data ?? new Dictionary<string, object>();
        box.prototype = proto;
        box.parameterData = proto.classData.Parameters;
        box.InitializeLUA();
        FinalizeDialogBox(box);

        if (closeAfterSeconds.HasValue)
        {
            Invoke("SoftCloseTopDialog", closeAfterSeconds.Value);
        }
    }

    /// <summary>
    /// Enables/Disables if a box can be interacted by the event system.
    /// </summary>
    /// <param name="box">The box to change</param>
    /// <param name="interactability">True means it can be interacted with</param>
    private void ChangeInteractabilityOfBox(BaseDialogBox box, bool interactability)
    {
        if (box != null)
        {
            box.root.GetComponent<CanvasGroup>().interactable = interactability;
        }
    }

    /// <summary>
    /// Enables/Disables if the top box can be interacted by the event system.
    /// </summary>
    /// <param name="interactability">True means it can be interacted with</param>
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

        ChangeInteractabilityOfTopDialog(false);
        ChangeInteractabilityOfBox(box, true);
        currentDialogs.Push(box);
    }
}
