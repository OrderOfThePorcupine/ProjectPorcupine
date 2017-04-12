using UnityEngine;
using System.Collections;
using System;

[AttributeUsage(AttributeTargets.Method)]
public class ParserAttribute : Attribute
{
    public Type target { get; protected set; }

    public ParserAttribute(Type target)
    {
        this.target = target;
    }
}
