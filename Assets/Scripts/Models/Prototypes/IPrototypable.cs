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

public interface IPrototypable
{
    /// <summary>
    /// Gets the Type of the prototype.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Reads the prototype from the specified XML reader.
    /// </summary>
    /// <param name="readerParent">The XML reader to read from.</param>
    void ReadXmlPrototype(XmlReader reader);

    /// <summary>
    /// Reads the prototype from the specified JObject.
    /// </summary>
    /// <param name="readerParent">The JObject containing the prototype.</param>
    void ReadJsonPrototype(JProperty jsonProto);
}
