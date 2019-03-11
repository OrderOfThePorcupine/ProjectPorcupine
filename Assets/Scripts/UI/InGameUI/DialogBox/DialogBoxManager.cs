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
    public int modalCount;

    public bool IsModal
    {
        get
        {
            return (modalCount + currentDialogs.Count) > 0;
        }
        set
        {
            modalCount += value ? 1 : -1;
        }
    }

    /// <summary>
    /// Create a new handler.
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static DialogBoxManager CreateDialogBoxManager(GameObject parentCanvas)
    {
        Canvas canvas = new GameObject("Canvas-DialogBoxes", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UIRescaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvas.transform.SetParent(parentCanvas.transform);
        canvas.transform.SetAsLastSibling();
        RectTransform transform = canvas.GetComponent<RectTransform>();
        transform.anchorMax = new Vector2(1, 1);
        transform.anchorMin = Vector2.zero;
        transform.position = Vector3.zero;
        transform.offsetMax = Vector2.zero;
        transform.offsetMin = Vector2.zero;
        DialogBoxManager dialogBoxManager = canvas.gameObject.AddComponent<DialogBoxManager>();
        dialogBoxManager.root = canvas.gameObject;
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
    /// Closes all dialogs without saving changes.
    /// </summary>
    public void ForceCloseAllDialogs()
    {
        while (currentDialogs.Count > 0)
        {
            BaseDialogBox box = currentDialogs.Pop();
            if (box != null) {
                Destroy(box.root);
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
            ChangeInteractabilityOfBox(box, false);
            box.OnClose();
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
                ChangeInteractabilityOfBox(box, false);
                box.OnClose();
            }
        }
    }

    private BaseDialogBox GetDialogBox(string name, params object[] args)
    {
        DialogBoxPrototype proto = PrototypeManager.DialogBox.Get(name);
        if (proto == null)
        {
            UnityDebugger.Debugger.LogError("DialogBox", "DialogBoxPrototype doesn't exist: " + name);
        }
        BaseDialogBox box;

        if (proto.ClassName != null)
        {
            box = FunctionsManager.DialogBox.CreateInstance<BaseDialogBox>(proto.ClassName, false, args);
        }
        else if (proto.CreatorFunction != null)
        {
            box = FunctionsManager.DialogBox.Call<BaseDialogBox>(proto.CreatorFunction, args);
        }
        else
        {
            UnityDebugger.Debugger.LogError("DialogBox", "DialogBox doesn't have a creator function or class type: " + name);
            return null;
        }

        box.prototype = proto;
        return box;
    }

    /// <summary>
    /// Show a prompt styled dialog box.
    /// </summary>
    /// <param name="name">The name of the dialog box to open</param>
    /// <param name="actions">Actionables that the dialog should support</param>
    /// <param name="args">Args to pass onto the init function</param>
    public void ShowDialogBox(string name, DialogBoxActionBuilder actions = null, OnClose onClose = null, params object[] args)
    {
        BaseDialogBox box = GetDialogBox(name, args);
        if (box == null) { return; }
        box.actionableData = actions;
        box.onClose = onClose;
        box.InitializeLUA();
        FinalizeDialogBox(box);
    }

    /// <summary>
    /// Show a prompt styled dialog box.
    /// </summary>
    /// <param name="name">The name of the dialog box to open</param>
    /// <param name="actions">Actionables that the dialog should support</param>
    /// <param name="args">Args to pass onto the init function</param>
    public void ShowDialogBox(string name, DialogBoxActionBuilder actions, params object[] args)
    {
        ShowDialogBox(name, actions, null, args);
    }

    /// <summary>
    /// Show a dialog box for a certain amount of time.
    /// </summary>
    /// <param name="name">The name of the dialog box to open</param>
    /// <param name="actions">Actionables that the dialog should support</param>
    /// <param name="args">Args to pass onto the init function</param>
    public void ShowDialogBox(string name, OnClose onClose, params object[] args)
    {
        ShowDialogBox(name, null, onClose, args);
    }

    /// <summary>
    /// Show a temporary dialog box.
    /// You can't have any conditional actions since it can't close before
    /// the timeout.
    /// </summary>
    /// <param name="name">The name of the dialog box to open</param>
    /// <param name="timeout">The time in seconds to wait</param>
    /// <param name="args">Args to pass onto the init function</param>
    public void ShowTemporaryDialogBox(string name, float timeout, params object[] args)
    {
        BaseDialogBox box = GetDialogBox(name, args);
        if (box == null) { return; }
        box.InitializeLUA();
        FinalizeDialogBox(box);
        Invoke("SoftCloseTopDialog", timeout);
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
