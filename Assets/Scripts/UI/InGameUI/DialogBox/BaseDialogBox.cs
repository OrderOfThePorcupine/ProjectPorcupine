#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// A base UI element.
/// </summary>
[MoonSharp.Interpreter.MoonSharpUserData]
public abstract class BaseDialogBox : BaseUIElement
{
    /// <summary>
    /// Actionable data.
    /// </summary>
    public IEnumerable<Actionable> actionableData;

    /// <summary>
    /// Data about this class
    /// </summary>
    public DialogBoxPrototype prototype;

    /// <summary>
    /// The root gameobject of this box.
    /// </summary>
    public GameObject root;

    /// <summary>
    /// Action to call upon exit of dialog box.
    /// </summary>
    public OnClose onClose;

    /// <summary>
    /// The result of action
    /// </summary>
    protected ActionResult result = ActionResult.None;

    public void OnClose()
    {
        if (onClose != null)
        {
            onClose(result);
        }
        GameObject.Destroy(root);
    }

    /// <summary>
    /// LUA Initializer.
    /// </summary>
    public void InitializeLUA()
    {
        // if (parameterData.ContainsKey("LUAInitializeFunction"))
        // {
        //     FunctionsManager.DialogBox.Call(parameterData["LUAInitializeFunction"].ToString(), this);
        // }
    }

    public override string GetName()
    {
        return prototype.Type;
    }
}
