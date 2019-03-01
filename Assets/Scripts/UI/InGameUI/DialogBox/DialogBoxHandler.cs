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
    private GameObject baseDialogTemplate;

    private Stack<BaseDialogBox> currentDialogs;

    public bool IsModal
    {
        get
        {
            return currentDialogs.Count > 0;
        }
    }

    public DialogBoxHandler(GameObject parent, GameObject baseTemplate)
    {
        root = new GameObject("Dialog Boxes", typeof(RectTransform));
        root.transform.SetParent(parent.transform, false);
        root.transform.SetAsLastSibling();
        baseDialogTemplate = baseTemplate;
    }

    /// <summary>
    /// Closes the top dialog without saving changes.
    /// </summary>
    public void ForceCloseTopDialog()
    {
        currentDialogs.Pop().ForceCloseDialog();
    }

    /// <summary>
    /// Closes all dialogs without saving changes.
    /// </summary>
    public void ForceCloseAllDialogs()
    {
        currentDialogs.Pop().ForceCloseDialog();
    }

    /// <summary>
    /// Closes the top dialog allowing it to use other dialogs and actions
    /// before it closes
    /// </summary>
    public void SoftCloseTopDialog()
    {
        currentDialogs.Pop().SoftCloseDialog();
    }

    /// <summary>
    /// Closes all dialogs allowing it to use other dialogs and actions
    /// before it closes
    /// </summary>
    public void SoftCloseAllDialogs()
    {
        currentDialogs.Pop().SoftCloseDialog();
    }

    public void ShowDialogBox(string name, Dictionary<string, object> data = null, BaseDialogBox.OnCloseAction action = null)
    {
        BaseDialogBox box = FunctionsManager.DialogBox.CreateInstance<BaseDialogBox>(name, false);
        if (box != null)
        {
            box.OnClose = action;
            box.callerData = data;
            DialogBoxPrototype proto = PrototypeManager.DialogBox.Get(name);
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

    private void FinalizeDialogBox(BaseDialogBox box)
    {
        GameObject go = box.InitializeElement();
        GameObject baseDialog = GameObject.Instantiate(baseDialogTemplate, Vector3.one, Quaternion.identity, root.transform);
        GameObject contentChild = baseDialog.transform.GetChild(0).gameObject;
        go.transform.SetParent(contentChild.transform);
        RectTransform transform = baseDialog.GetComponent<RectTransform>();
        Vector2 pos = box.prototype.position;
        BoxedDimensions size = box.prototype.size;

        /*
          since the UI top/left/right/bottom are responsible for both the size
          and the position we need to use some relational math.
          effectively it is; L => -x, R => x, T => -y, B => y (=> is \propTo)
          i.e. as x gets larger L gets smaller and R gets larger
          thus shifts towards right while maintaining the size.

          Note: this is not talking about BoxedDimensions.top/left/right/bottom
          this is purely talking about Rect.top/left/bottom/right
        */

        /* == The calculation can derived as per the following ==
         * pos.x and pos.y in reality start at the centre rather than the
           bottom left so to convert to what we will use subtract 0.5 from them
         * To calculate the percentage that the left is going to extend
           from the centre just add it to the adjusted pos.x
           * To calculate the right you subtract the adjusted pos.x from it
             you subtract because if pos.x is large you want it to move towards
             the right that is the distance from the right gets smaller
           * For top and bottom the same math applies but you use y instead
           * That is the adjusted y is added to the top and subtracted from
             the bottom
         * Next since this is just a percentage we need to multiply it by
           Screen.width and height to get the delta width/height
           * That is the result will be width - AdjustedLeft or Right * width
           * And height - AdjustedTop or Bottom * height
           * That is you can think of it like an additive inverse i.e. `a - ax`
             * Which is really just a(1 - x)
        */

        float adjustedPosX = pos.x - 0.5f;
        float adjustedPosY = pos.y - 0.5f;

        float left = Screen.width * (1 - size.left + adjustedPosX);
        float right = Screen.width * (1 - size.right - adjustedPosX);
        float top = Screen.height * (1 - size.top + adjustedPosY);
        float bottom = Screen.height * (1 - size.bottom - adjustedPosY);

        transform.offsetMin = new Vector2(left, bottom);
        transform.offsetMax = new Vector2(-right, -top);
        currentDialogs.Push(box);
    }
}