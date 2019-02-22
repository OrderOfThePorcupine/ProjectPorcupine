﻿#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json.Linq;

/// <summary>
/// A group of performance components.
/// </summary>
public class PerformanceGroup : IPrototypable
{
    /// <summary>
    /// What classes to show in each group.
    /// </summary>
    public List<UIComponent> componentData = new List<UIComponent>();

    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    /// <param name="name"> The name of the group. </param>
    /// <param name="classData"> Class data for the group. </param>
    public PerformanceGroup(string name, List<UIComponent> classData)
    {
        this.Type = name;
        this.componentData = classData;
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

        XmlReader reader = parentReader.ReadSubtree();

        while (reader.Read())
        {
            if (reader.Name == "Component")
            {
                reader.MoveToContent();
                string className = reader.GetAttribute("ClassName");

                if (string.IsNullOrEmpty(className) == false)
                {
                    componentData.Add(new UIComponent(reader.GetAttribute("ClassName"), reader != null && reader.ReadToDescendant("Params") ? Parameter.ReadXml(reader) : new Parameter()));
                }
            }
        }
    }

    /// <summary>
    /// Reads the prototype from the specified JProperty.
    /// </summary>
    /// <param name="jsonProto">The JProperty containing the prototype.</param>
    public void ReadJsonPrototype(JProperty jsonProto)
    {
        Type = jsonProto.Name;
        JToken innerJson = jsonProto.Value;

        foreach (var item in innerJson["Components"])
        {
            UIComponent component = new UIComponent();
            component.ReadJson(item);

            componentData.Add(component);
        }
    }
}
