#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System.Xml;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;

[MoonSharpUserData]
public class QuestGoal
{
    public string Description { get; set; }

    public string IsCompletedLuaFunction { get; set; }

    public Parameter Parameters { get; set; }

    public bool IsCompleted { get; set; }

    public void ReadXmlPrototype(XmlReader reader_parent)
    {
        Description = reader_parent.GetAttribute("Description");
        IsCompletedLuaFunction = reader_parent.GetAttribute("IsCompletedLuaFunction");

        XmlReader reader = reader_parent.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Params":
                    Parameters = Parameter.ReadXml(reader);
                    break;
            }
        }
    }

    public void ReadJsonPrototype(JToken token)
    {
        if (token != null)
        {
            Description = PrototypeReader.ReadJson(Description, token["Description"]);
            IsCompletedLuaFunction = PrototypeReader.ReadJson(IsCompletedLuaFunction, token["IsCompletedLuaFunction"]);

            Parameters = new Parameter();
            Parameters.FromJson(token["Parameters"]);
        }
    }
}