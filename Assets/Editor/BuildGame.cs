#region License
// =================================================================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
//
// This program comes with ABSOLUTELY NO WARRANTY. This is free software,
// and you are welcome to redistribute it under certain conditions. 
// See file LICENSE, which is part of this source code package, for details.
// =================================================================================================
#endregion

using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides a series of static methods to build the game for multiple platforms,
/// including Windows (32 and 64-bit), macOS, and Linux.
/// 
/// Usage from command line (examples):
///   - macOS:
///       /Applications/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode -logfile -projectPath "path/to/ProjectPorcupine" -executeMethod BuildGame.BuildOSX
///   - Windows:
///       "C:\Program Files\Unity\Editor\Unity.exe" -quit -batchmode -logfile -projectPath "path/to/ProjectPorcupine" -executeMethod BuildGame.BuildWindows
/// </summary>
public static class BuildGame
{
    /// <summary>
    /// The name of the project used for build folder naming.
    /// </summary>
    private const string BuildName = "ProjectPorcupine";

    /// <summary>
    /// Builds the project for all main platforms (Windows, macOS, Linux).
    /// </summary>
    public static void BuildAllTargets()
    {
        BuildOSX();
        BuildWindows();
        BuildLinux();
    }

    /// <summary>
    /// Builds both 32-bit and 64-bit versions on Windows.
    /// </summary>
    public static void BuildWindows()
    {
        BuildWindows32();
        BuildWindows64();
    }

    /// <summary>
    /// Builds the project as a 32-bit Windows Standalone.
    /// </summary>
    public static void BuildWindows32()
    {
        // Example: if you want debugging, pass BuildOptions.Development
        BuildPipeline.BuildPlayer(
            GetEnabledScenes(),
            "Builds/" + BuildName + "_Win32",
            BuildTarget.StandaloneWindows,
            BuildOptions.None
        );
    }

    /// <summary>
    /// Builds the project as a 64-bit Windows Standalone.
    /// </summary>
    public static void BuildWindows64()
    {
        BuildPipeline.BuildPlayer(
            GetEnabledScenes(),
            "Builds/" + BuildName + "_Win64",
            BuildTarget.StandaloneWindows64,
            BuildOptions.None
        );
    }

    /// <summary>
    /// Builds the project for macOS (standalone).
    /// </summary>
    public static void BuildOSX()
    {
        #if UNITY_2017_4_OR_NEWER
            BuildPipeline.BuildPlayer(
                GetEnabledScenes(),
                "Builds/" + BuildName + "_OSX",
                BuildTarget.StandaloneOSX,
                BuildOptions.None
            );
        #else
            BuildPipeline.BuildPlayer(
                GetEnabledScenes(),
                "Builds/" + BuildName + "_OSX",
                BuildTarget.StandaloneOSXUniversal,
                BuildOptions.None
            );
        #endif
    }

    /// <summary>
    /// Builds the project for Linux (universal).
    /// </summary>
    public static void BuildLinux()
    {
        BuildPipeline.BuildPlayer(
            GetEnabledScenes(),
            "Builds/" + BuildName + "_Linux",
            BuildTarget.StandaloneLinuxUniversal,
            BuildOptions.None
        );
    }

    /// <summary>
    /// Retrieves all enabled scenes from the EditorBuildSettings.
    /// Only scenes marked as "enabled" will be included in the build.
    /// </summary>
    /// <returns>An array of scene paths to include in the build.</returns>
    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }
}
