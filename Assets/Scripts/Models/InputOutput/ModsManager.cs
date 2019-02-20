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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectPorcupine.Entities;
using UnityEngine;

public class ModsManager
{
    private DirectoryInfo[] mods;
    private Dictionary<string, List<Action<JProperty>>> prototypeHandlers = new Dictionary<string, List<Action<JProperty>>>();

    public ModsManager()
    {
        if (SceneController.IsAtIntroScene())
        {
            SetUp(Type.Intro);
        }
        else if (SceneController.IsAtMainScene())
        {
            SetUp(Type.MainScene);
        }
    }

    public ModsManager(Type type)
    {
        SetUp(type);
    }

    public enum Type
    {
        Intro, MainScene
    }

    /// <summary>
    /// Return directory info of the mod folder.
    /// </summary>
    public static DirectoryInfo[] GetModsFiles()
    {
        DirectoryInfo modsDir = new DirectoryInfo(GetPathToModsFolder());
        return modsDir.GetDirectories();
    }

    /// <summary>
    /// Loads the script file in the given location.
    /// </summary>
    /// <param name="file">The file name.</param>
    /// <param name="functionsName">The functions name.</param>
    public void LoadFunctionsInFile(FileInfo file, string functionsName)
    {
        LoadTextFile(
            file.DirectoryName,
            file.Name,
            (filePath) =>
            {
                StreamReader reader = new StreamReader(file.OpenRead());
                string text = reader.ReadToEnd();
                FunctionsManager.Get(functionsName).LoadScript(text, functionsName, file.Extension == ".lua" ? Functions.Type.Lua : Functions.Type.CSharp);
            });
    }

    /// <summary>
    /// Return the path to the mod folder.
    /// </summary>
    private static string GetPathToModsFolder()
    {
        return Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Mods");
    }

    /// <summary>
    /// Common initialization to make testing easier.
    /// </summary>
    private void SetUp(Type type)
    {
        mods = GetModsFiles();

        LoadSharedFiles();

        if (type == Type.Intro)
        {
            LoadIntroFiles();
        }
        else if (type == Type.MainScene)
        {
            LoadMainSceneFiles();
        }

        LoadPrototypes();
    }

    private void LoadMainSceneFiles()
    {
        LoadFunctions("Furniture.lua", "Furniture");
        LoadFunctions("Utility.lua", "Utility");
        LoadFunctions("RoomBehavior.lua", "RoomBehavior");
        LoadFunctions("Need.lua", "Need");
        LoadFunctions("GameEvent.lua", "GameEvent");
        LoadFunctions("Tiles.lua", "TileType");
        LoadFunctions("Quest.lua", "Quest");
        LoadFunctions("ScheduledEvent.lua", "ScheduledEvent");
        LoadFunctions("Overlay.lua", "Overlay");

        LoadFunctions("FurnitureFunctions.cs", "Furniture");
        LoadFunctions("OverlayFunctions.cs", "Overlay");

        HandlePrototypes("Tile", PrototypeManager.TileType.LoadJsonPrototypes);
        HandlePrototypes("Furniture", PrototypeManager.Furniture.LoadJsonPrototypes);
        HandlePrototypes("Utility", PrototypeManager.Utility.LoadJsonPrototypes);
        HandlePrototypes("RoomBehavior", PrototypeManager.RoomBehavior.LoadJsonPrototypes);
        HandlePrototypes("Inventory", PrototypeManager.Inventory.LoadJsonPrototypes);
        HandlePrototypes("Need", PrototypeManager.Need.LoadJsonPrototypes);
        HandlePrototypes("Trader", PrototypeManager.Trader.LoadJsonPrototypes);
        HandlePrototypes("Currency", PrototypeManager.Currency.LoadJsonPrototypes);
        HandlePrototypes("GameEvent", PrototypeManager.GameEvent.LoadJsonPrototypes);
        HandlePrototypes("ScheduledEvent", PrototypeManager.ScheduledEvent.LoadJsonPrototypes);
        HandlePrototypes("Stat", PrototypeManager.Stat.LoadJsonPrototypes);
        HandlePrototypes("Quest", PrototypeManager.Quest.LoadJsonPrototypes);
        HandlePrototypes("Headline", PrototypeManager.Headline.LoadJsonPrototypes);
        HandlePrototypes("Overlay", PrototypeManager.Overlay.LoadJsonPrototypes);
        HandlePrototypes("Ship", PrototypeManager.Ship.LoadJsonPrototypes);

        LoadCharacterNames("CharacterNames.txt");

        LoadDirectoryAssets("Images", SpriteManager.LoadSpriteFiles);
        LoadDirectoryAssets("Audio", AudioManager.LoadAudioFiles);
    }

    private void LoadIntroFiles()
    {
        LoadDirectoryAssets("Audio", AudioManager.LoadAudioFiles);
        LoadDirectoryAssets("MainMenu/Images", SpriteManager.LoadSpriteFiles);
        LoadDirectoryAssets("MainMenu/Audio", AudioManager.LoadAudioFiles);
    }

    private void LoadSharedFiles()
    {
        // Not currently used
        // LoadDirectoryAssets("Shared/Images", SpriteManager.LoadSpriteFiles);
        // LoadDirectoryAssets("Shared/Audio", AudioManager.LoadAudioFiles);
        LoadFunctions("CommandFunctions.cs", "DevConsole");
        LoadFunctions("ConsoleCommands.lua", "DevConsole");

        HandlePrototypes("ConsoleCommand", PrototypeManager.DevConsole.LoadJsonPrototypes);
        HandlePrototypes("Category", PrototypeManager.SettingsCategories.LoadJsonPrototypes);
        HandlePrototypes("ComponentGroup", PrototypeManager.PerformanceHUD.LoadJsonPrototypes);

        LoadFunctions("SettingsMenuFunctions.cs", "SettingsMenu");
        LoadFunctions("SettingsMenuCommands.lua", "SettingsMenu");

        LoadFunctions("PerformanceHUDFunctions.cs", "PerformanceHUD");
        LoadFunctions("PerformanceHUDCommands.lua", "PerformanceHUD");
    }

