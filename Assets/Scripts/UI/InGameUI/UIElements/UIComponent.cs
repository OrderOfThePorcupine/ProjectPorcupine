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
/// Represents data for a code UI class.
/// </summary>
public class UIComponent
{
    public UIComponent()
    {
        Parameters = new Parameter();
    }

    /// <summary>
    /// Construct a class of UIClassData.
    /// </summary>
    /// <param name="className"> The name of the UI element class. </param>
    /// <param name="parameterData"> Any parameter data to supply. </param>
    public UIComponent(string className, Parameter parameterData)
    {
        this.Type = className;
        this.Parameters = parameterData;
    }

    /// <summary>
    /// The class name.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Parameters for the class.
    /// </summary>
    public Parameter Parameters { get; private set; }public void ReadXml(XmlReader reader)
    {
        string className = reader.GetAttribute("ClassName");
        XmlReader subReader = reader.ReadSubtree();
        subReader.Read();
        if (string.IsNullOrEmpty(className) == false)
        {
            Type = className;
            if (subReader != null && subReader.ReadToDescendant("Params"))
            {
                Parameters = Parameter.ReadXml(subReader);
            }
            else
            {
                Parameters = new Parameter();
            }
        }
    }

    public void ReadJson(JToken jToken)
    {
        Type = (string)jToken["ClassName"];
        if (jToken["Parameters"] != null)
        {
            Parameters.FromJson(jToken["Parameters"]);
        }
    }
}