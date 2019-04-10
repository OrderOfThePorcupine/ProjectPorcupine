#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

// Since we modify some of the variables outside of this compilation unit
// The 'field' is not assigned to and will always have it's default value
// Will be true, thus we can disable this warning briefly.
#pragma warning disable 0649

using System.Collections.Generic;
using System.Linq;
using ProjectPorcupine.Localization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Our main options/settings menu class (will contain all options).
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    // For statics
    private static SettingsMenu instance;

    private Dictionary<string, Dictionary<string, BaseSettingsElement[]>> options = new Dictionary<string, Dictionary<string, BaseSettingsElement[]>>();
    private List<BaseSettingsElement> changesTracker = new List<BaseSettingsElement>();

    [SerializeField]
    private GameObject elementRoot;
    [SerializeField]
    private GameObject categoryRoot;
    [SerializeField]
    private GameObject mainRoot;
    [SerializeField]
    private GameObject categoryPrefab;
    [SerializeField]
    private GameObject headingPrefab;
    [SerializeField]
    private Text categoryHeading;
    [SerializeField]
    private ScrollRect settingsScrollRect;

    // For optimising saving
    private string currentCategory = string.Empty;

    public static void Open()
    {
        if (instance == null)
        {
            return;
        }

        GameController.Instance.SoundController.OnButtonSFX();

        instance.changesTracker.Clear();
        instance.mainRoot.SetActive(true);
        DialogBoxManager.FindInstance().IsModal = true;

        if (instance.options.Count > 0)
        {
            DisplayCategory(instance.options.First().Key);
        }
        else
        {
            DisplayCategory("No Settings Loaded");
        }
    }

    public static void DisplayCategory(string category)
    {
        if (instance == null)
        {
            return;
        }

        Dictionary<string, BaseSettingsElement[]> option;

        // Optimisation for saving
        if (instance.currentCategory != string.Empty && instance.currentCategory != category && instance.options.TryGetValue(instance.currentCategory, out option))
        {
            foreach (string headingName in option.Keys)
            {
                BaseSettingsElement[] settingsElements = option[headingName];
                for (int i = 0; i < settingsElements.Length; i++)
                {
                    BaseSettingsElement elementCopy = settingsElements[i];

                    if (elementCopy != null && elementCopy.valueChanged)
                    {
                        instance.changesTracker.Add(elementCopy);
                    }
                }
            }

            if (instance.changesTracker.Count > 0)
            {
                instance.Apply();
            }
        }

        instance.categoryHeading.text = LocalizationTable.GetLocalization(category);
        instance.currentCategory = category;

        // Clear root
        foreach (Transform child in instance.elementRoot.transform)
        {
            child.BroadcastMessage("Despawn", SendMessageOptions.DontRequireReceiver);
            Destroy(child.gameObject);
        }

        foreach (ColorButton button in instance.categoryRoot.GetComponentsInChildren<ColorButton>())
        {
            if (button.gameObject.name != category)
            {
                button.RevertColor();
            }
            else
            {
                button.SelectColor();
            }
        }
        
        if (instance.currentCategory != string.Empty && instance.options.TryGetValue(category, out option))
        {
            foreach (string headingName in option.Keys)
            {
                // Create heading prefab
                SettingsHeading heading = Instantiate(instance.headingPrefab).GetComponent<SettingsHeading>();
                heading.SetText(headingName);
                heading.transform.SetParent(instance.elementRoot.transform);

                BaseSettingsElement[] settingsElements = option[headingName];
                for (int i = 0; i < settingsElements.Length; i++)
                {
                    if (settingsElements[i] != null)
                    {
                        BaseSettingsElement element = settingsElements[i];
                        GameObject go = element.InitializeElement();
                        TooltipComponent tc = go.AddComponent<TooltipComponent>();
                        tc.Tooltip = element.option.tooltip;
                        tc.UseHeavyTextMode = true;

                        heading.AddObjectToRoot(go);
                        element.valueChanged = false;
                    }
                }
            }
        }

        // Update canvases, to allow the normalized position to properly exist.
        Canvas.ForceUpdateCanvases();
        instance.settingsScrollRect.normalizedPosition = new Vector2(0, 1);
    }

    public void Apply()
    {
        Dictionary<string, BaseSettingsElement[]> option;
        if (options.TryGetValue(currentCategory, out option))
        {
            changesTracker.AddRange(option.Values.SelectMany(x => x).Where(x => x != null && x.valueChanged));
        }

        for (int i = 0; i < changesTracker.Count; i++)
        {
            changesTracker[i].ApplySetting();
            changesTracker[i].ApplySettingLUA();
        }
    }

    public void Save()
    {
        Apply();
        Settings.SaveSettings();
        Exit();
    }

    public void Cancel()
    {
        // Add all changes made that haven't been applied or tracked yet
        Dictionary<string, BaseSettingsElement[]> opts;
        if (options.TryGetValue(currentCategory, out opts))
        {
            changesTracker.AddRange(opts.Values.SelectMany(x => x).Where(x => x != null && x.valueChanged));
        }

        // If we have made no changes we can freely exit
        if (changesTracker.Count == 0)
        {
            Exit();
            return;
        }

        DialogBoxManager.FindInstance().ShowDialogBox("Prompt", DialogBoxActionBuilder.YesNo(),
            (ActionResult res) => {
                if (res == ActionResult.Yes)
                {
                    // Cancel changes then quit
                    Settings.LoadSettings();
                    for (int i = 0; i < changesTracker.Count; i++)
                    {
                        changesTracker[i].CancelSetting();
                        changesTracker[i].CancelSettingLUA();
                    }

                    Exit();
                }
                GameController.Instance.SoundController.OnButtonSFX();
            }, "confirm_settings_menu_close");
    }

    public void Default()
    {
        // Reset current category
        foreach (string headingName in instance.options[instance.currentCategory].Keys)
        {
            for (int i = 0; i < instance.options[instance.currentCategory][headingName].Length; i++)
            {
                if (instance.options[instance.currentCategory][headingName][i] != null)
                {
                    Settings.SetSetting(options[instance.currentCategory][headingName][i].option.key, options[instance.currentCategory][headingName][i].option.defaultValue);
                }
            }
        }

        changesTracker.Clear();

        DisplayCategory(currentCategory);
    }

    /// <summary>
    /// Will be considerably slower (it does all the options).
    /// </summary>
    public void ResetAll()
    {
        foreach (string headingName in options.Keys)
        {
            BaseSettingsElement[] values = options[headingName].Values.SelectMany(x => x).ToArray();

            // Reset current category
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                {
                    Settings.SetSetting(values[i].option.key, values[i].option.defaultValue);
                }
            }
        }

        changesTracker.Clear();

        DisplayCategory(currentCategory);
    }

    // Initial State
    private void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = this;
        }
        else
        {
            UnityDebugger.Debugger.LogError("There can only be one Settings Menu per 'game'.  Deleting instance with name: " + gameObject.name);
            Destroy(this.gameObject);
        }
    }

    // Use this for initialization
    private void Start()
    {
        LoadCategories();
    }

    private void Exit()
    {
        currentCategory = string.Empty;
        mainRoot.SetActive(false);
        changesTracker.Clear();
        DialogBoxManager.FindInstance().IsModal = false;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape) && mainRoot.activeInHierarchy == true)
        {
            Cancel();
        }
    }

    /// <summary>
    /// Load our categories.
    /// </summary>
    private void LoadCategories()
    {
        // Clear root
        foreach (Transform child in elementRoot.transform)
        {
            Destroy(child.gameObject);
        }

        // Clear root
        foreach (Transform child in categoryRoot.transform)
        {
            Destroy(child.gameObject);
        }

        options = new Dictionary<string, Dictionary<string, BaseSettingsElement[]>>();

        List<SettingsCategory> categories = PrototypeManager.SettingsCategories.Values;

        for (int i = 0; i < categories.Count; i++)
        {
            ColorButton button = Instantiate(categoryPrefab).GetComponent<ColorButton>();
            button.transform.SetParent(categoryRoot.transform);
            button.name = categories[i].Type;
            button.SetText(LocalizationTable.GetLocalization(categories[i].Type));
            options.Add(categories[i].Type, new Dictionary<string, BaseSettingsElement[]>());

            foreach (KeyValuePair<string, List<SettingsOption>> keyValuePair in categories[i].headings)
            {
                options[categories[i].Type].Add(keyValuePair.Key, new BaseSettingsElement[keyValuePair.Value.Count]);

                for (int j = 0; j < keyValuePair.Value.Count; j++)
                {
                    BaseSettingsElement element;
                    FunctionsManager.SettingsMenu.TryCreateInstance(keyValuePair.Value[j].classData.Type, false, out element);
                    element.option = keyValuePair.Value[j];
                    element.parameterData = keyValuePair.Value[j].classData.Parameters;
                    element.InitializeLUA();
                    options[categories[i].Type][keyValuePair.Key][j] = element;
                }
            }
        }
    }
}
