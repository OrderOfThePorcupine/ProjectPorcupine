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
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.CSharp;
using MoonSharp.Interpreter;

public class CSharpFunctions : IFunctions
{
    // this is just to support convertion of object to DynValue
    protected Script script;

    private Dictionary<string, List<MethodInfo>> methods;

    private Dictionary<string, List<ConstructorInfo>> constructors;

    private Evaluator evaluator;

    public CSharpFunctions()
    {
        script = new Script();
        methods = new Dictionary<string, List<MethodInfo>>();
        constructors = new Dictionary<string, List<ConstructorInfo>>();
        CompilationResult = new CompilingResult();
        evaluator = null;
    }

    /// <summary>
    /// Gets the compiling result.
    /// </summary>
    /// <value>The compiling result.</value>
    private CompilingResult CompilationResult { get; set; }

    /// <summary>
    /// Little helper method to detect dynamic assemblies.
    /// </summary>
    /// <param name="assembly">Assembly to check.</param>
    /// <returns>True if assembly is dynamic, otherwise false.</returns>
    public static bool IsDynamic(Assembly assembly)
    {
        // http://bloggingabout.net/blogs/vagif/archive/2010/07/02/net-4-0-and-notsupportedexception-complaining-about-dynamic-assemblies.aspx
        // Will cover both System.Reflection.Emit.AssemblyBuilder and System.Reflection.Emit.InternalAssemblyBuilder
        return assembly.GetType().FullName.EndsWith("AssemblyBuilder") || assembly.Location == null || assembly.Location == string.Empty;
    }

    private int MatchArgs(Type[] a, IEnumerable<Type> b) {
        int count = 0;
        // Arguably this isn't foolproof but it should be good enough
        // so that we don't have really slow functions.
        foreach (Type t in b) {
            if (a.Length > count && t.Name == a[count].Name) {
                count++;
            } else {
                break;
            }
        }
        return count;
    }

    public DynValue ConvertObject(object obj) {
        return DynValue.FromObject(script, obj);
    }

    public bool TryCallFunctionWithError(string name, out DynValue res, params object[] args) {
        object tmp;
        if (!TryCallFunctionWithError(name, out tmp, args)) {
            res = null;
            return false;
        } else {
            res = DynValue.FromObject(script, tmp);
            return true;
        }
    }

    public bool TryCallFunction(string name, out DynValue res, params object[] args) {
        object tmp;
        if (!TryCallFunction(name, out tmp, args)) {
            res = null;
            return false;
        } else {
            res = DynValue.FromObject(script, tmp);
            return true;
        }
    }

    public bool TryCreateInstance(string name, out DynValue res, params object[] args) {
        object tmp;
        if (!TryCreateInstance(name, out tmp, args)) {
            res = null;
            return false;
        } else {
            res = DynValue.FromObject(script, tmp);
            return true;
        }
    }

    public bool TryCallFunctionWithError<T>(string name, out T res, params object[] args) {
        // Only LUA can really utilise this such that we can silence errors
        // Really only DeveloperConsole uses this right now (rightfully so however)
        // So a tad of a hack; @HACK
        return TryCallFunction(name, out res, args);
    }

    public bool TryCallFunction<T>(string name, out T res, params object[] args) {
        List<MethodInfo> info;
        if (methods.TryGetValue(name, out info) && info.Count > 0) {
            // We want to find the best fit based on our args
            Type[] types = args.Select(x => x.GetType()).ToArray();
            MethodInfo bestMatch = info.First();
            int bestMatchCount = MatchArgs(types, bestMatch.GetParameters().Select(x => x.GetType()));
            for (int i = 1; i < info.Count; i++) {
                int matchCount = MatchArgs(types, info[i].GetParameters().Select(x => x.GetType()));
                if (matchCount > bestMatchCount) {
                    bestMatchCount = matchCount;
                    bestMatch = info[i];
                } else if (matchCount == bestMatchCount && matchCount != 0) {
                    // oh no! This is an internal implementation error since
                    // we can't choose which one is better just error
                    // and pick the first hoping it'll be fine
                    UnityDebugger.Debugger.LogError("CSharp",
                        "Ambiguous Matching, choosing first ignoring rest for: " + name);
                }
            }

            res = (T)bestMatch.Invoke(null, args);
            return true;
        }
        res = default(T);
        return false;
    }

