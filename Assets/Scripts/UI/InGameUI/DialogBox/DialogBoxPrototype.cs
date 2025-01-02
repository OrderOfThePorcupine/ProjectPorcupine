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
    /// <summary>
    /// The dimensions of the dialog box.
    /// </summary>
    public BoxedDimensions size { get; private set; }

    /// <summary>
    /// The position of the dialog box.
    /// </summary>
    public Vector2 position { get; private set; }

    /// <summary>
    /// The name of the dialog box
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// The type of background.
    /// </summary>
    public string Background { get; private set; }

    /// <summary>
    /// Associated creator function for this prototype to call.
    /// If null you should use the constructor of the className to build
    /// </summary>
    public string CreatorFunction { get; private set; }

    /// <summary>
    /// This classname to instantiate for this dialog box.
    /// </summary>
    public string ClassName { get; private set; }

    /// <summary>
    /// Reads the prototype from the specified JObject.
    /// </summary>
    /// <param name="jsonProto">The JProperty containing the prototype.</param>
    public void ReadJsonPrototype(JProperty jsonProto)
    {
        Type = jsonProto.Name;

        if (jsonProto.Value["ClassName"] != null)
        {
            ClassName = jsonProto.Value["ClassName"].ToString();
        }

        if (jsonProto.Value["CreatorFunction"] != null)
        {
            CreatorFunction = jsonProto.Value["CreatorFunction"].ToString();
        }

        if (ClassName != null && CreatorFunction != null)
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Can't have both a creator function and a class name, choosing creator function");
        }
        else if (ClassName == null && CreatorFunction == null)
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Needs either a creator function or a class name.");
        }

        string err = "";
        float? x = BoxedDimensions.ParsePercentage(jsonProto.Value["Position"]["x"], ref err);
        float? y = BoxedDimensions.ParsePercentage(jsonProto.Value["Position"]["y"], ref err);

        if (!string.IsNullOrEmpty(err))
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Error occurred in parsing position: " + err);
        }

        if (jsonProto.Value["Background"] != null)
        {
            Background = jsonProto.Value["Background"].ToString();
        }
        else
        {
            Background = "general";
        }

        position = new Vector2(x ?? 0f, y ?? 0f);
        size = BoxedDimensions.ReadJsonPrototype(jsonProto.Value["Dimensions"]);
    }
}
