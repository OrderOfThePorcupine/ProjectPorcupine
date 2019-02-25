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
    public delegate void OnCloseAction(Parameter result);

    /// <summary>
    /// The result of the box.
    /// </summary>
    public Parameter result;

    /// <summary>
    /// Extra data from the caller.
    /// </summary>
    public Dictionary<string, object> callerData;

    /// <summary>
    /// Data about this class
    /// </summary>
    public DialogBoxPrototype prototype;

    /// <summary>
    /// On close of the dialog box.
    /// </summary>
    public OnCloseAction OnClose;

    // Was the game controller modal before this dialog
    public bool wasModal;

    protected void CloseDialog()
    {
        OnClose(result);
        OnClose = null;
        callerData = null;
        if (!wasModal)
        {
            GameController.Instance.IsModal = false;
        }
        wasModal = false;
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

    public string GetStringParam(string key, bool require = true)
    {
        string val = null;
        if (callerData != null && callerData.ContainsKey(key)) 
        {
            val = (string)callerData[key];
        }
        else if (parameterData.ContainsKey(key))
        {
            val = parameterData[key].ToString();
        }
        else if (require)
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Was expecting " + key + " parameter data");
        }

        return val;
    }

    public float? GetFloatParam(string key, bool require = true)
    {
        float? val = null;
        if (callerData != null && callerData.ContainsKey(key)) 
        {
            val = (float)callerData[key];
        }
        else if (parameterData.ContainsKey(key))
        {
            val = parameterData[key].ToFloat();
        }
        else if (require)
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Was expecting " + key + " parameter data");
        }

        return val;
    }

    public int? GetIntParam(string key, bool require = true)
    {
        int? val = null;
        if (callerData != null && callerData.ContainsKey(key)) 
        {
            val = (int)callerData[key];
        }
        else if (parameterData.ContainsKey(key))
        {
            val = parameterData[key].ToInt();
        }
        else if (require)
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Was expecting " + key + " parameter data");
        }

        return val;
    }

    public bool? GetBoolParam(string key, bool require = true)
    {
        bool? val = null;
        if (callerData != null && callerData.ContainsKey(key)) 
        {
            val = (bool)callerData[key];
        }
        else if (parameterData.ContainsKey(key))
        {
            val = parameterData[key].ToBool();
        }
        else if (require)
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Was expecting " + key + " parameter data");
        }

        return val;
    }

    public string[] GetStringArray(string key, bool require = true, params char[] separators)
    {
        string[] res = null;
        if (callerData.ContainsKey(key))
        {
            res = (string[])callerData[key];
        }
        else if (parameterData.ContainsKey(key))
        {
            // comma separated list
            res = parameterData[key].ToString().Split(separators);
        }
        else if (require)
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Was expecting " + key + " parameter data");
        }

        return res;
    }

    public override string GetName()
    {
        return prototype.Type;
    }
}
