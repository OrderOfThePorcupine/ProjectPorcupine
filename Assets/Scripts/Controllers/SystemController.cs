#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[MoonSharp.Interpreter.MoonSharpUserData]
public class SystemController
{
    /// <summary>
    /// We should be in the _WORLD scene, when this is called.
    /// </summary>
    public SystemController()
    {
        QuestController = new QuestController();
        TradeController = new TradeController();
        AutosaveManager = new AutosaveManager();
    }

    public SpawnInventoryController SpawnInventoryController { get; private set; }

    public QuestController QuestController { get; private set; }

    public AutosaveManager AutosaveManager { get; private set; }

    public TradeController TradeController { get; private set; }

    public World CurrentWorld { get; private set; }

    public VisualPath Path { get; private set; }

    public void BuildUI(GameObject uiMenus)
    {
        Path = new GameObject("VisualPath").AddComponent<VisualPath>();

        GameObject canvas = GameObject.Find("Canvas");

        SpawnInventoryController = new SpawnInventoryController();

        // Add the currency display
        GameObject currencyDisplay = GameObject.Instantiate(Resources.Load("UI/CurrencyDisplay"), uiMenus.GetComponentInChildren<PerformanceHUDManager>().transform.parent) as GameObject;
        currencyDisplay.name = "CurrencyDisplay";

        GameObject gameMenu = GameObject.Instantiate(Resources.Load("UI/GameMenu"), uiMenus.transform) as GameObject;
        gameMenu.name = "GameMenu";

        GameObject menuleft = GameObject.Instantiate(Resources.Load("UI/MenuLeft"), uiMenus.transform) as GameObject;
        menuleft.name = "MenuLeft";

        GameObject headlines = GameObject.Instantiate(Resources.Load("UI/Headlines"), uiMenus.transform) as GameObject;
        headlines.name = "Headlines";

        GameObject menuRight = GameObject.Instantiate(Resources.Load("UI/MenuRight"), uiMenus.transform) as GameObject;
        menuRight.name = "MenuRight";

        GameObject contextMenu = GameObject.Instantiate(Resources.Load("UI/ContextMenu"), canvas.transform.position, canvas.transform.rotation, canvas.transform) as GameObject;
        contextMenu.name = "ContextMenu";

        GameObject timeScale = GameObject.Instantiate(Resources.Load("UI/TimeScale"), GameObject.Find("TopRight").transform, false) as GameObject;
        timeScale.name = "TimeScale";

        GameObject dateTimeDisplay = GameObject.Instantiate(Resources.Load("UI/DateTimeDisplay"), canvas.transform, false) as GameObject;
        dateTimeDisplay.name = "DateTimeDisplay";

        GameController.Instance.DialogBoxManager.transform.SetAsLastSibling();
        GameController.Instance.DialogBoxManager.CreateSystemUI();

        // Hiding Dev Mode spawn inventory controller if devmode is off.
        SpawnInventoryController.SetUIVisibility(SettingsKeyHolder.DeveloperMode);
    }

    public void ChangeDevMode(bool newDevMode)
    {
        SpawnInventoryController.SetUIVisibility(newDevMode);
    }

    public void TearDown()
    {
        GameObject.Destroy(Path);
    }

    /// <summary>
    /// Serializes current Instance of the World and starts a thread
    /// that actually saves serialized world to HDD.
    /// </summary>
    /// <param name="filePath">Where to save (Full path).</param>
    /// <returns>Returns the thread that is currently saving data to HDD.</returns>
    public Thread SaveWorld(string filePath)
    {
        // Make sure the save folder exists.
        if (Directory.Exists(GameController.FileSaveBasePath) == false)
        {
            // NOTE: This can throw an exception if we can't create the folder,
            // but why would this ever happen? We should, by definition, have the ability
            // to write to our persistent data folder unless something is REALLY broken
            // with the computer/device we're running on.
            Directory.CreateDirectory(GameController.FileSaveBasePath);
        }

        StreamWriter sw = new StreamWriter(filePath);
        JsonWriter writer = new JsonTextWriter(sw);

        JObject worldJson = World.Current.ToJson();

        // Launch saving operation in a separate thread.
        // This reduces lag while saving by a little bit.
        Thread t = new Thread(new ThreadStart(delegate { SaveWorldToHdd(worldJson, writer); }));
        t.Start();

        return t;
    }

    public void CreateWorld(int width, int height, int depth, int seed, bool generateAsteroids = true, string generatorFile = "Default.xml")
    {
        UnityDebugger.Debugger.Log("WorldController", "Empty World");
        ProjectPorcupine.Localization.LocalizationTable.UnregisterDelegates();

        CurrentWorld = new World(width, height, depth, seed, generateAsteroids, generatorFile);
        Camera.main.transform.position = new Vector3(CurrentWorld.Width / 2, CurrentWorld.Height / 2, Camera.main.transform.position.z);
    }

    public void LoadWorld(string fileName)
    {
        UnityDebugger.Debugger.Log("WorldController", "CreateWorldFromSaveFile");
        ProjectPorcupine.Localization.LocalizationTable.UnregisterDelegates();

        CurrentWorld = new World(fileName);
        Camera.main.transform.position = new Vector3(CurrentWorld.Width / 2, CurrentWorld.Height / 2, Camera.main.transform.position.z);
    }

    public void AssignWorld(World world)
    {
    }

    public void UnAssignWorld(World world)
    {
    }

    /// <summary>
    /// Create/overwrite the save file with the XML text.
    /// </summary>
    /// <param name="filePath">Full path to file.</param>
    /// <param name="writer">TextWriter that contains serialized World data.</param>
    private void SaveWorldToHdd(JObject worldJson, JsonWriter writer)
    {
        JsonSerializer serializer = new JsonSerializer()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };
        serializer.Serialize(writer, worldJson);

        writer.Flush();
    }
}
