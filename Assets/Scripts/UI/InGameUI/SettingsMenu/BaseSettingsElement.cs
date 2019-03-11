#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;

/// <summary>
/// A base UI element.
/// </summary>
[MoonSharp.Interpreter.MoonSharpUserData]
public abstract class BaseSettingsElement : BaseUIElement
{
    /// <summary>
    /// Internal option data.
    /// </summary>
    public Parameter parameterData;

    /// <summary>
    /// Option data for this element.
    /// </summary>
    public SettingsOption option;

    /// <summary>
    /// Has the value changed.
    /// </summary>
    public bool valueChanged;

    /// <summary>
    /// Apply your setting.  You can use variables in this.
    /// Do a setting.setSetting beforehand.
    /// </summary>
    public event EventHandler ApplySettingHandler;

    /// <summary>
    /// Undo your setting.  You should do a setting.getSetting call
    /// To get the latest setting info.
    /// </summary>
    public event EventHandler CancelSettingHandler;

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

    /// <summary>
    /// Apply setting data.
    /// </summary>
    public abstract void ApplySetting();

    /// <summary>
    /// Cancel setting data.
    /// </summary>
    public abstract void CancelSetting();

    /// <summary>
    /// Apply setting data, sends events to LUA or C# event handlers.
    /// </summary>
    public void ApplySettingLUA()
    {
        // Please C# 4.6
        EventHandler invoker = ApplySettingHandler;
        if (invoker != null)
        {
            invoker(this, null);
        }
    }

    /// <summary>
    /// Cancel setting data, sends events to LUA or C# event handlers.
    /// </summary>
    public void CancelSettingLUA()
    {
        // Please C# 4.6
        EventHandler invoker = CancelSettingHandler;
        if (invoker != null)
        {
            invoker(this, null);
        }
    }

    /// <summary>
    /// The name of the settings element.
    /// </summary>
    public override string GetName()
    {
        return option.name;
    }
}
