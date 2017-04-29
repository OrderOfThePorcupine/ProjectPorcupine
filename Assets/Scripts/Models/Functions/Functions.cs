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
using System.Linq;
using MoonSharp.Interpreter;

public class Functions
{
    private const string ModFunctionsLogChannel = "ModScript";

    public Functions()
    {
        FunctionsSets = new List<IFunctions>();
    }

    public enum Type
    {
        Lua,
        CSharp
    }

    public List<IFunctions> FunctionsSets { get; private set; }

    public bool HasFunction(string name)
    {
        return GetFunctions(name) != null;
    }

    public bool LoadScript(string text, string scriptName, Type type)
    {
        bool result = false;
        if (type == Type.Lua)
        {
            LuaFunctions luaFunctions = new LuaFunctions();

            if (luaFunctions.LoadScript(text, scriptName))
            {
                FunctionsSets.Add(luaFunctions);
            }
        }
        else
        {
            CSharpFunctions netFunctions = new CSharpFunctions();
            if (netFunctions.LoadScript(text, scriptName))
            {
                FunctionsSets.Add(netFunctions);
            }
        }

        return result;
    }

    /// <summary>
    /// The Common Call Function.
    /// </summary>
    public DynValue Call(string functionName, params object[] args)
    {
        return Call(functionName, false, args);
    }

    /// <summary>
    /// Throws an error if warranted.
    /// </summary>
    public DynValue CallWithError(string functionName, params object[] args)
    {
        return Call(functionName, true, args);
    }

    public T Call<T>(string functionName, params object[] args)
    {
        IFunctions functions = GetFunctions(functionName);
        if (functions != null)
        {
            return functions.Call<T>(functionName, args);
        }
        else
        {
            UnityDebugger.Debugger.Log(ModFunctionsLogChannel, "'" + functionName + "' is not a LUA nor CSharp function!");
            return default(T);
        }
    }

    /// <summary>
    /// The Common Call Function expanded for multiple functions.
    /// </summary>
    public void Call(List<string> functionNames, params object[] args)
    {
        bool ranLUAArgs = false;
        DynValue[] luaArgs = null;

        for (int i = 0; i < functionNames.Count; i++)
        {
            if (functionNames[i] == null)
            {
                UnityDebugger.Debugger.LogError(ModFunctionsLogChannel, "'" + functionNames[i] + "'  is not a LUA nor CSharp function!");
                continue;
            }

            IFunctions functions = GetFunctions(functionNames[i]);

            if (functions is LuaFunctions)
            {
                if (ranLUAArgs == false)
                {
                    luaArgs = new DynValue[args.Length];
                    for (int j = 0; j < args.Length; j++)
                    {
                        luaArgs[j] = functions.CreateInstance(args[j]);
                    }
                }

                Call(functionNames[i], false, luaArgs);
            }
            else
            {
                Call(functionNames[i], false, args);
            }
        }

    }

    public T CreateInstance<T>(string className, bool throwError, params object[] args)
    {
        string fullClassName = className + (args.Length > 0 ? string.Join(",", args.Select(x => x.GetType().Name).ToArray()) : string.Empty);
        IFunctions functions = GetFunctions(fullClassName, true);
        if (functions != null)
        {
            return functions.CreateInstance<T>(fullClassName, args);
        }
        else
        {
            UnityDebugger.Debugger.Log(ModFunctionsLogChannel, "'" + className + "' is not a LUA function nor is it a CSharp constructor!");

            if (throwError)
            {
                throw new Exception("'" + className + "' is not a LUA function nor is it a CSharp constructor!");
            }

            return default(T);
        }
    }

    public void RegisterType(System.Type type)
    {
        foreach (IFunctions functionsSet in FunctionsSets)
        {
            functionsSet.RegisterType(type);
        }
    }

    private DynValue Call(string functionName, bool throwError, params object[] args)
    {
        IFunctions functions = GetFunctions(functionName);
        if (functions != null)
        {
            return functions.Call(functionName, args);
        }
        else
        {
            UnityDebugger.Debugger.Log(ModFunctionsLogChannel, "'" + functionName + "' is not a LUA nor is it a CSharp function!");

            if (throwError)
            {
                throw new Exception("'" + functionName + "' is not a LUA nor is it a CSharp function!");
            }

            return null;
        }
    }

    private DynValue Call(string functionName, bool throwError, params DynValue[] args)
    {
        IFunctions functions = GetFunctions(functionName);
        if (functions != null)
        {
            return functions.Call(functionName, args);
        }
        else
        {
            UnityDebugger.Debugger.Log(ModFunctionsLogChannel, "'" + functionName + "' is not a LUA nor is it a CSharp function!");

            if (throwError)
            {
                throw new Exception("'" + functionName + "' is not a LUA nor is it a CSharp function!");
            }

            return null;
        }
    }

    private IFunctions GetFunctions(string name, bool constructor = false)
    {
        for (int i = 0; i < FunctionsSets.Count; i++)
        {
            if ((constructor == false && FunctionsSets[i].HasFunction(name)) || (constructor && FunctionsSets[i].HasConstructor(name)))
            {
                return FunctionsSets[i];
            }
        }

        return null;
    }
}