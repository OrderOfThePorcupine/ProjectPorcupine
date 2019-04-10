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
        RegisterGlobal(typeof(WorldController));
        RegisterGlobal(typeof(Connection));
        RegisterGlobal(typeof(Scheduler.Scheduler));
        RegisterGlobal(typeof(Scheduler.ScheduledEvent));
        RegisterGlobal(typeof(ProjectPorcupine.Jobs.RequestedItem));
        RegisterGlobal(typeof(DeveloperConsole.DevConsole));
        RegisterGlobal(typeof(Settings));
    }

    public DynValue ConvertObject(object obj) {
        return DynValue.FromObject(script, obj);
    }

    public bool TryCallFunctionWithError(string name, out DynValue res, params object[] args) {
        if (name == null || script.Globals[name] == null) {
            res = null;
            return false;
        }

        res = Call(name, true, args);
        return res != null;
    }

    public bool TryCallFunction(string name, out DynValue res, params object[] args) {
        if (name == null || script.Globals[name] == null) {
            res = null;
            return false;
        }

        res = Call(name, false, args);
        return res != null;
    }

    public bool TryCreateInstance(string name, out DynValue res, params object[] args) {
        return TryCallFunction(name, out res, args);
    }

    public bool TryCallFunctionWithError<T>(string name, out T res, params object[] args) {
        DynValue tmp;
        if (!TryCallFunctionWithError(name, out tmp, args)) {
            res = default(T);
            return false;
        } else {
            res = tmp.ToObject<T>();
            return true;
        }
    }

    public bool TryCallFunction<T>(string name, out T res, params object[] args) {
        DynValue tmp;
        if (!TryCallFunction(name, out tmp, args)) {
            res = default(T);
            return false;
        } else {
            res = tmp.ToObject<T>();
            return true;
        }
    }

    public bool TryCreateInstance<T>(string name, out T res, params object[] args) {
        DynValue tmp;
        if (!TryCreateInstance(name, out tmp, args)) {
            res = default(T);
            return false;
        } else {
            res = tmp.ToObject<T>();
            return true;
        }
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

    /// <summary>
    /// Loads the script from the given file.
    /// </summary>
    /// <param name="file">The file to run.</param>
    /// <param name="scriptName">The script name.</param>
    public bool LoadFile(string file, string scriptName)
    {
        // can't use LoadFile without a custom ILoader (since it defaults to Resources/Scripts)
        // so basically it would just be more work than doing this
        return LoadScript(System.IO.File.ReadAllText(file), scriptName);
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
            UnityDebugger.Debugger.LogError("Lua", "[" + scriptName + "," + functionName + "] LUA RunTime error: " + e.DecoratedMessage);
            return null;
        }
        catch (Exception e)
        {
            UnityDebugger.Debugger.LogError("Lua", "[" + scriptName + "," + functionName + "] Something else went wrong: " + e.Message);
            UnityDebugger.Debugger.LogError("Lua", e);
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
