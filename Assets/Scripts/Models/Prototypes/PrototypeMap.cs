#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// A class that holds prototypes to be used later.
/// </summary>
public class PrototypeMap<T> where T : IPrototypable, new()
{
    private readonly Dictionary<string, T> prototypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrototypeMap`1"/> class.
    /// </summary>
    public PrototypeMap()
    {
        this.prototypes = new Dictionary<string, T>();
    }

    /// <summary>
    /// Gets the prototype keys.
    /// </summary>
    /// <value>The prototype keys.</value>
    public Dictionary<string, T>.KeyCollection Keys
    {
        get
        {
            return prototypes.Keys;
        }
    }

    /// <summary>
    /// Gets the prototype Values.
    /// </summary>
    /// <value>The prototype values.</value>
    public List<T> Values
    {
        get
        {
            return prototypes.Values.ToList();
        }
    }

    /// <summary>
    /// Gets the prototypes count.
    /// </summary>
    /// <value>The prototypes count.</value>
    public int Count
    {
        get
        {
            return prototypes.Count;
        }
    }

    /// <summary>
    /// Returns the prototype at the specified index.
    /// </summary>
    /// <param name="index">The prototype index.</param>
    /// <returns>The prototype.</returns>
    public T this[int index]
    {
        get
        {
            return prototypes.ElementAt(index).Value;
        }
    }

    /// <summary>
    /// Determines whether there is a prototype with the specified type.
    /// </summary>
    /// <returns><c>true</c> if there is a prototype with the specified type; otherwise, <c>false</c>.</returns>
    /// <param name="type">The prototype type.</param>
    [System.Obsolete("Has is deprecated, please use TryGet instead")]
    public bool Has(string type)
    {
        return prototypes.ContainsKey(type);
    }

    /// <summary>
    /// Tries to get a specified prototype.
    /// </summary>
    /// <param name="type">The prototype type.</param>
    /// <param name="val">The prototype.</param>
    /// <returns><c>true</c> if there is a prototype with the specified type; otherwise, <c>false</c>.</returns>
    public bool TryGet(string type, out T val)
    {
        return prototypes.TryGetValue(type, out val);
    }

    /// <summary>
    /// Returns the prototype with the specified type.
    /// </summary>
    /// <returns>The prototype.</returns>
    /// <param name="type">The prototype type.</param>
    public T Get(string type)
    {
        T prot;
        if (prototypes.TryGetValue(type, out prot))
        {
            return prot;
        }

        return default(T);
    }

    /// <summary>
    /// Adds the given prototype. If the protptype exists it is overwirten.
    /// </summary>
    /// <param name="proto">The prototype instance.</param>
    public void Set(T proto)
    {
        prototypes[proto.Type] = proto;
    }

    /// <summary>
    /// Add the given prototype. If a prototype of the given type is already registered, overwrite the old one while logging a warning.
    /// </summary>
    /// <param name="proto">The prototype instance.</param>
    public void Add(T proto)
    {
        T oldProto;
        if (TryGet(proto.Type, out oldProto))
        {
            UnityDebugger.Debugger.LogWarningFormat("PrototypeMap", "Trying to register a prototype of type '{0}' which already exists. Overwriting. Old value: {1}", proto.Type, oldProto);
        }

        Set(proto);
    }

    /// <summary>
    /// Loads all the prototypes from the specified JProperty.
    /// </summary>
    /// <param name="protoToken">JProperty to parse.</param>
    public void LoadJsonPrototypes(JProperty protoToken)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        settings.Formatting = Newtonsoft.Json.Formatting.Indented;
        settings.NullValueHandling = NullValueHandling.Ignore;
        settings.DefaultValueHandling = DefaultValueHandling.Ignore;

        foreach (JToken token in protoToken.Value)
        {
            if (protoToken.Name != "Headline")
            {
                JProperty item = (JProperty)token;
                T prototype = new T();

                prototype.ReadJsonPrototype(item);

                Set(prototype);
            }
            else
            {
                // HACK: headlines currently need special handling, should be made into not a prototype
                JProperty jproperty = new JProperty((string)token, (string)token);
                T prototype = new T();

                prototype.ReadJsonPrototype(jproperty);

                Set(prototype);
            }
        }
    }
}
