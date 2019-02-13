#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

/// <summary>
/// Holds the category and its options (and name).
/// There is no need to implement a write method (to write to an xml document),
/// since the template will only ever be read from.
/// </summary>
public class SettingsCategory : IPrototypable
{
    public Dictionary<string, List<SettingsOption>> headings = new Dictionary<string, List<SettingsOption>>();

    /// <summary>
    /// The type of the category.
    /// In JSON its the 'Name' of the category.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Reads the prototype from the specified JObject.
    /// </summary>
    /// <param name="jsonProto">The JProperty containing the prototype.</param>
    public void ReadJsonPrototype(JProperty jsonProto)
    {
        Type = jsonProto.Name;
        JToken innerJson = jsonProto.Value["Headings"];
        foreach (JProperty heading in innerJson)
        {
            string headingName = heading.Name;
            List<SettingsOption> options = new List<SettingsOption>();
            foreach (JToken optionToken in heading.Value)
            {
                options.Add(new SettingsOption(optionToken));
            }

            headings.Add(headingName, options);
        }
    }
}