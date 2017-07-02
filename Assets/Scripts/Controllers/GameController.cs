#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using ProjectPorcupine.Entities;
using ProjectPorcupine.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

[MoonSharp.Interpreter.MoonSharpUserData]
public class GameController : MonoBehaviour
{
    // TODO: Should this be also saved with the world data?
    // If so - beginner task!
    public const string GameVersion = "Someone_will_come_up_with_a_proper_naming_scheme_later";

    public const string MainScene = "_World";
    public const string MainMenuScene = "MainMenu";

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

    public static GameController Instance { get; private set; }

    public KeyboardManager KeyboardManager { get; private set; }

    public SoundController SoundController { get; private set; }

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

    public SystemController CurrentSystem { get; private set; }

    /// <summary>
    /// Equivalent to <see cref="CurrentSystem"/>.CurrentWorld.
    /// </summary>
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
    /// Whether or not the user opened the game from _World.
    /// </summary>
    public bool OpenedWorldScene { get; private set; }

    // Quit the app whether in editor or a build version.
    public static void QuitGame()
    {
        // Maybe ask the user if he want to save or is sure they want to quit??
#if UNITY_EDITOR
        // Allows you to quit in the editor.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

    public void ToMainScene(int width, int height, int depth, int seed, bool generateAsteroids, string generatorFile)
    {
        // Load Main World
        CreateSystem();
        CurrentSystem.CreateWorld(width, height, depth, seed, generateAsteroids, generatorFile);
        SceneManager.LoadSceneAsync(MainScene);
    }

    public void ToMainScene(string fileName)
    {
        // Load Main World
        CreateSystem();
        CurrentSystem.LoadWorld(fileName);
        SceneManager.LoadSceneAsync(MainScene);
    }

    public void ToMainMenu()
    {
        // Should unassign all worlds!!
        // But since this only allows for multi-worlds not enables it
        // I won't include it
        Destroy(GetComponentInChildren<OverlayMap>().gameObject);
        UnAssignWorld(CurrentWorld);
        CurrentSystem.TearDown();
        CurrentSystem = null; // Removal of current system
        SceneManager.LoadScene(MainMenuScene); // We don't care what the old scene was.
    }

    public void ActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        if (newScene.name == MainScene)
        {
            if (OpenedWorldScene)
            {
                // This is for people loading up from _World
                CreateSystem();
                CurrentSystem.CreateWorld(100, 100, 5, UnityEngine.Random.Range(int.MinValue, int.MaxValue), true, "Default.xml");
            }

            IsModal = false;
            IsPaused = false;
            BuildUI();
            Instantiate(Resources.Load("UI/Overlay"), this.transform);
            CurrentSystem.BuildUI(GameObject.Find("UIMenus"), circleCursorPrefab);
            AssignWorld(CurrentWorld);
        }
    }

    public void AssignWorld(World world)
    {
        CharacterSpriteController.AssignWorld(world);
        FurnitureSpriteController.AssignWorld(world);
        InventorySpriteController.AssignWorld(world);
        JobSpriteController.AssignWorld(world);
        ShipSpriteController.AssignWorld(world);
        TileSpriteController.AssignWorld(world);
        UtilitySpriteController.AssignWorld(world);

        SoundController.AssignWorld(world);

        CurrentSystem.AssignWorld(world);
    }

    public void UnAssignWorld(World world)
    {
        CharacterSpriteController.UnAssignWorld(world);
        FurnitureSpriteController.UnAssignWorld(world);
        InventorySpriteController.UnAssignWorld(world);
        JobSpriteController.UnAssignWorld(world);
        ShipSpriteController.UnAssignWorld(world);
        TileSpriteController.UnAssignWorld(world);
        UtilitySpriteController.UnAssignWorld(world);

        SoundController.UnAssignWorld(world);

        CurrentSystem.UnAssignWorld(world);
    }

