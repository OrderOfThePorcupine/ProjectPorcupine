#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software,
// and you are welcome to redistribute it under certain conditions; See
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
#pragma warning disable 0649
using System.IO;
using System.Threading;
using MoonSharp.Interpreter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectPorcupine.Entities;
using ProjectPorcupine.Mouse;
using Scheduler;
using UnityEngine;

[MoonSharpUserData]
public class WorldController : MonoBehaviour
{
    [SerializeField]
    private GameObject circleCursorPrefab;

    [SerializeField]
    private GameObject inventoryUI;

    #region Instances
    public static WorldController Instance { get; protected set; }

    public SoundController SoundController { get; private set; }

    public TileSpriteController TileSpriteController { get; private set; }

    public CharacterSpriteController CharacterSpriteController { get; private set; }

    public JobSpriteController JobSpriteController { get; private set; }

    public InventorySpriteController InventorySpriteController { get; private set; }

    public ShipSpriteController ShipSpriteController { get; private set; }

    public FurnitureSpriteController FurnitureSpriteController { get; private set; }

    public UtilitySpriteController UtilitySpriteController { get; private set; }

    public QuestController QuestController { get; private set; }

    public BuildModeController BuildModeController { get; private set; }

    public MouseController MouseController { get; private set; }

    public CameraController CameraController { get; private set; }

    public SpawnInventoryController SpawnInventoryController { get; private set; }

    public AutosaveManager AutosaveManager { get; private set; }

    public TradeController TradeController { get; private set; }

    public ModsManager ModsManager { get; private set; }

    public DialogBoxManager DialogBoxManager { get; private set; }

    // The world and tile data.
    public World World { get; protected set; }

    #endregion

    public GameObject CircleCursorPrefab
    {
        get
        {
            return circleCursorPrefab;
        }
    }