    public bool TryCreateInstance<T>(string name, out T res, params object[] args) {
        List<ConstructorInfo> info;
        if (constructors.TryGetValue(name, out info) && info.Count > 0) {
            // We want to find the best fit based on our args
            Type[] types = args.Select(x => x.GetType()).ToArray();
            ConstructorInfo bestMatch = info.First();
            int bestMatchCount = MatchArgs(types, bestMatch.GetParameters().Select(x => x.GetType()));
            for (int i = 1; i < info.Count; i++) {
                int matchCount = MatchArgs(types, info[i].GetParameters().Select(x => x.GetType()));
                if (matchCount > bestMatchCount) {
                    bestMatchCount = matchCount;
                    bestMatch = info[i];
                } else if (matchCount == bestMatchCount && matchCount != 0) {
                    // oh no! This is an internal implementation error since
                    // we can't choose which one is better just error
                    // and pick the first hoping it'll be fine
                    UnityDebugger.Debugger.LogError("CSharp",
                        "Ambiguous Matching, choosing first ignoring rest for: " + name);
                }
            }

            res = (T)bestMatch.Invoke(null, args);
            return true;
        }
        res = default(T);
        return false;
    }

    /// <summary>
    /// Loads the script from the specified text.
    /// </summary>
    /// <param name="text">The code text.</param>
    /// <param name="scriptName">The script name.</param>
    public bool LoadScript(string text, string scriptName)
    {
        try
        {
            CompilerSettings settings = new CompilerSettings();

            // only optimise on release builds so that we have faster loadtimes
            // when debugging
            settings.Optimize = !UnityEngine.Debug.isDebugBuild;

            evaluator = new Evaluator(new CompilerContext(settings, CompilationResult));
            evaluator.ReferenceAssembly(Assembly.GetExecutingAssembly());

            // Add all unity assemblies
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                // only load unity assemblies and the unity debugger
                if (assemblies[i].FullName.Contains("UnityEngine") || assemblies[i].FullName.Contains("UnityDebugger"))
                {
                    evaluator.ReferenceAssembly(assemblies[i]);
                }
            }

            // first, try if it already exists
            Assembly resAssembly = GetCompiledAssembly(scriptName);

            if (resAssembly == null)
            {
                evaluator.Compile(text + GetConnectionPointClassDeclaration(scriptName));
                resAssembly = GetCompiledAssembly(scriptName);
            }

            if (resAssembly == null)
            {
                if (CompilationResult.HasErrors)
                {
                    UnityDebugger.Debugger.LogError(
                        "CSharp",
                        string.Format("[{0}] CSharp compile errors ({1}): {2}", scriptName, CompilationResult.Errors.Count, CompilationResult.GetErrorsLog()));
                }
                else if (CompilationResult.HasWarnings)
                {
                    UnityDebugger.Debugger.LogWarning(
                        "CSharp",
                        string.Format("[{0}] CSharp warning ({1}): {2}", scriptName, CompilationResult.Warnings.Count, CompilationResult.GetWarningsLog()));
                }

                return false;
            }

            CreateDelegates(resAssembly);
        }
        catch (Exception ex)
        {
            if (CompilationResult.HasErrors)
            {
                UnityDebugger.Debugger.LogError(
                    "CSharp",
                    string.Format("[{0}] CSharp compile errors ({1}):\n{2}\nException Details: {3}", scriptName, CompilationResult.Errors.Count, CompilationResult.GetErrorsLog(), ex));
            }
            else
            {
                UnityDebugger.Debugger.LogError(
                        "CSharp",
                        string.Format("[{0}] Problem loading functions from CSharp script: {1}", scriptName, ex.Message));
            }
        }

