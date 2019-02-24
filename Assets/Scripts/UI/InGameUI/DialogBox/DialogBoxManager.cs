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

    public DialogBoxHandler(GameObject parent)
    {
        root = new GameObject("Dialog Boxes");
        root.transform.SetParent(parent.transform);
        root.transform.SetAsLastSibling();
        LoadDialogs();
    }

    public void CreateDialogBox(string name, BaseDialogBox.OnCloseAction action = null)
    {
        if (dialogBoxes.ContainsKey(name))
        {
            dialogBoxes[name].OnClose = action;
            
            dialogBoxes[name].InitializeElement().transform.SetParent(root.transform);
        }
        else
        {
            UnityDebugger.Debugger.LogError("DialogBox", "DialogBox doesn't exist: " + name);
        }
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