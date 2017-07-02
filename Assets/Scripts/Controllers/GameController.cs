#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using ProjectPorcupine.Entities;
using ProjectPorcupine.Localization;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // TODO: Should this be also saved with the world data?
    // If so - beginner task!
    public static readonly string GameVersion = "Someone_will_come_up_with_a_proper_naming_scheme_later";

    [SerializeField]
    private GameObject circleCursorPrefab;

    /// <summary>
    /// Path to all file saves.
    /// </summary>
    public static string FileSaveBasePath
    {
        get { return System.IO.Path.Combine(Application.persistentDataPath, "Saves"); }
    }

    /// <summary>
    /// Path to all the generator files.
    /// </summary>
    public static string GeneratorBasePath
    {
        get { return System.IO.Path.Combine(Application.streamingAssetsPath, "WorldGen"); }
    }

    #region Instances

    public static GameController Instance { get; protected set; }

    public KeyboardManager KeyboardManager { get; private set; }

    public AudioManager AudioManager { get; private set; }

    public ModsManager ModsManager { get; private set; }

    public DialogBoxManager DialogBoxManager { get; private set; }

    public FurnitureSpriteController FurnitureSpriteController { get; private set; }

    public UtilitySpriteController UtilitySpriteController { get; private set; }

    public TileSpriteController TileSpriteController { get; private set; }

    public CharacterSpriteController CharacterSpriteController { get; private set; }

    public JobSpriteController JobSpriteController { get; private set; }

    public InventorySpriteController InventorySpriteController { get; private set; }

    public ShipSpriteController ShipSpriteController { get; private set; }

    public BuildModeController BuildModeController { get; private set; }

    public MouseController MouseController { get; private set; }

    public CameraController CameraController { get; private set; }

    public SystemController CurrentSystem { get; private set; }

    public World CurrentWorld
    {
        get { return CurrentSystem.CurrentWorld; }
    }

    #endregion

    /// <summary>
    /// Is any dialog box open?.
    /// </summary>
    public bool IsModal { get; set; }

    /// <summary>
    /// Is the game paused?.
    /// </summary>
    public bool IsPaused
    {
        get
        {
            return TimeManager.Instance.IsPaused || IsModal;
        }

        set
        {
            TimeManager.Instance.IsPaused = value;
        }
    }

    /// <summary>
    /// Change the developper mode.
    /// </summary>
    public void ChangeDevMode()
    {
        bool newMode = !SettingsKeyHolder.DeveloperMode;
        SettingsKeyHolder.DeveloperMode = newMode;

        if (CurrentSystem != null)
        {
            CurrentSystem.ChangeDevMode(newMode);
        }
    }

    // Each time a scene is loaded.
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        // FIXME: Do something real here. This is just to show how to register a C# event prototype for the Scheduler.
        /*
        PrototypeManager.ScheduledEvent.Add(
            new ScheduledEvent(
                "ping_log",
                (evt) => UnityDebugger.Debugger.LogFormat("Scheduler", "Event {0} fired", evt.Name)));
        */

        DontDestroyOnLoad(this);

        new FunctionsManager();
        new PrototypeManager();
        new CharacterNameManager();
        new SpriteManager();

        // Load Keyboard Mapping.
        KeyboardManager = KeyboardManager.Instance;

        AudioManager = new AudioManager();
        ModsManager = new ModsManager();

        FurnitureSpriteController = new FurnitureSpriteController();
        UtilitySpriteController = new UtilitySpriteController();
        TileSpriteController = new TileSpriteController();
        CharacterSpriteController = new CharacterSpriteController();
        JobSpriteController = new JobSpriteController();
        InventorySpriteController = new InventorySpriteController(Resources.Load<GameObject>("UI/InventoryUI"));
        ShipSpriteController = new ShipSpriteController();

        BuildModeController = new BuildModeController();
        MouseController = new MouseController(circleCursorPrefab);
        CameraController = new CameraController();
        CameraController.Initialize();

        KeyboardManager.RegisterInputAction("Pause", KeyboardMappedInputType.KeyUp, () => { IsPaused = !IsPaused; });

        // Initialising controllers.
        GameObject canvas = GameObject.Find("Canvas");

        // Instantiate a FPSCounter.
        GameObject menuTop = (GameObject)Instantiate(Resources.Load("UI/MenuTop"));
        menuTop.name = "MenuTop";
        menuTop.transform.SetParent(canvas.transform, false);
        GameObject fpsCounter = menuTop.GetComponentInChildren<PerformanceHUDManager>().gameObject;
        fpsCounter.SetActive(true);

        DialogBoxManager = new GameObject("Dialog Boxes").AddComponent<DialogBoxManager>();
        DialogBoxManager.transform.SetParent(canvas.transform, false);

        // Settings UI is a 'dialog box' (kinda), so it comes here.  
        // Where as DevConsole is a constant menu item (it can appear 'anywhere' so it appears after)
        GameObject settingsMenu = (GameObject)Instantiate(Resources.Load("UI/SettingsMenu/SettingsMenu"));

        if (settingsMenu != null)
        {
            settingsMenu.name = "Settings Menu";
            settingsMenu.transform.SetParent(canvas.transform, false);
            settingsMenu.SetActive(true);
        }

        GameObject devConsole = (GameObject)Instantiate(Resources.Load("UI/Console/DevConsole"));

        if (devConsole != null)
        {
            devConsole.name = "DevConsole-Spawned";
            devConsole.transform.SetParent(canvas.transform, false);
            devConsole.transform.SetAsLastSibling();
            devConsole.SetActive(true);
            DeveloperConsole.DevConsole.Close();
        }

        IsModal = false;
        IsPaused = false;
    }

    // Only on first time a scene is loaded.
    private void Start()
    {
        // Load settings.
        Settings.LoadSettings();

        // Add a gameobject that Localization
        this.gameObject.AddComponent<LocalizationLoader>();
    }

    private void Update()
    {
        TimeManager.Instance.Update(Time.deltaTime);
    }

    private void OnApplicationQuit()
    {
        // Ensure that the audiomanager's resources get released properly on quit. This may only be a problem in the editor.
        AudioManager.Destroy();
    }
}