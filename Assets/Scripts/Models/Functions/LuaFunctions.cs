#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using ProjectPorcupine.PowerNetwork;

public class LuaFunctions : IFunctions
{
    protected Script script;
    private string scriptName;

    public LuaFunctions()
    {
        // Tell the LUA interpreter system to load all the classes
        // that we have marked as [MoonSharpUserData]
        UserData.RegisterAssembly();

        this.script = new Script();

        // Registering types
        UserData.RegisterType<UnityEngine.Vector3>();
        UserData.RegisterType<UnityEngine.Vector2>();
        UserData.RegisterType<UnityEngine.Vector4>();
        UserData.RegisterType<UnityEngine.UI.Text>();

        // If we want to be able to instantiate a new object of a class
        //   i.e. by doing    SomeClass.__new()
        // We need to make the base type visible.
        RegisterGlobal(typeof(Inventory));
        RegisterGlobal(typeof(Job));
        RegisterGlobal(typeof(ModUtils));
        RegisterGlobal(typeof(World));
        RegisterGlobal(typeof(GameController));
        RegisterGlobal(typeof(Connection));
        RegisterGlobal(typeof(Scheduler.Scheduler));
        RegisterGlobal(typeof(Scheduler.ScheduledEvent));
        RegisterGlobal(typeof(ProjectPorcupine.Jobs.RequestedItem));
        RegisterGlobal(typeof(DeveloperConsole.DevConsole));
        RegisterGlobal(typeof(Settings));
    }

    public bool HasFunction(string name)
    {
        return name != null && script.Globals[name] != null;
    }

    public bool HasConstructor(string className)
    {
        return className != null && script.Globals[className] != null;
    }

    /// <summary>
    /// Loads the script from the specified text.
    /// </summary>
    /// <param name="text">The code text.</param>
    /// <param name="scriptName">The script name.</param>
    public bool LoadScript(string text, string scriptName)
    {
        this.scriptName = scriptName;
        try
        {
            script.DoString(text, script.Globals);
        }
        catch (SyntaxErrorException e)
        {
            UnityDebugger.Debugger.LogError("Lua", "[" + scriptName + "] LUA Parse error: " + e.DecoratedMessage);
            return false;
        }

        return true;
    }

    public DynValue CallWithError(string functionName, params object[] args)
    {
        return Call(functionName, true, args);
    }

    public DynValue Call(string functionName, params object[] args)
    {
        return Call(functionName, false, args);
    }

    public T Call<T>(string functionName, params object[] args)
    {
        return Call(functionName, args).ToObject<T>();
    }

    public DynValue CreateInstance(object fromObject)
    {
        return DynValue.FromObject(script, fromObject);
    }

    public T CreateInstance<T>(string className, params object[] arguments)
    {
        return Call<T>(className, arguments);
    }

    public void RegisterType(Type type)
    {
        RegisterGlobal(type);
    }

    /// <summary>
    /// Call the specified lua function with the specified args.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="args">Arguments.</param>
    private DynValue Call(string functionName, bool throwError, params object[] args)
    {
        try
        {
            return ((Closure)script.Globals[functionName]).Call(args);
        }
        catch (ScriptRuntimeException e)
        {
            UnityDebugger.Debugger.LogError("Lua", "[" + scriptName + "] LUA RunTime error: " + e.DecoratedMessage);
            return null;
        }
        catch (Exception e)
        {
            UnityDebugger.Debugger.LogError("Lua", "[" + scriptName + "] Something else went wrong: " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// Registers a class as a global entity to use it inside of lua.
    /// </summary>
    /// <param name="type">Class typeof.</param>
    private void RegisterGlobal(Type type)
    {
        script.Globals[type.Name] = type;
    }
}
