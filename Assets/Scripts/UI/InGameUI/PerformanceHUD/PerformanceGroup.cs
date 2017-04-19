#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Collections.Generic;
using System.Xml;

public class PerformanceGroup : IPrototypable
{
    /// <summary>
    /// What classes to show in each group.
    /// </summary>
    public List<string> componentData = new List<string>();

    /// <summary>
    /// Parameter data to pass to classes to implement.
    /// </summary>
    public List<Parameter> parameterData = new List<Parameter>();

    /// <summary>
    /// Disable UI or not.
    /// </summary>
    public bool disableUI;

    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    /// <param name="name"> The name of the group. </param>
    /// <param name="componentData"> The element data for names of classes to show. </param>
    /// <param name="parameterData"> Parameter data to pass to the classes. </param>
    /// <param name="disableUI"> Disable UI or not. </param>
    public PerformanceGroup(string name, List<string> componentData, List<Parameter> parameterData, bool disableUI)
    {
        this.Type = name;
        this.componentData = componentData;
        this.disableUI = disableUI;
        this.parameterData = parameterData;
    }

    /// <summary>
    /// Empty constructor.
    /// </summary>
    public PerformanceGroup()
    {
    }

    /// <summary>
    /// The type of the performance group.
    /// In JSON its the 'Name' of the component group.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Reads from the reader provided.
    /// </summary>
    public void ReadXmlPrototype(XmlReader parentReader)
    {
        Type = parentReader.GetAttribute("Name");

        if (Type == null)
        {
            throw new System.Exception("Type ('Name') doesn't exist in component group.");
        }

        string disableUIText = parentReader.GetAttribute("DisableUI");
        bool disableUI = string.IsNullOrEmpty(disableUIText) ? false : XmlConvert.ToBoolean(disableUIText.ToLower());

        XmlReader reader = parentReader.ReadSubtree();

        while (reader.Read())
        {
            if (reader.Name == "Component")
            {
                reader.MoveToContent();
                string className = reader.GetAttribute("ClassName");

                if (string.IsNullOrEmpty(className) == false)
                {
                    componentData.Add(reader.GetAttribute("ClassName"));
                    parameterData.Add(reader != null && reader.ReadToDescendant("Params") ? Parameter.ReadXml(reader) : new Parameter());
                }
            }
        }
    }
}
