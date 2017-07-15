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
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Gear : Inventory
{
    Dictionary<String, JToken> properties;

    public Gear(): base()
    {
        properties = new Dictionary<string, JToken>();
    }
    
    protected Gear(Gear other):base(other)
    {
        properties = new Dictionary<string, JToken>(other.properties);
    }

    public override void ReadJSONPrototype(JProperty token)
    {
        //begin inventory values
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
        //end inventory values
        JToken propertyToken = token.Value["properties"];
        foreach(JProperty property in propertyToken)
        {
            properties.Add(property.Name, property.Value);
        }
        PrototypeManager.Inventory.Set(this);
    }

    new public Gear Clone()
    {
        return new Gear(this);
    }
}