        return true;
    }

    /// <summary>
    /// Loads the script from the specified file.
    /// </summary>
    /// <param name="file">The file to open.</param>
    /// <param name="scriptName">The script name.</param>
    public bool LoadFile(string file, string scriptName)
    {
        return LoadScript(File.ReadAllText(file), scriptName);
    }

    public void RegisterType(Type type)
    {
        // nothing to do for C#
    }

    private string GetConnectionPointClassDeclaration(string name)
    {
        return Environment.NewLine + " public struct MonoSharp_DynamicAssembly_" + name + " {}";
    }

    private string GetConnectionPointGetTypeExpression(string name)
    {
        return "typeof(MonoSharp_DynamicAssembly_" + name + ");";
    }

    private void CreateDelegates(Assembly assembly)
    {
        foreach (Type type in GetAllTypesFromAssembly(assembly))
        {
            foreach (MethodInfo method in GetAllMethodsFromType(type))
            {
                List<MethodInfo> info;
                if (!methods.TryGetValue(method.Name, out info)) {
                    info = new List<MethodInfo>();
                    methods.Add(method.Name, info);
                }
                info.Add(method);
            }

            foreach (ConstructorInfo constructor in GetAllConstructorsFromType(type))
            {
                List<ConstructorInfo> info;
                if (!constructors.TryGetValue(constructor.Name, out info)) {
                    info = new List<ConstructorInfo>();
                    constructors.Add(constructor.Name, info);
                }
                info.Add(constructor);
            }
        }
    }

    private MethodInfo[] GetAllMethodsFromType(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Static);
    }

    private ConstructorInfo[] GetAllConstructorsFromType(Type type)
    {
        return type.GetConstructors();
    }

    private Type[] GetAllTypesFromAssembly(Assembly assembly)
    {
        return assembly.GetTypes();
    }

    private Assembly GetCompiledAssembly(string name)
    {
        try
        {
            string className = GetConnectionPointGetTypeExpression(name);
            return ((Type)evaluator.Evaluate(className)).Assembly;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private Assembly GetCompiledAssemblyForScript(string className)
    {
        try
        {
            return ((Type)evaluator.Evaluate(className)).Assembly;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private class CompilingResult : ReportPrinter
    {
        /// <summary>
        /// The collection of compiling errors.
        /// </summary>
        public List<string> Errors = new List<string>();

        /// <summary>
        /// The collection of compiling warnings.
        /// </summary>
        public List<string> Warnings = new List<string>();

        /// <summary>
        /// Indicates if the last compilation yielded any errors.
        /// </summary>
        /// <value>If set to <c>true</c> indicates presence of compilation error(s).</value>
        public bool HasErrors
        {
            get
            {
                return Errors.Count > 0;
            }
        }

        /// <summary>
        /// Indicates if the last compilation yielded any warnings.
        /// </summary>
        /// <value>If set to <c>true</c> indicates presence of compilation warning(s).</value>
        public bool HasWarnings
        {
            get
            {
                return Warnings.Count > 0;
            }
        }

        /// <summary>
        /// Clears all errors and warnings.
        /// </summary>
        public new void Reset()
        {
            Errors.Clear();
            Warnings.Clear();
            base.Reset();
        }

        /// <summary>
        /// Handles compilation event message.
        /// </summary>
        /// <param name="msg">The compilation event message.</param>
        /// <param name="showFullPath">If set to <c>true</c> [show full path].</param>
        public override void Print(AbstractMessage msg, bool showFullPath)
        {
            string msgInfo = string.Format("{0} {1} CS{2:0000}: {3}", msg.Location, msg.MessageType, msg.Code, msg.Text);
            if (!msg.IsWarning)
            {
                Errors.Add(msgInfo);
            }
            else
            {
                Warnings.Add(msgInfo);
            }
        }

        public string GetWarningsLog()
        {
            return string.Join(Environment.NewLine, Warnings.ToArray());
        }

        public string GetErrorsLog()
        {
            return string.Join(Environment.NewLine, Errors.ToArray());
        }
    }
}