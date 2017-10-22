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
using ProjectPorcupine.Entities;
using ProjectPorcupine.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages everything required for each game.
/// Only one should exist from beginning the game to ending.
/// </summary>
[MoonSharp.Interpreter.MoonSharpUserData]
public class GameController : MonoBehaviour
{
    /// <summary>
    /// The Game Version.
    /// </summary>
    /// TODO: Should this be also saved with the world data?
    /// If so - beginner task!
    public const string GameVersion = "Someone_will_come_up_with_a_proper_naming_scheme_later";

    /// <summary>
    /// The main scene that gameplay takes place in.
    /// </summary>
    /// TODO: Do we even need this?  Since UI has been decoupled from the _World scene
    public const string MainScene = "_World";

    /// <summary>
    /// The main menu where new/old games are created/loaded.
    /// </summary>
    public const string MainMenuScene = "MainMenu";

    /// <summary>
    /// The circle cursor prefab to use for the cursor.
    /// </summary>
    /// TODO: Make it moddable, so users can change it on the fly (preferably in game)
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

    /// <summary>
    /// Equivalent to <see cref="CurrentSystem"/>.CurrentWorld.
    /// </summary>
    /// <remarks>
    /// Could be null, should check for nullability.
    /// Furthermore should only trust if subsidiary of CurrentSystem or CurrentWorld.
    /// </remarks>
    public static World CurrentWorld
    {
        get { return Instance.CurrentSystem.CurrentWorld; }
    }

    /// <summary>
    /// Use this to access child instances.
    /// </summary>
    /// <remarks>
    /// Shouldn't be null.  Especially if your a subsidiary of GameController.
    /// </remarks>
    public static GameController Instance { get; private set; }

    /// <summary>
    /// Manages inputs from keyboard.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public KeyboardManager KeyboardManager { get; private set; }

    /// <summary>
    /// Manages all sound effects.  Sits ontop of AudioController.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public SoundController SoundController { get; private set; }

    /// <summary>
    /// Manages loaded/unloaded mods.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public ModsManager ModsManager { get; private set; }

    /// <summary>
    /// Manages all dialog boxes.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public DialogBoxManager DialogBoxManager { get; private set; }

    /// <summary>
    /// Manages all furniture sprites.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public FurnitureSpriteController FurnitureSpriteController { get; private set; }

    /// <summary>
    /// Manages all utility sprites.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public UtilitySpriteController UtilitySpriteController { get; private set; }

    /// <summary>
    /// Manages all tile sprites.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public TileSpriteController TileSpriteController { get; private set; }

    /// <summary>
    /// Manages all character sprites.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public CharacterSpriteController CharacterSpriteController { get; private set; }

    /// <summary>
    /// Manages all job sprites.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public JobSpriteController JobSpriteController { get; private set; }

    /// <summary>
    /// Manages all inventory sprites.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public InventorySpriteController InventorySpriteController { get; private set; }

    /// <summary>
    /// Manages all ship sprites.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public ShipSpriteController ShipSpriteController { get; private set; }

    /// <summary>
    /// Manages what currently is selected to build and where.
    /// </summary>
    /// <remarks>
    /// Won't be null while <see cref="Instance"/> isn't null.
    /// </remarks>
    public BuildModeController BuildModeController { get; private set; }

    /// <summary>
    /// The current system controller which manages the world and instances required for
    /// each save.
    /// </summary>
    /// <remarks>
    /// Could be null, can only trust isn't null if your a subsidiary of SystemController.
    /// </remarks>
    public SystemController CurrentSystem { get; private set; }

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

    /// <summary>
    /// Quit the game whether in editor or a build version.
    /// </summary>
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

    /// <summary>
    /// Goes to the main scene creating a new world before.
    /// </summary>
    /// <param name="width"> The width of the world. </param>
    /// <param name="height"> The height of the world. </param>
    /// <param name="depth"> The depth of the world. </param>
    /// <param name="seed"> The seed used for generation. </param>
    /// <param name="generateAsteroids"> Generate Asteroids?. </param>
    /// <param name="generatorFile"> The generator file to use. </param>
    /// <exception cref="ArgumentException"> If <paramref name="width"/> or <paramref name="height"/> or <paramref name="depth"/> is less than or equal to 0. </exception>
    /// <exception cref="FileNotFoundException"> If <paramref name="generatorFile"/> Doesn't exist. </exception>
    public void ToMainScene(int width, int height, int depth, int seed, bool generateAsteroids, string generatorFile)
    {
        if (width <= 0 || height <= 0 || depth <= 0)
        {
            throw new ArgumentException("Width, Height, or Depth is <= 0");
        }

        string path = Path.Combine(GeneratorBasePath, generatorFile);
        if (File.Exists(path) == false)
        {
            throw new FileNotFoundException("File doesn't exist at path: " + path);
        }

        // Load Main World
        CreateSystem();
        CurrentSystem.CreateWorld(width, height, depth, seed, generateAsteroids, generatorFile);
        SceneManager.LoadSceneAsync(MainScene);
    }

    /// <summary>
    /// Goes to the main game scene, loading the initial world.
    /// </summary>
    /// <param name="filePath"> The world to load. </param>
    /// <exception cref="FileNotFoundException"> If filePath is invalid. </exception>
    public void ToMainScene(string filePath)
    {
        if (File.Exists(filePath) == false)
        {
            throw new FileNotFoundException("Couldn't find file at path: " + filePath);
        }

        // Load Main World
        CreateSystem();
        CurrentSystem.LoadWorld(filePath);
        SceneManager.LoadSceneAsync(MainScene);
    }

