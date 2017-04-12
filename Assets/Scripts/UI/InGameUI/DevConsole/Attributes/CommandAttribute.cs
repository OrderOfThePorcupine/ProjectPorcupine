using UnityEngine;
using System.Collections;
using System;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string description;
    public string detailedDescription;
    public string title;
    public string[] tags { get; protected set; }

    public CommandAttribute(params string[] tags)
    {
        this.tags = tags;
    }
}