    public void OnEnable()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
        }
        else
        {
            UnityDebugger.Debugger.LogError("WorldController", "There should never be two world controllers.");
        }

        FunctionsManager.Initialize();
        PrototypeManager.Initialize();
        CharacterNameManager.Initialize();
        SpriteManager.Initialize();
        AudioManager.Initialize();

        // FIXME: Do something real here. This is just to show how to register a C# event prototype for the Scheduler.
        PrototypeManager.ScheduledEvent.Add(
            new ScheduledEvent(
                "ping_log",
                (evt) => UnityDebugger.Debugger.LogFormat("Scheduler", "Event {0} fired", evt.Name)));

        ModsManager = new ModsManager();

        // Reload incase any mods have changed settings
        Settings.LoadSettings();

        if (SceneController.LoadWorldFromFileName != null)
        {
            CreateWorldFromSaveFile(SceneController.LoadWorldFromFileName);
            SceneController.LoadWorldFromFileName = null;
        }
        else
        {
            CreateEmptyWorld();
        }

        SoundController = new SoundController(World);
    }

    public void Start()
    {
        // Create GameObject so we can have access to a transform which has a position of "Vector3.zero".
        GameObject go;

        TileSpriteController = new TileSpriteController(World);
        CharacterSpriteController = new CharacterSpriteController(World);
        FurnitureSpriteController = new FurnitureSpriteController(World);
        UtilitySpriteController = new UtilitySpriteController(World);
        JobSpriteController = new JobSpriteController(World, FurnitureSpriteController, UtilitySpriteController);
        InventorySpriteController = new InventorySpriteController(World, inventoryUI);
        ShipSpriteController = new ShipSpriteController(World);

        BuildModeController = new BuildModeController();
        SpawnInventoryController = new SpawnInventoryController();
        MouseController = new MouseController();
        QuestController = new QuestController();
        CameraController = new CameraController();
        TradeController = new TradeController();
        AutosaveManager = new AutosaveManager();

        MouseController.RegisterModeHandler(MouseMode.BUILD, BuildModeController);
        MouseController.RegisterModeHandler(MouseMode.INVENTORY, SpawnInventoryController);

        // Register inputs actions
        KeyboardManager.Instance.RegisterInputAction("DevMode", KeyboardMappedInputType.KeyDown, ChangeDevMode);

        // Hiding Dev Mode spawn inventory controller if devmode is off.
        SpawnInventoryController.SetUIVisibility(SettingsKeyHolder.DeveloperMode);

        CameraController.Initialize();

        // Initialising controllers.
        GameObject canvas = GameObject.Find("Canvas");
        go = Instantiate(Resources.Load("UI/ContextMenu"), canvas.transform.position, canvas.transform.rotation, canvas.transform) as GameObject;
        go.name = "ContextMenu";

        GameObject timeScale = Instantiate(Resources.Load("UI/TimeScale"), GameObject.Find("TopRight").transform, false) as GameObject;
        timeScale.name = "TimeScale";

        GameObject dateTimeDisplay = Instantiate(Resources.Load("UI/DateTimeDisplay"), canvas.transform, false) as GameObject;
        dateTimeDisplay.name = "DateTimeDisplay";

        GameController.Instance.IsModal = false;

        // Settings UI is a 'dialog box' (kinda), so it comes here.  
        // Where as DevConsole is a constant menu item (it can appear 'anywhere' so it appears after)
        GameObject settingsMenu = (GameObject)Instantiate(Resources.Load("UI/SettingsMenu/SettingsMenu"));

        if (settingsMenu != null)
        {
            settingsMenu.name = "Settings Menu";
            settingsMenu.transform.SetParent(canvas.transform, false);
            settingsMenu.SetActive(true);
        }

        // This will place it after context menu (and the inventory menu) and settings menu
        DialogBoxManager = GameObject.Find("Dialog Boxes").GetComponent<DialogBoxManager>();
        DialogBoxManager.transform.SetAsLastSibling();

        GameObject devConsole = (GameObject)Instantiate(Resources.Load("UI/Console/DevConsole"));

        if (devConsole != null)
        {
            devConsole.name = "DevConsole-Spawned";
            devConsole.transform.SetParent(canvas.transform, false);
            devConsole.transform.SetAsLastSibling();
            devConsole.SetActive(true);
            DeveloperConsole.DevConsole.Close();
        }
    }

    /// <summary>
    /// Gets the tile at the Unity-space coordinates.
    /// </summary>
    /// <returns>The tile at world coordinate.</returns>
    /// <param name="coord">Unity World-Space coordinates.</param>
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);

        return World.GetTileAt(x, y, (int)coord.z);
    }

    public void Destroy()
    {
        TimeManager.Instance.Destroy();
        KeyboardManager.Instance.Destroy();
        Scheduler.Scheduler.Current.Destroy();
        GameMenuManager.Instance.Destroy();
    }

    /// <summary>
    /// Change the developper mode.
    /// </summary>
    public void ChangeDevMode()
    {
        bool mode = !SettingsKeyHolder.DeveloperMode;
        SettingsKeyHolder.DeveloperMode = mode;
        SpawnInventoryController.SetUIVisibility(mode);
        ///FurnitureBuildMenu.instance.RebuildMenuButtons(developerMode);
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
        if (Directory.Exists(GameController.Instance.FileSaveBasePath()) == false)
        {
            // NOTE: This can throw an exception if we can't create the folder,
            // but why would this ever happen? We should, by definition, have the ability
            // to write to our persistent data folder unless something is REALLY broken
            // with the computer/device we're running on.
            Directory.CreateDirectory(GameController.Instance.FileSaveBasePath());
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

    /// <summary>
    /// Create/overwrite the save file with the serialized text.
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

    private void CreateEmptyWorld()
    {
        World = SceneController.CreateNewWorld();

        // Center the Camera
        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);
    }

    private void CreateWorldFromSaveFile(string fileName)
    {
        UnityDebugger.Debugger.Log("WorldController", "CreateWorldFromSaveFile");

        World = new World();
        World.ReadJson(fileName);
    }
}