    /// <summary>
    /// Goes to the main menu scene.
    /// </summary>
    public void ToMainMenu()
    {
        // TODO: Should unassign all worlds!!
        // When multiple worlds are added.
        Destroy(GetComponentInChildren<OverlayMap>().gameObject);
        UnAssignWorld(CurrentWorld);
        CurrentSystem.TearDown();
        CurrentSystem = null; // Removal of current system
        SceneManager.LoadScene(MainMenuScene); // We don't care what the old scene was.
    }

    /// <summary>
    /// Callback occurs when the active scene has changed.
    /// </summary>
    /// <param name="oldScene"> The old scene. </param>
    /// <param name="newScene"> The new scene. </param>
    public void ActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        // If we are going to our main game scene initialize world
        if (newScene.name == MainScene)
        {
            if (OpenedWorldScene)
            {
                // This is for people loading up from _World rather than from MainMenu
                CreateSystem();
                CurrentSystem.CreateWorld(100, 100, 5, UnityEngine.Random.Range(int.MinValue, int.MaxValue), true, "Default.xml");
            }

            IsModal = false;
            IsPaused = false;
            BuildUI();
            CurrentSystem.BuildUI(GameObject.Find("UIMenus"), circleCursorPrefab);
            AssignWorld(CurrentWorld);
        }
    }

    /// <summary>
    /// Assigns the world to the current system.
    /// </summary>
    /// <param name="world"> The world to assign. </param>
    /// <exception cref="ArgumentNullException"> If <paramref name="world"/> is null. </exception>
    public void AssignWorld(World world)
    {
        if (world == null)
        {
            throw new ArgumentNullException("World is null");
        }

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

    /// <summary>
    /// Unassigns the world from the current system.
    /// </summary>
    /// <param name="world"> The world to unassign. </param>
    /// <exception cref="ArgumentNullException"> If <paramref name="world"/> is null. </exception>
    public void UnAssignWorld(World world)
    {
        if (world == null)
        {
            throw new ArgumentNullException("World is null");
        }

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

    /// <summary>
    /// Script initialization; handling singleton, and creation subsidiary managers/controllers.
    /// </summary>
    private void Awake()
    {
        // Singleton management
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

    /// <summary>
    /// Builds the UI.
    /// Should be run when the scene initializes.
    /// </summary>
    private void BuildUI()
    {
        // Initialising controllers.
        GameObject canvas = GameObject.Find("Canvas");

        GameObject uiMenu = new GameObject("UIMenus");
        uiMenu.transform.SetParent(canvas.transform);
        uiMenu.transform.SetAsFirstSibling();
        RectTransform tempTransform = uiMenu.AddComponent<RectTransform>();
        tempTransform.anchorMin = Vector2.zero;
        tempTransform.anchorMax = Vector2.one;
        tempTransform.offsetMin = Vector2.zero;
        tempTransform.offsetMax = Vector2.zero;

        // Instantiate a FPSCounter.
        GameObject temp = (GameObject)Instantiate(Resources.Load("UI/MenuTop"));
        temp.name = "MenuTop";
        temp.transform.SetParent(uiMenu.transform, false);
        GameObject fpsCounter = temp.GetComponentInChildren<PerformanceHUDManager>().gameObject;
        fpsCounter.SetActive(true);

        // Settings UI is a 'dialog box' (kinda), so it comes here.  
        // Where as DevConsole is a constant menu item (it can appear 'anywhere' so it appears after)
        temp = (GameObject)Instantiate(Resources.Load("UI/SettingsMenu/SettingsMenu"));

        if (temp != null)
        {
            temp.name = "Settings Menu";
            temp.transform.SetParent(canvas.transform, false);
            temp.SetActive(true);
        }

        DialogBoxManager = new GameObject("Dialog Boxes").AddComponent<DialogBoxManager>();
        DialogBoxManager.transform.SetParent(canvas.transform, false);
        RectTransform dialogBoxManagerRectTransform = DialogBoxManager.gameObject.AddComponent<RectTransform>();
        dialogBoxManagerRectTransform.anchorMin = Vector2.zero;
        dialogBoxManagerRectTransform.anchorMax = Vector2.one;
        dialogBoxManagerRectTransform.offsetMin = Vector2.zero;
        dialogBoxManagerRectTransform.offsetMax = Vector2.zero;
        DialogBoxManager.CreateUI();

        temp = (GameObject)Instantiate(Resources.Load("UI/Console/DevConsole"));

        if (temp != null)
        {
            temp.name = "DevConsole-Spawned";
            temp.transform.SetParent(canvas.transform, false);
            temp.transform.SetAsLastSibling();
            temp.SetActive(true);
            DeveloperConsole.DevConsole.Close();
        }
    }

    /// <summary>
    /// Creates a new system.
    /// </summary>
    /// <exception cref="InvalidOperationException"> If CreateSystem was previously called, or CurrentSystem has been assigned. </exception>
    /// TODO: Allow for multiple systems perhaps.
    private void CreateSystem()
    {
        if (CurrentSystem != null)
        {
            // We already have a system
            throw new InvalidOperationException("CurrentSystem is already allocated, so a system has already been created.");
        }

        CurrentSystem = new SystemController();
    }

    /// <summary>
    /// Run once per frame, TimeManager handles the ticks from here.
    /// </summary>
    private void Update()
    {
        TimeManager.Instance.Update(Time.deltaTime);
    }

    /// <summary>
    /// Run when the application quits, this cleans up the audiomanager resources.
    /// </summary>
    private void OnApplicationQuit()
    {
        // Ensure that the audiomanager's resources get released properly on quit. This may only be a problem in the editor.
        AudioManager.Destroy();
    }
}