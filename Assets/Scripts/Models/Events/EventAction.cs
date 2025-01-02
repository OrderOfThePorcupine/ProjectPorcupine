﻿#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System.Collections.Generic;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;

/// <summary>
/// This class handles LUA actions take in response to events triggered within C# or LUA. For each event name (e.g. OnUpdate, ...) there
/// is a list of LUA function that are registered and will be called once the event with that name is fired.
/// </summary>
[MoonSharpUserData]
public class EventActions
{
    /// <summary>
    /// Stores a list of LUA functions for each type of event (eventName). All will be called at once.
    /// </summary>
    protected Dictionary<string, List<string>> actionsList = new Dictionary<string, List<string>>();

    /// <summary>
    /// Used to transfer register actions to new object.
    /// </summary>
    /// <returns>A new object copy of this.</returns>
    public EventActions Clone()
    {
        EventActions evt = new EventActions();

        evt.actionsList = new Dictionary<string, List<string>>(actionsList);

        return evt;
    }

    /// <summary>
    /// Fill the values of this from Json.
    /// </summary>
    /// <param name="eventActionsToken">JToken pointing to an Action tag.</param>
    public void ReadJson(JToken eventActionsToken)
    {
        if (eventActionsToken == null)
        {
            return;
        }

        foreach (JProperty eventAction in eventActionsToken)
        {
            string name = eventAction.Name;

            // TODO: this could possibly be converted over to use tools from PrototpeReader
            foreach (JToken function in (JArray)eventAction.Value)
            {
                string functionName = (string)function;
                Register(name, functionName);
            }
        }
    }

    /// <summary>
    /// Register a function named luaFunc, that gets fired in response to an action named actionName.
    /// </summary>
    /// <param name="actionName">Name of event triggering action.</param>
    /// <param name="luaFunc">Lua function to add to list of actions.</param>
    public void Register(string actionName, string luaFunc)
    {
        List<string> actions;
        if (actionsList.TryGetValue(actionName, out actions) == false || actions == null)
        {
            actions = new List<string>();
            actionsList[actionName] = actions;
        }

        actions.Add(luaFunc);
    }

    /// <summary>
    /// Deregister a function named luaFunc, from the action.
    /// </summary>
    /// <param name="actionName">Name of event triggering action.</param>
    /// <param name="luaFunc">Lua function to add to list of actions.</param>
    public void Deregister(string actionName, string luaFunc)
    {
        List<string> actions;
        if (actionsList.TryGetValue(actionName, out actions) == false || actions == null)
        {
            return;
        }

        actions.Remove(luaFunc);
    }

    /// <summary>
    /// Fire the event named actionName, resulting in all lua functions being called.
    /// This one reduces GC bloat.
    /// </summary>
    /// <param name="actionName">Name of the action being triggered.</param>
    /// <param name="target">Object, passed to LUA function as 1-argument.</param>
    /// <param name="parameters">Parameters in question.  First one must be target instance. </param>
    public void Trigger(string actionName, params object[] parameters)
    {
        List<string> actions;
        if (actionsList.TryGetValue(actionName, out actions) && actions != null)
        {
            FunctionsManager.Get(parameters[0].GetType().Name).TryCall(actions, parameters);
        }
    }

    /// <summary>
    /// Determines whether this instance has any events named actionName.
    /// </summary>
    /// <returns><c>true</c> if this instance has any events named actionName; otherwise, <c>false</c>.</returns>
    /// <param name="actionName">Action name.</param>
    public bool HasEvent(string actionName)
    {
        // FIXME: 'Has' methods are generally a bad idea, should be 'TryGet' instead
        return actionsList.ContainsKey(actionName);
    }

    /// <summary>
    /// Determines whether this instance has any events.
    /// </summary>    
    public bool HasEvents()
    {
        return actionsList.Count > 0;
    }
}
