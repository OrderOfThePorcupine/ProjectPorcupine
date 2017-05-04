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
/// A struct representing data of a settings option.
/// </summary>
public struct SettingsOption
{
    /// <summary>
    /// The name of this settings option.
    /// </summary>
    public string name;

    /// <summary>
    /// Tool tip to display in UI Mode.
    /// </summary>
    public string tooltip;

    /// <summary>
    /// The key to save for this option.
    /// </summary>
    public string key;

    /// <summary>
    /// The default value for this option.
    /// </summary>
    public string defaultValue;

    /// <summary>
    /// Class data associated with this option.
    /// </summary>
    public UIClassData classData;

    /// <summary>
    /// Construct a settings option from parameters.
    /// </summary>
    /// <param name="name"> The name of the option (unlocalized). </param>
    /// <param name="key"> The key to save. </param>
    /// <param name="defaultValue"> The default value. </param>
    /// <param name="tooltip"> Tooltip to show. </param>
    /// <param name="classData"> Any class data associated with the option. </param>
    public SettingsOption(string name, string key, string defaultValue, string tooltip, UIClassData classData)
    {
        this.name = name;
        this.key = key;
        this.defaultValue = defaultValue;
        this.classData = classData;
        this.tooltip = tooltip;
    }

    /// <summary>
    /// A nice little helper.
    /// </summary>
    public SettingsOption(XmlReader reader)
    {
        XmlReader subReader = reader.ReadSubtree();
        name = reader.GetAttribute("Name");
        key = reader.GetAttribute("Key");
        defaultValue = reader.GetAttribute("DefaultValue");
        tooltip = reader.GetAttribute("Tooltip");
        classData = new UIClassData(reader.GetAttribute("ClassName"), (reader != null && subReader.ReadToDescendant("Params")) ? Parameter.ReadXml(reader) : new Parameter());
        subReader.Close();
    }
}