    private void Awake()
    {
        if (Instance == null || Instance == this)
        {
            Instance = this;
        }
        else
        {
            // This is fine!
            Debug.LogWarning("We already have a GameController, so we are going to destroy 'this' copy.  This is fine, and will occur once per scene change.");
            Destroy(this.gameObject);
            return;
        }

        // FIXME: Do something real here. This is just to show how to register a C# event prototype for the Scheduler.
        /*
        PrototypeManager.ScheduledEvent.Add(
            new ScheduledEvent(
                "ping_log",
                (evt) => UnityDebugger.Debugger.LogFormat("Scheduler", "Event {0} fired", evt.Name)));
        */

        DontDestroyOnLoad(this);
        SceneManager.activeSceneChanged += ActiveSceneChanged;
        IsModal = false;
        IsPaused = true;

        new FunctionsManager();     // We don't need to maintain a reference for this
        new PrototypeManager();     // We don't need to maintain a reference for this
        new CharacterNameManager(); // We don't need to maintain a reference for this
        new SpriteManager();        // We don't need to maintain a reference for this

        // Load Keyboard Mapping.
        KeyboardManager = KeyboardManager.Instance;
        SoundController = new SoundController();
        ModsManager = new ModsManager();

        FurnitureSpriteController = new FurnitureSpriteController();
        UtilitySpriteController = new UtilitySpriteController();
        TileSpriteController = new TileSpriteController();
        CharacterSpriteController = new CharacterSpriteController();
        JobSpriteController = new JobSpriteController();
        InventorySpriteController = new InventorySpriteController(Resources.Load<GameObject>("UI/InventoryUI"));
        ShipSpriteController = new ShipSpriteController();

        BuildModeController = new BuildModeController();

        KeyboardManager.RegisterInputAction("Pause", KeyboardMappedInputType.KeyUp, () => { IsPaused = !IsPaused; });

        // Load settings.
        Settings.LoadSettings();

        // Add a gameobject that applies localization to scene
        this.gameObject.AddComponent<LocalizationLoader>();

        if (SceneManager.GetActiveScene().name == MainScene)
        {
            OpenedWorldScene = true;
        }
        else if (SceneManager.GetActiveScene().name == MainMenuScene)
        {
            // Initialising controllers.
            GameObject canvas = GameObject.Find("Canvas");

            GameObject backgroundGO = new GameObject("Background");
            backgroundGO.AddComponent<SpriteRenderer>().sprite = SpriteManager.GetRandomSprite("Background");

            // Create a Title.
            GameObject title = (GameObject)Instantiate(Resources.Load("UI/TitleMainMenu"));
            title.transform.SetParent(canvas.transform, false);
            title.SetActive(true);

            // Display Main Menu.
            GameObject mainMenu = (GameObject)Instantiate(Resources.Load("UI/MainMenu"));
            mainMenu.transform.SetParent(canvas.transform, false);
            mainMenu.SetActive(true);

            BuildUI();
        }
        else
        {
            // This means that in the future if any more scenes are added we don't forget to add a case.
            throw new NotImplementedException("New scene detected, need to add a new case in GameController.Awake");
        }
    }

    private void BuildUI()
    {
        // Initialising controllers.
        GameObject canvas = GameObject.Find("Canvas");

        GameObject uiMenus = new GameObject("UIMenus");
        uiMenus.transform.SetParent(canvas.transform);
        uiMenus.transform.SetAsFirstSibling();
        RectTransform uiMenuTransform = uiMenus.AddComponent<RectTransform>();
        uiMenuTransform.anchorMin = Vector2.zero;
        uiMenuTransform.anchorMax = Vector2.one;
        uiMenuTransform.offsetMin = Vector2.zero;
        uiMenuTransform.offsetMax = Vector2.zero;

        // Instantiate a FPSCounter.
        GameObject menuTop = (GameObject)Instantiate(Resources.Load("UI/MenuTop"));
        menuTop.name = "MenuTop";
        menuTop.transform.SetParent(uiMenus.transform, false);
        GameObject fpsCounter = menuTop.GetComponentInChildren<PerformanceHUDManager>().gameObject;
        fpsCounter.SetActive(true);

        // Settings UI is a 'dialog box' (kinda), so it comes here.  
        // Where as DevConsole is a constant menu item (it can appear 'anywhere' so it appears after)
        GameObject settingsMenu = (GameObject)Instantiate(Resources.Load("UI/SettingsMenu/SettingsMenu"));

        if (settingsMenu != null)
        {
            settingsMenu.name = "Settings Menu";
            settingsMenu.transform.SetParent(canvas.transform, false);
            settingsMenu.SetActive(true);
        }

        DialogBoxManager = new GameObject("Dialog Boxes").AddComponent<DialogBoxManager>();
        DialogBoxManager.transform.SetParent(canvas.transform, false);
        RectTransform dialogBoxManagerRectTransform = DialogBoxManager.gameObject.AddComponent<RectTransform>();
        dialogBoxManagerRectTransform.anchorMin = Vector2.zero;
        dialogBoxManagerRectTransform.anchorMax = Vector2.one;
        dialogBoxManagerRectTransform.offsetMin = Vector2.zero;
        dialogBoxManagerRectTransform.offsetMax = Vector2.zero;
        DialogBoxManager.CreateUI();

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

    private void CreateSystem()
    {
        if (CurrentSystem != null)
        {
            // We already have a system
            throw new Exception("CurrentSystem is already allocated, so a system has already been created.");
        }

        CurrentSystem = new SystemController();
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