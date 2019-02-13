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
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;

[MoonSharpUserData]
public class Parameter
{
    // Name is primarily to simplify writing to XML, and will be the same as the key used to access it in a higher up Parameter when read from XML
    private string name;

    // Value is stored as a string and converted as needed, this simplifies storing multiple value types.
    private string value;

    // If this Parameter contains other Parameters, contents will contain the actual parameters
    private Dictionary<string, Parameter> contents;

    public Parameter(string name, string value)
    {
        this.name = name;
        this.value = value;
        contents = new Dictionary<string, Parameter>();
    }

    // Constructor with object parameter allows it to easily create a Parameter with any object that has a string representation (primarily for use if that string
    //  representation can be converted back to the original value, such as with a float. Not suitable for holding a Parameter object.
    public Parameter(string name, object value)
    {
        this.name = name;
        this.value = value.ToString();
        contents = new Dictionary<string, Parameter>();
    }

    // Parameter with no value assumes it is being used for Parameter with contents, and initialized the dictionary
    public Parameter(string name)
    {
        this.name = name;
        contents = new Dictionary<string, Parameter>();
    }

    // Constructor for top-level Parameter (e.g. furnParameters in Furniture.css
    public Parameter()
    {
        contents = new Dictionary<string, Parameter>();
    }

    // Copy constructur, should properly handle copying both types of Parameters (singular and group)
    public Parameter(Parameter other)
    {
        this.name = other.GetName();
        if (other.HasContents())
        {
            this.contents = other.DeepCloneDictionary();
        }
        else
        {
            contents = new Dictionary<string, Parameter>();
        }

        this.value = other.ToString();
    }

    public string Value
    {
        get { return value; }
    }

    // Iterator to simplify usage, this works properly in Lua
    public Parameter this[string key]
    {
        get
        {
            if (contents.ContainsKey(key) == false)
            {
                // Add a new blank key to contents, that will then be returned.
                contents.Add(key, new Parameter(key));
            }

            return contents[key];
        }

        set
        {
            contents[key] = value;
        }
    }

    public override string ToString()
    {
        return value;
    }

    public string ToString(string defaultValue)
    {
        if (value == null)
        {
            return defaultValue;
        }

        return ToString();
    }

    public float ToFloat()
    {
        float returnValue = 0;
        float.TryParse(value, out returnValue);
        return returnValue;
    }

    public int ToInt()
    {
        int returnValue = 0;
        int.TryParse(value, out returnValue);

        return returnValue;
    }

    public bool ToBool()
    {
        if (string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase) || ToFloat().AreEqual(1))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public float ToFloat(float defaultValue)
    {
        if (value == null)
        {
            return defaultValue;
        }

        return ToFloat();
    }

    public void SetValue(string value)
    {
        this.value = value;
    }

    public void SetValue(object value)
    {
        this.value = value.ToString();
    }

    // Change value by a float, primarily here to approximate old parameter system usage
    public void ChangeFloatValue(float value)
    {
        this.value = string.Empty + (ToFloat() + value);
    }

    public string GetName()
    {
        return name;
    }

    // Converts contents Keys from KeyCollection to plain array
    public string[] Keys()
    {
        string[] keys = new string[contents.Keys.Count];
        contents.Keys.CopyTo(keys, 0);
        return keys;
    }

    public bool ContainsKey(string key)
    {
        return contents.ContainsKey(key);
    }

    public void AddParameter(Parameter parameter)
    {
        contents[parameter.GetName()] = parameter;
    }

    // Primary method to differentiate an unknown Parameter between a singular Parameter and Group Parameter
    public bool HasContents()
    {
        return contents.Count > 0;
    }

    public JToken ToJson()
    {
        if (HasContents())
        {
            JObject contentsJson = new JObject();
            foreach (string key in contents.Keys)
            {
                contentsJson.Add(key, contents[key].ToJson());
                return contentsJson;
            }
        }

        return value;
    }

    public void FromJson(JToken parameterToken)
    {
        JObject parameterJObject = (JObject)parameterToken;
        if (parameterJObject == null)
        {
            return;
        }

        foreach (JProperty parameterProperty in parameterJObject.Properties())
        {
            string key = parameterProperty.Name;
            Parameter parameter = new Parameter(key);
            JToken valueToken = parameterProperty.Value;

            if (valueToken.Children().Count() > 1)
            {
                parameter.FromJson(valueToken);
            }
            else
            {
                parameter.SetValue((string)valueToken);
            }

            AddParameter(parameter);
        }
    }

    // Provides a deep clone of the dictionary, to ensure contained Parameters aren't linked between old and new objects
    private Dictionary<string, Parameter> DeepCloneDictionary()
    {
        return contents.ToDictionary(
            entry => entry.Key,
            entry => new Parameter((Parameter)entry.Value));
    }
}
