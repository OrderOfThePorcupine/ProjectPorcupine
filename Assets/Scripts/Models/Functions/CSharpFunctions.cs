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
using System.Reflection;
using Mono.CSharp;
using MoonSharp.Interpreter;

public class CSharpFunctions : IFunctions
{
    // this is just to support convertion of object to DynValue
    protected Script script;

    private Dictionary<string, MethodInfo> methods;

    private Dictionary<string, ConstructorInfo> constructors;

    private Evaluator evaluator;

    public CSharpFunctions()
    {
        script = new Script();
        methods = new Dictionary<string, MethodInfo>();
        constructors = new Dictionary<string, ConstructorInfo>();
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

    public bool HasFunction(string name)
    {
        return methods.ContainsKey(name);
    }

    public bool HasConstructor(string className)
    {
        return constructors.ContainsKey(className);
    }

    /// <summary>
    /// Loads the script from the specified text.
    /// </summary>
    /// <param name="text">The code text.</param>
    /// <param name="scriptName">The script name.</param>
    public bool LoadScript(string text, string scriptName)
    {
        bool success = false;

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

                return false;
            }

            CreateDelegates(resAssembly);
            success = true;
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

        return success;
    }

    /// <summary>
    /// Call the specified lua function with the specified args.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="args">Arguments.</param>
    public DynValue Call(string functionName, params object[] args)
    {
        object ret = methods[functionName].Invoke(null, args);
        return DynValue.FromObject(script, ret);
    }

    /// <summary>
    /// Call the specified lua function with the specified args.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="args">Arguments.</param>
    public T Call<T>(string functionName, params object[] args)
    {
        return (T)methods[functionName].Invoke(null, args);
    }

    public void RegisterType(Type type)
    {
        // nothing to do for C#
    }

    // This really doesn't need to exist, CallWithError is only for LUA
    public DynValue CallWithError(string functionName, params object[] args)
    {
        return Call(functionName, args);
    }

    public DynValue CreateInstance(object fromObject)
    {
        return DynValue.FromObject(script, fromObject);
    }

    public T CreateInstance<T>(string className, params object[] arguments)
    {
        return (T)constructors[className].Invoke(arguments);
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
                methods.Add(method.Name, method);
            }

            foreach (ConstructorInfo constructor in GetAllConstructorsFromType(type))
            {
                constructors.Add(constructor.ReflectedType.Name + string.Join(",", constructor.GetParameters().Select(x => x.ParameterType.Name).ToArray()), constructor);
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

        public string GetErrorsLog()
        {
            return string.Join(Environment.NewLine, Errors.ToArray());
        }
    }
}