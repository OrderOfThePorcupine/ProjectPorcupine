#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Xml;

/// <summary>
/// For XML reader.
/// </summary>
public class SettingsOption
{
    public string name;
    public string key;
    public string defaultValue;
    public string className;
    public Parameter options;

    public SettingsOption(string name, string key, string defaultValue, string className, Parameter parameter = null)
    {
        this.name = name;
        this.key = key;
        this.defaultValue = defaultValue;
        this.className = className;
        this.options = parameter;
    }

    /// <summary>
    /// A nice little helper (pass it a reader class that is up to the subtree).
    /// </summary>
    public SettingsOption(XmlReader reader)
    {
        name = reader.GetAttribute("Name");
        key = reader.GetAttribute("Key");
        defaultValue = reader.GetAttribute("DefaultValue");
        className = reader.GetAttribute("ClassName");
        this.options = (reader != null && reader.ReadToDescendant("Params")) ? Parameter.ReadXml(reader) : new Parameter();
    }
}