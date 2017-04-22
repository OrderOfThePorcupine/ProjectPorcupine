#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the PerformanceHUD (new FPS counter).
/// </summary>
public class PerformanceHUDManager : MonoBehaviour
{
    /// <summary>
    /// The current group/mode to display.
    /// </summary>
    private static Dictionary<PerformanceGroup, BasePerformanceHUDComponent[]> allGroups;

    /// <summary>
    /// Instance managament.
    /// </summary>
    private static PerformanceHUDManager instance;

    /// <summary>
    /// What group are we currently at.
    /// </summary>
    private PerformanceGroup groupPointer;

    /// <summary>
    /// What current root are we at.
    /// </summary>
    private int columnRootIndex = 0;

    /// <summary>
    /// All of our root objects.
    /// </summary>
    private List<GameObject> columnRootObjects = new List<GameObject>();

    /// <summary>
    /// Get all the group names.
    /// </summary>
    /// <returns> All the group names. </returns>
    public static string[] GetNames()
    {
        return allGroups.Keys.Select(x => x.Type).ToArray();
    }

    /// <summary>
    /// Loads all the groups.
    /// </summary>
    public static void LoadGroups()
    {
        // Load Settings
        allGroups = new Dictionary<PerformanceGroup, BasePerformanceHUDComponent[]>();

        List<PerformanceGroup> groups = PrototypeManager.PerformanceHUD.Values;

        allGroups.Add(new PerformanceGroup("none", new List<UIClassData>()), new BasePerformanceHUDComponent[0]);
        List<BasePerformanceHUDComponent> elements = new List<BasePerformanceHUDComponent>();

        // Convert the dictionary of specialised elements to a more generalised format
        for (int i = 0; i < groups.Count; i++)
        {
            for (int j = 0; j < groups[i].classData.Count; j++)
            {
                if (FunctionsManager.PerformanceHUD.HasFunction("Get" + groups[i].classData[j].ClassName))
                {
                    BasePerformanceHUDComponent element = FunctionsManager.PerformanceHUD.Call("Get" + groups[i].classData[j].ClassName).ToObject<BasePerformanceHUDComponent>();
                    element.parameterData = groups[i].classData[j].ParameterData;
                    element.InitializeLUA();
                    elements.Add(element);
                }
                else
                {
                    Debug.LogWarning("Get" + groups[i] + groups[i].classData[j].ClassName + "() Doesn't exist");
                }
            }

            allGroups.Add(groups[i], elements.ToArray());
            elements.Clear();
        }
    }

    /// <summary>
    /// Clean and Re-Draw.
    /// </summary>
    public static void DirtyUI()
    {
        if (instance == null)
        {
            return;
        }

        // Clear
        foreach (Transform rootTransform in instance.columnRootObjects.Select(x => x.transform))
        {
            foreach (Transform child in rootTransform)
            {
                child.BroadcastMessage("Despawn", SendMessageOptions.DontRequireReceiver);
                Destroy(child.gameObject);
            }
        }

        instance.groupPointer = allGroups.FirstOrDefault(x => x.Key.Type == SettingsKeyHolder.PerformanceHUD).Key;

        // Set group
        if (instance.groupPointer == null || string.IsNullOrEmpty(instance.groupPointer.Type))
        {
            instance.groupPointer = allGroups.First(x => x.Key.Type == "none").Key;
        }

        // Draw and Begin UI Functionality
        foreach (BasePerformanceHUDComponent elementName in allGroups[instance.groupPointer])
        {
            Transform rootTransfer = GetColumnRootObject().transform;
            GameObject go = elementName.InitializeElement();
            go.transform.SetParent(rootTransfer);
            go.name = elementName.GetName();
        }
    }

    /// <summary>
    /// The root object for the HUD.
    /// </summary>
    private static GameObject GetColumnRootObject()
    {
        if (instance.columnRootIndex < instance.columnRootObjects.Count)
        {
            instance.columnRootIndex++;
            return instance.columnRootObjects[instance.columnRootIndex - 1];
        }
        else if (instance.columnRootIndex > 0)
        {
            instance.columnRootIndex = 0;
            return instance.columnRootObjects[instance.columnRootIndex];
        }
        else if (instance.columnRootObjects.Count == 0)
        {
            throw new System.Exception("Column Root Object Array is empty and the system wants an object");
        }
        else
        {
            instance.columnRootIndex++;
            return GetColumnRootObject();
        }
    }

    /// <summary>
    /// Singleton management.
    /// </summary>
    private void Awake()
    {
        if (instance == this || instance == null)
        {
            instance = this;
        }
        else
        {
            UnityDebugger.Debugger.LogError("There can only be one PerformanceHUD per 'game'.  Deleting instance with name: " + gameObject.name);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Remove self from the every frame call.
    /// </summary>
    private void OnDestroy()
    {
        TimeManager.Instance.EveryFrame -= Instance_EveryFrame;
    }

    /// <summary>
    /// Assign variables, and hookup to API.
    /// </summary>
    private void Start()
    {
        TimeManager.Instance.EveryFrame += Instance_EveryFrame;

        // Root should already exist just grab child
        columnRootObjects = new List<GameObject>();
        foreach (Transform child in transform)
        {
            columnRootObjects.Add(child.gameObject);
        }

        LoadGroups();

        // Setup UI
        DirtyUI();
    }

    /// <summary>
    /// Update instance.
    /// </summary>
    /// <param name="deltaTime"> Time since last update. </param>
    private void Instance_EveryFrame(float deltaTime)
    {
        // Update UI
        foreach (BasePerformanceHUDComponent element in allGroups[groupPointer])
        {
            if (element != null)
            {
                element.Update();
                element.UpdateLUA();
            }
        }
    }
}
