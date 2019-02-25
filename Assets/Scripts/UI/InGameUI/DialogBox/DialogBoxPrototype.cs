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
public class DialogBoxPrototype : IPrototypable
{
    public delegate void OnCloseAction(Parameter result);

    /// <summary>
    /// Data about this class
    /// </summary>
    public UIComponent classData;

    /// <summary>
    /// The dimensions of the dialog box.
    /// </summary>
    public BoxedDimensions size;

    /// <summary>
    /// The position of the dialog box.
    /// </summary>
    public Vector2 position;

    /// <summary>
    /// The name of the dialog box
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Reads the prototype from the specified JObject.
    /// </summary>
    /// <param name="jsonProto">The JProperty containing the prototype.</param>
    public void ReadJsonPrototype(JProperty jsonProto)
    {
        Type = jsonProto.Name;
        string err = "";
        float? x = BoxedDimensions.ParsePercentage(jsonProto.Value["Position"]["x"], ref err);
        float? y = BoxedDimensions.ParsePercentage(jsonProto.Value["Position"]["y"], ref err);

        if (!string.IsNullOrEmpty(err))
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Error occurred in parsing position: " + err);
        }

        position = new Vector2(x ?? 0f, y ?? 0f);
        size = BoxedDimensions.ReadJsonPrototype(jsonProto.Value["Dimensions"]);
        classData = new UIComponent();
        classData.ReadJson(jsonProto.Value);
    }
}
