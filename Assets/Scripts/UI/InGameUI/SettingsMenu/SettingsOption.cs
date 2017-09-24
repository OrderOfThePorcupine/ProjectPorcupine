#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================

#endregion
using System.Xml;
using Newtonsoft.Json.Linq;

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
    public UIComponent classData;

    /// <summary>
    /// Construct a settings option from parameters.
    /// </summary>
    /// <param name="name"> The name of the option (unlocalized). </param>
    /// <param name="key"> The key to save. </param>
    /// <param name="defaultValue"> The default value. </param>
    /// <param name="classData"> Any class data associated with the option. </param>
    public SettingsOption(string name, string key, string defaultValue, UIComponent classData)
    {
        this.name = name;
        this.key = key;
        this.defaultValue = defaultValue;
        this.classData = classData;
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
        classData = new UIComponent(reader.GetAttribute("ClassName"), (reader != null && subReader.ReadToDescendant("Params")) ? Parameter.ReadXml(reader) : new Parameter());
        subReader.Close();
    }

    /// <summary>
    /// A nice little helper (pass it a reader class that is up to the subtree).
    /// </summary>
    public SettingsOption(JToken innerJson)
    {
        name = PrototypeReader.ReadJson("name", innerJson["Name"]);
        //JToken innerJson = jsonProto.Value;
        key = PrototypeReader.ReadJson("key", innerJson["Key"]);
        defaultValue = PrototypeReader.ReadJson("defaultValue", innerJson["DefaultValue"]);
        classData = new UIComponent();

        classData.ReadJson(innerJson);
    }
}
