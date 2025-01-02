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
    /// Reduce GC bloat by caching the dyn values.
    /// </summary>
    /// <param name="functionNames">All the functions to call.</param>
    /// <param name="args">The args.</param>
    /// <returns>How many function successfully called</returns>
    public int TryCall(IEnumerable<string> functionNames, params object[] args)
    {
        if (FunctionsSets.Count == 0) {
            // Then we won't ever find it so just return
            return 0;
        }

        DynValue[] values = new DynValue[args.Length];
        for (int i = 0; i < args.Length; i++) {
            values[i] = FunctionsSets[0].ConvertObject(args[i]);
        }
        int count = 0;
        DynValue tmp;

        foreach (string name in functionNames) {
            if (TryCall(name, out tmp, values)) {
                count++;
            }
        }
        return count;
    }

    public bool TryCall(string functionName, params object[] args)
    {
        DynValue tmp;
        return TryCall(functionName, false, out tmp, args);
    }

    public bool TryCall(string functionName, out DynValue res, params object[] args)
    {
        return TryCall(functionName, false, out res, args);
    }

    public bool TryCall(string functionName, bool throwError, out DynValue res, params object[] args)
    {
        if (throwError ? TryCallIFunctions(functionName, false, out res, args) 
                       : TryCallIFunctionsError(functionName, out res, args)) {
            return true;
        } else {
            UnityDebugger.Debugger.Log(ModFunctionsLogChannel, "'" + functionName + "' is not a LUA nor is it a CSharp function!");

            if (throwError)
            {
                throw new Exception("'" + functionName + "' is not a LUA nor is it a CSharp function!");
            }

            res = null;
            return false;
        }
    }

    public bool TryCall<T>(string functionName, out T res, params object[] args)
    {
        return TryCall(functionName, false, out res, args);
    }

    public bool TryCall(string functionName, bool throwError, out DynValue res, params DynValue[] args)
    {
        if (throwError ? TryCallIFunctions(functionName, false, out res, args) 
                       : TryCallIFunctionsError(functionName, out res, args)) {
            return true;
        } else {
            UnityDebugger.Debugger.Log(ModFunctionsLogChannel, "'" + functionName + "' is not a LUA nor is it a CSharp function!");

            if (throwError)
            {
                throw new Exception("'" + functionName + "' is not a LUA nor is it a CSharp function!");
            }

            res = null;
            return false;
        }
    }

    public bool TryCall<T>(string functionName, bool throwError, out T res, params object[] args)
    {
        if (TryCallIFunctions(functionName, false, out res, args)) {
            return true;
        } else {
            UnityDebugger.Debugger.Log(ModFunctionsLogChannel, "'" + functionName + "' is not a LUA function nor is it a CSharp function!");

            if (throwError)
            {
                throw new Exception("'" + functionName + "' is not a LUA function nor is it a CSharp function!");
            }

            res = default(T);
            return false;
        }
    }

    public bool TryCreateInstance<T>(string className, bool throwError, out T res, params object[] args)
    {
        if (TryCallIFunctions(className, true, out res, args)) {
            return true;
        } else {
            UnityDebugger.Debugger.Log(ModFunctionsLogChannel, "'" + className + "' is not a LUA function nor is it a CSharp constructor!");

            if (throwError)
            {
                throw new Exception("'" + className + "' is not a LUA function nor is it a CSharp constructor!");
            }

            res = default(T);
            return false;
        }
    }

    public void RegisterType(System.Type type)
    {
        foreach (IFunctions functionsSet in FunctionsSets)
        {
            functionsSet.RegisterType(type);
        }
    }

    private bool TryCallIFunctionsError(string name, out DynValue res, object[] args) {
        for (int i = 0; i < FunctionsSets.Count; i++)
        {
            if (FunctionsSets[i].TryCallFunctionWithError(name, out res, args))
            {
                return true;
            }
        }
        res = null;
        return false;
    }

    private bool TryCallIFunctionsError<T>(string name, out T res, object[] args) {
        for (int i = 0; i < FunctionsSets.Count; i++)
        {
            if (FunctionsSets[i].TryCallFunctionWithError(name, out res, args))
            {
                return true;
            }
        }
        res = default(T);
        return false;
    }

    private bool TryCallIFunctions(string name, bool constructor, out DynValue res, object[] args) {
        for (int i = 0; i < FunctionsSets.Count; i++)
        {
            if ((constructor == false && FunctionsSets[i].TryCallFunction(name, out res, args)) ||
                (constructor && FunctionsSets[i].TryCreateInstance(name, out res, args)))
            {
                return true;
            }
        }
        res = null;
        return false;
    }

    private bool TryCallIFunctions<T>(string name, bool constructor, out T res, object[] args) {
        for (int i = 0; i < FunctionsSets.Count; i++)
        {
            if ((constructor == false && FunctionsSets[i].TryCallFunction(name, out res, args)) ||
                (constructor && FunctionsSets[i].TryCreateInstance(name, out res, args)))
            {
                return true;
            }
        }
        res = default(T);
        return false;
    }
}