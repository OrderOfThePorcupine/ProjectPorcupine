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

/// <summary>
/// A base UI element.
/// </summary>
[MoonSharp.Interpreter.MoonSharpUserData]
public abstract class BaseDialogBox : BaseUIElement
{
    public delegate void OnCloseAction(Parameter result);

    /// <summary>
    /// The result of the box.
    /// </summary>
    public Parameter result;

    /// <summary>
    /// Data about this class
    /// </summary>
    public DialogBoxPrototype prototype;

    /// <summary>
    /// On close of the dialog box.
    /// </summary>
    public OnCloseAction OnClose;

    public void CloseDialog()
    {
        OnClose(result);
        OnClose = null;
    }

    /// <summary>
    /// LUA Initializer.
    /// </summary>
    public void InitializeLUA()
    {
        if (parameterData.ContainsKey("LUAInitializeFunction"))
        {
            FunctionsManager.SettingsMenu.Call(parameterData["LUAInitializeFunction"].ToString(), this);
        }
    }

    public override string GetName()
    {
        return prototype.Type;
    }
}
