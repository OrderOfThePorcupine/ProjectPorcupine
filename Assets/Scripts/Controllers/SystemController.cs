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

    public MouseController MouseController { get; private set; }

    public CameraController CameraController { get; private set; }

    public OverlayMap OverlayMap { get; private set; }

    public World CurrentWorld { get; private set; }

    public VisualPath Path { get; private set; }

    public void BuildUI(GameObject uiMenus, GameObject circleCursorPrefab)
    {
        GameObject canvas = GameObject.Find("Canvas");

        GameObject temp = GameObject.Instantiate(Resources.Load<GameObject>("UI/ContextMenu")) as GameObject;
        temp.name = "ContextMenu";
        temp.transform.SetParent(canvas.transform);
        temp.transform.SetAsFirstSibling();
        RectTransform tempTransform = temp.GetComponent<RectTransform>();
        tempTransform.anchorMin = new Vector2(0, 1);
        tempTransform.anchorMax = new Vector2(0, 1);
        tempTransform.offsetMin = Vector2.zero;
        tempTransform.offsetMax = new Vector2(10, 0);

        GameObject topRight = GameObject.Instantiate(Resources.Load<GameObject>("UI/TopRight")) as GameObject;
        topRight.name = "TopRight";
        topRight.transform.SetParent(canvas.transform);
        topRight.transform.SetAsFirstSibling();
        tempTransform = topRight.GetComponent<RectTransform>();
        tempTransform.anchorMin = Vector2.one;
        tempTransform.anchorMax = Vector2.one;
        tempTransform.anchoredPosition3D = new Vector3(-171.2f, 0f, 0f);
        tempTransform.sizeDelta = new Vector2(343.9f, 100f);
        tempTransform.pivot = new Vector2(0.5f, 1f);

        OverlayMap = ((GameObject)GameObject.Instantiate(Resources.Load("UI/Overlay"), GameController.Instance.transform)).GetComponent<OverlayMap>();
        OverlayMap.Init(topRight);

        MouseController = new MouseController(circleCursorPrefab);
        CameraController = new CameraController();

        Path = new GameObject("VisualPath").AddComponent<VisualPath>();

        SpawnInventoryController = new SpawnInventoryController();

        // Add the currency display
        temp = GameObject.Instantiate(Resources.Load("UI/CurrencyDisplay"), uiMenus.GetComponentInChildren<PerformanceHUDManager>().transform.parent) as GameObject;
        temp.name = "CurrencyDisplay";

        temp = GameObject.Instantiate(Resources.Load("UI/GameMenu"), uiMenus.transform) as GameObject;
        temp.name = "GameMenu";

        temp = GameObject.Instantiate(Resources.Load("UI/MenuLeft"), uiMenus.transform) as GameObject;
        temp.name = "MenuLeft";

        temp = GameObject.Instantiate(Resources.Load("UI/Headlines"), uiMenus.transform) as GameObject;
        temp.name = "Headlines";

        temp = GameObject.Instantiate(Resources.Load("UI/MenuRight"), uiMenus.transform) as GameObject;
        temp.name = "MenuRight";

        temp = GameObject.Instantiate(Resources.Load("UI/ContextMenu"), canvas.transform.position, canvas.transform.rotation, canvas.transform) as GameObject;
        temp.name = "ContextMenu";

        temp = GameObject.Instantiate(Resources.Load("UI/TimeScale"), topRight.transform, false) as GameObject;
        temp.name = "TimeScale";

        temp = GameObject.Instantiate(Resources.Load("UI/DateTimeDisplay"), canvas.transform, false) as GameObject;
        temp.name = "DateTimeDisplay";

        uiMenus.transform.SetAsLastSibling();

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
    }

    public void LoadWorld(string fileName)
    {
        UnityDebugger.Debugger.Log("WorldController", "CreateWorldFromSaveFile");
        ProjectPorcupine.Localization.LocalizationTable.UnregisterDelegates();

        CurrentWorld = new World(fileName);
    }

    public void AssignWorld(World world)
    {
        CameraController.Initialize(world);
        Camera.main.transform.position = new Vector3(CurrentWorld.Width / 2, CurrentWorld.Height / 2, Camera.main.transform.position.z);
        OverlayMap.OnWorldChange(world);
    }

    public void UnAssignWorld(World world)
    {
        OverlayMap.OnWorldChange(null);
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
