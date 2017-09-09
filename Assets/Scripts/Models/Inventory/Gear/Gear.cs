#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class Gear : Inventory
{
    /// <summary>
    /// The dictionary to hold the properties of this item.
    /// 
    /// In the future the string key could be replaced with an enum?.
    /// </summary>
    private Dictionary<string, JToken> properties;

    /// <summary>
    /// Blank initializer.
    /// </summary>
    public Gear() : base()
    {
        properties = new Dictionary<string, JToken>();
    }
    
    /// <summary>
    /// Copy initializer.
    /// </summary>
    /// <param name="other">Item to be copied.</param>
    protected Gear(Gear other) : base(other)
    {
        properties = new Dictionary<string, JToken>(other.properties);
    }

    /// <summary>
    /// Properties getter.
    /// </summary>
    public Dictionary<string, JToken> Properties
    {
        get
        {
            return properties;
        }
    }

    /// <summary>
    /// Load from assets.
    /// 
    /// File can be found at: "StreamingAssets/Data".
    /// </summary>
    /// <param name="token"></param>
    public override void ReadJSONPrototype(JProperty token)
    {
        // begin inventory values
        Type = token.Name;
        Debug.Log(token.ToString());
        JToken inventoryToken = token.Value["inventory"];
        MaxStackSize = (int)inventoryToken["MaxStackSize"];
        StackSize = (int)inventoryToken["StackSize"];
        BasePrice = (float)inventoryToken["BasePrice"];
        Category = (string)inventoryToken["Category"];
        Locked = (bool)inventoryToken["Locked"];
        LocalizationName = (string)inventoryToken["LocalizationName"];
        LocalizationDescription = (string)inventoryToken["LocalizationDesc"];
        //// end inventory values
        JToken propertyToken = token.Value["properties"];
        foreach (JProperty property in propertyToken)
        {
            properties.Add(property.Name, property.Value);
        }

        PrototypeManager.Inventory.Set(this);
    }

    /// <summary>
    /// Builds the JSON object to be saved and then returns it.
    /// </summary>
    /// <returns>The built JSON Object.</returns>
    public new object ToJSon()
    {
        JObject token = (JObject)base.ToJSon();
        JObject propertyList = new JObject();
        JToken value;
        foreach (string key in properties.Keys)
        {
            if (properties.TryGetValue(key, out value))
            {
                propertyList.Add(key, value);
            }
        }

        token.Add("Properties", propertyList);
        return token;
    }

    /// <summary>
    /// Sets all the properties of this object from the given JSON fragment.
    /// 
    /// An example of a saved character object with gear attached in JSON format:
    /// 
    ///  {
    ///      "Name": "Behindcurtain3",
    ///      "X": 48,
    ///      "Y": 49,
    ///      "Z": 0,
    ///      "Needs": {
    ///        "oxygen": 0.0,
    ///        "sleep": 2.73051953
    ///      },
    ///      "Colors": {
    ///        "CharacterColor": [
    ///          0.6830622,
    ///          0.815521,
    ///          0.07269311
    ///        ],
    ///        "UniformColor": [
    ///          0.1997838,
    ///          0.1997838,
    ///          0.1997838
    ///        ],
    ///        "SkinColor": [
    ///          0.827451,
    ///          0.5568628,
    ///          0.435294122
    ///        ]
    ///      },
    ///      "Stats": {
    ///        "Strength": 19,
    ///        "Dexterity": 2,
    ///        "Constitution": 9,
    ///        "Intelligence": 18,
    ///        "Wisdom": 15,
    ///        "Charisma": 18
    ///      },
    ///      "Gear": {
    ///        "Type": "test",
    ///        "MaxStackSize": 1,
    ///        "StackSize": 1,
    ///        "BasePrice": 2.0,
    ///        "Category": null,
    ///        "Locked": false,
    ///        "LocalizationName": "inv_copper_wire",
    ///        "LocalizationDesc": "inv_copper_wire_desc",
    ///        "Properties": {
    ///          "Health": 1000.0
    ///        }
    ///      }
    ///    }
    ///    .
    /// </summary>
    /// <param name="inventoryToken">The given JSON fragment.</param>
    public new void FromJson(JToken inventoryToken)
    {
        base.FromJson(inventoryToken);
        //// end inventory values
        JToken propertyToken = inventoryToken["Properties"];
        foreach (JProperty property in propertyToken)
        {
            properties.Add(property.Name, property.Value);
        }
    }

    /// <summary>
    /// Clones the itself.
    /// </summary>
    /// <returns>Returns the copy.</returns>
    public new Gear Clone()
    {
        return new Gear(this);
    }
}
