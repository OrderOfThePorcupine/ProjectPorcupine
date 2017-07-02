#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SystemController
{
    public SystemController()
    {
        if (SceneController.LoadWorldFromFileName != null)
        {
            CreateWorldFromSaveFile(SceneController.LoadWorldFromFileName);
            SceneController.LoadWorldFromFileName = null;
        }
        else
        {
            CreateEmptyWorld();
        }

        Path = new GameObject("VisualPath").AddComponent<VisualPath>();

        GameObject canvas = GameObject.Find("Canvas");

        // Add the currency display
        GameObject currencyDisplay = GameObject.Instantiate(Resources.Load("UI/CurrencyDisplay"), canvas.GetComponentInChildren<PerformanceHUDManager>().transform.parent) as GameObject;

        GameObject contextMenu = GameObject.Instantiate(Resources.Load("UI/ContextMenu"), canvas.transform.position, canvas.transform.rotation, canvas.transform) as GameObject;
        contextMenu.name = "ContextMenu";

        GameObject timeScale = GameObject.Instantiate(Resources.Load("UI/TimeScale"), GameObject.Find("TopRight").transform, false) as GameObject;
        timeScale.name = "TimeScale";

        GameObject dateTimeDisplay = GameObject.Instantiate(Resources.Load("UI/DateTimeDisplay"), canvas.transform, false) as GameObject;
        dateTimeDisplay.name = "DateTimeDisplay";

        SpawnInventoryController = new SpawnInventoryController();
        QuestController = new QuestController();
        TradeController = new TradeController();
        AutosaveManager = new AutosaveManager();

        GameController.Instance.DialogBoxManager.transform.SetAsLastSibling();

        // Hiding Dev Mode spawn inventory controller if devmode is off.
        SpawnInventoryController.SetUIVisibility(SettingsKeyHolder.DeveloperMode);
    }

    public SpawnInventoryController SpawnInventoryController { get; private set; }

    public QuestController QuestController { get; private set; }

    public AutosaveManager AutosaveManager { get; private set; }

    public TradeController TradeController { get; private set; }

    public World CurrentWorld { get; private set; }

    public VisualPath Path { get; private set; }

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

    private void CreateEmptyWorld()
    {
        CurrentWorld = SceneController.CreateNewWorld();

        // Center the Camera
        Camera.main.transform.position = new Vector3(CurrentWorld.Width / 2, CurrentWorld.Height / 2, Camera.main.transform.position.z);
    }

    private void CreateWorldFromSaveFile(string fileName)
    {
        UnityDebugger.Debugger.Log("WorldController", "CreateWorldFromSaveFile");

        CurrentWorld = new World();
        CurrentWorld.ReadJson(fileName);
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
