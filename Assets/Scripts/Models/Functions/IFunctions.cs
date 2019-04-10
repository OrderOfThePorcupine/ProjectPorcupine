#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using MoonSharp.Interpreter;

public interface IFunctions
{
    void RegisterType(System.Type type);

    DynValue ConvertObject(object obj);

    bool TryCallFunctionWithError(string name, out DynValue res, params object[] args);

    bool TryCallFunction(string name, out DynValue res, params object[] args);

    bool TryCreateInstance(string name, out DynValue res, params object[] args);

    bool TryCallFunctionWithError<T>(string name, out T res, params object[] args);

    bool TryCallFunction<T>(string name, out T res, params object[] args);

    bool TryCreateInstance<T>(string name, out T res, params object[] args);

    bool LoadScript(string text, string scriptName);
}