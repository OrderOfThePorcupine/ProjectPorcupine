﻿#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using Newtonsoft.Json.Linq;

public class JobCategory : IPrototypable
{
    private string type;

    /// <summary>
    /// Initializes a new instance of the <see cref="Currency"/> class.
    /// Empty constructor to implement IPrototypable, should not be used.
    /// </summary>
    public JobCategory()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Currency"/> class.
    /// Copy constructor. Use Clone instead.
    /// </summary>
    /// <param name="other">The currency to copy.</param>
    public JobCategory(JobCategory other)
    {
        LocalizationName = other.LocalizationName;
    }

    /// <summary>
    /// Gets the currency short name.
    /// </summary>
    /// <value>The currency short name.</value>
    public string LocalizationName { get; private set; }

    public string Type
    {
        get
        {
            return type;
        }
    }

    /// <summary>
    /// Clone this instance.
    /// </summary>
    public JobCategory Clone()
    {
        return new JobCategory(this);
    }

    /// <summary>
    /// Reads the prototype from the specified JProperty.
    /// </summary>
    /// <param name="jsonProto">The JProperty containing the prototype.</param>
    public void ReadJsonPrototype(JProperty jsonProto)
    {
        JToken innerJson = jsonProto.Value;
        type = jsonProto.Name;
        LocalizationName = (string)innerJson["LocalizationName"];
    }
}
