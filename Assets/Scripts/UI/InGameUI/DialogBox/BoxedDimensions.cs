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
/// Each element corresponds to a bounded value from 0 to 1 representing the
/// percentage of that screen axis.
/// </summary>
public struct BoxedDimensions {
    public float top;
    public float bottom;
    public float right;
    public float left;

    public BoxedDimensions(float top, float bottom, float right, float left)
    {
        this.top = top;
        this.bottom = bottom;
        this.right = right;
        this.left = left;
    }

    private static float? ParsePercentage(JToken val, ref string err)
    {
        if (val.Type == JTokenType.String)
        {
            // percentage
            string res = val.ToObject<string>();
            float flt;
            if (string.IsNullOrEmpty(res) || res[res.Length - 1] != '%' ||
                !float.TryParse(res, out flt))
            {
                err += "Was expecting a percentage (i.e. 20%) got: " + res + "\n";
                return null;
            }
            return flt;
        }
        else if (val.Type == JTokenType.Float)
        {
            float res = val.ToObject<float>();
            if (res < 0.0 || res > 1.0)
            {
                err += "Invalid percentage value (0.0 <= x <= 1.0): " + res + "\n";
                return null;
            }
            return res;
        }
        else if (val.Type == JTokenType.Integer)
        {
            int res = val.ToObject<int>();
            if (res < 0 || res > 1)
            {
                err += "Out of range integer value (0 <= x <= 1) perhaps you were missing the '%' sign: " + res + "\n";
                return null;
            }
            return (float)res;
        }
        else
        {
            err += "Invalid Type for percentage: " + val.Type.ToString() + "\n";
            return null;
        }
    }

    public static BoxedDimensions ReadJsonPrototype(JToken jsonProto)
    {
        /*
        The following synonyms are allowed:
          - top + bottom == height or y (divide by 2 to get top/bottom values)
          - left + right == width or x (divide by 2 to get left/right values)
          - You can either give it in a value from 0 to 1 or a percentage
        */

        float? top = null;
        float? bottom = null;
        float? right = null;
        float? left = null;

        string err = string.Empty;
        float? res;

        foreach (JProperty property in jsonProto)
        {
            switch (property.Name.ToLower())
            {
                case "top":
                    if (top.HasValue) { err += "top already has a value\n"; }
                    top = ParsePercentage(property.Value, ref err);
                    break;
                case "bottom":
                    if (bottom.HasValue) { err += "bottom already has a value\n"; }
                    bottom = ParsePercentage(property.Value, ref err);
                    break;
                case "left":
                    if (left.HasValue) { err += "left already has a value\n"; }
                    left = ParsePercentage(property.Value, ref err);
                    break;
                case "right":
                    if (right.HasValue) { err += "right already has a value\n"; }
                    right = ParsePercentage(property.Value, ref err);
                    break;
                case "width":
                case "x":
                    res = ParsePercentage(property.Value, ref err);
                    if (res.HasValue)
                    {
                        // basically if error has a value the resultant object
                        // doesn't have to be well defined so this is just easier
                        if (left.HasValue) { err += "left already has a value\n"; }
                        if (right.HasValue) { err += "right already has a value\n"; }
                        right = left = res.Value / 2;
                    }
                    break;
                case "height":
                case "y":
                    res = ParsePercentage(property.Value, ref err);
                    if (res.HasValue)
                    {
                        // basically if error has a value the resultant object
                        // doesn't have to be well defined so this is just easier
                        if (bottom.HasValue) { err += "bottom already has a value\n"; }
                        if (top.HasValue) { err += "top already has a value\n"; }
                        top = bottom = res.Value / 2;
                    }
                    break;
                default:
                    err += "Invalid Property: " + property.Name + "\n";
                    break;
            }
        }

        if (!top.HasValue) { err += "Missing top value\n"; }
        if (!bottom.HasValue) { err += "Missing bottom value\n"; }
        if (!right.HasValue) { err += "Missing right value\n"; }
        if (!left.HasValue) { err += "Missing left value\n"; }

        // Check if we have initialised all values
        if (!string.IsNullOrEmpty(err))
        {
            UnityDebugger.Debugger.LogError("DialogBox", "Incorrect Dimensions/Position: \n" + err);
        }

        return new BoxedDimensions(top ?? 0f, bottom ?? 0f, right ?? 0f, left ?? 0f);
    }
}