    /// <summary>
    /// Loads all the functions using the given file name.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="functionsName">The functions name.</param>
    private void LoadFunctions(string fileName, string functionsName)
    {
        string ext = Path.GetExtension(fileName);
        string folder = "LUA";
        Functions.Type scriptType = Functions.Type.Lua;

        if (string.Compare(".cs", ext, true) == 0)
        {
            folder = "CSharp";
            scriptType = Functions.Type.CSharp;
        }

        LoadTextFile(
            folder,
            fileName,
            (filePath) =>
            {
                if (File.Exists(filePath))
                {
                    string text = File.ReadAllText(filePath);
                    FunctionsManager.Get(functionsName).LoadScript(text, functionsName, scriptType);
                }
                else
                {
                    UnityDebugger.Debugger.LogError(folder == "CSharp" ? "CSharp" : "LUA", "file " + filePath + " not found");
                }
            });
    }

    /// <summary>
    /// Subscribes to the prototypeLoader to handle all prototypes with the given key.
    /// </summary>
    /// <param name="prototypeKey">Key for the prototypes to handle.</param>
    /// <param name="prototypesLoader">Called to handle the prototypes loading.</param>
    private void HandlePrototypes(string prototypeKey, Action<JProperty> prototypesLoader)
    {
        if (!prototypeHandlers.ContainsKey(prototypeKey))
        {
            prototypeHandlers.Add(prototypeKey, new List<Action<JProperty>>());
        }

        // The way these work suggest it should be in a separate class, either a new class (PrototypeLoader?) or in one of the prototype related classes
        prototypeHandlers[prototypeKey].Add(prototypesLoader);
    }

    private void LoadPrototypes()
    {
        string prototypesDirectoryPath = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Data"), "Prototypes");
        DirectoryInfo prototypeDir = new DirectoryInfo(prototypesDirectoryPath);
        FileInfo[] prototypeFiles = prototypeDir.GetFiles("*.json").ToArray();
        Dictionary<string, JToken> tagNameToProperty = new Dictionary<string, JToken>();

        for (int i = 0; i < prototypeFiles.Length; i++)
        {
            FileInfo file = prototypeFiles[i];
            StreamReader reader = File.OpenText(file.FullName);
            JToken protoJson = JToken.ReadFrom(new JsonTextReader(reader));
            UnityDebugger.Debugger.Log("ModsManager", "Loading " + file.FullName);
            string tagName = ((JProperty)protoJson.First).Name;

            tagNameToProperty.Add(tagName, protoJson);
        }

        foreach (KeyValuePair<string, List<Action<JProperty>>> prototypeHandler in prototypeHandlers)
        {
            foreach (Action<JProperty> handler in prototypeHandler.Value)
            {
                foreach (JToken prototypeGroup in tagNameToProperty[prototypeHandler.Key])
                {
                    handler((JProperty)prototypeGroup);
                }
            }
        }
    }

    /// <summary>
    /// Loads all the protoypes using the given file name.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="prototypesLoader">Called to handle the prototypes loading.</param>
    private void LoadPrototypes(string fileName, Action<string> prototypesLoader)
    {
        LoadTextFile(
            "Data",
            fileName,
            (filePath) =>
            {
                string text = File.ReadAllText(filePath);
                prototypesLoader(text);
            });
    }

    /// <summary>
    /// Loads all the character names from the given file.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    private void LoadCharacterNames(string fileName)
    {
        LoadTextFile(
            "Data",
            fileName,
            (filePath) =>
            {
                string[] lines = File.ReadAllLines(filePath);
                CharacterNameManager.LoadNames(lines);
            });
    }

    /// <summary>
    /// Loads the given file from the given folder in the base and inside the mods and
    /// calls the Action with the file path.
    /// </summary>
    /// <param name="directoryName">Directory name.</param>
    /// <param name="fileName">File name.</param>
    /// <param name="readText">Called to handle the text reading and actual loading.</param>
    private void LoadTextFile(string directoryName, string fileName, Action<string> readText)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, directoryName);
        filePath = Path.Combine(filePath, fileName);
        if (File.Exists(filePath))
        {
            readText(filePath);
        }
        else
        {
            UnityDebugger.Debugger.LogError("File at " + filePath + " not found");
        }

        foreach (DirectoryInfo mod in mods)
        {
            filePath = Path.Combine(mod.FullName, fileName);
            if (File.Exists(filePath))
            {
                readText(filePath);
            }
        }
    }

    /// <summary>
    /// Loads the all the assets from the given directory.
    /// </summary>
    /// <param name="directoryName">Directory name.</param>
    /// <param name="readDirectory">Called to handle the loading of each file in the given directory.</param>
    private void LoadDirectoryAssets(string directoryName, Action<string> readDirectory)
    {
        string directoryPath = Path.Combine(Application.streamingAssetsPath, directoryName);
        if (Directory.Exists(directoryPath))
        {
            readDirectory(directoryPath);
        }
        else
        {
            UnityDebugger.Debugger.LogWarning("Directory at " + directoryPath + " not found");
        }

        foreach (DirectoryInfo mod in mods)
        {
            directoryPath = Path.Combine(mod.FullName, directoryName);
            if (Directory.Exists(directoryPath))
            {
                readDirectory(directoryPath);
            }
        }
    }
}
