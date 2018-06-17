#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
using ProjectPorcupine.Localization;
using UnityEngine;
using UnityEngine.UI;

public class LanguageDropdownUpdater : MonoBehaviour
{
    private void Start()
    {
        UpdateLanguageDropdown();
        LocalizationTable.CBLocalizationFilesChanged += UpdateLanguageDropdown;
    }

    private void OnDestroy()
    {
        LocalizationTable.CBLocalizationFilesChanged -= UpdateLanguageDropdown;
    }

    private void UpdateLanguageDropdown()
    {
        Dropdown dropdown = GetComponentInChildren<Dropdown>();
        string[] languages = LocalizationTable.GetLanguages();
        dropdown.ClearOptions();

        foreach (string lang in languages)
        {
            dropdown.options.Add(new DropdownValue(lang));
        }

        for (int i = 0; i < languages.Length; i++)
        {
            if (languages[i] == LocalizationTable.currentLanguage)
            {
                dropdown.value = i;
                break;
            }
        }

        // Set scroll sensitivity based on the save-item count.
        dropdown.template.GetComponent<ScrollRect>().scrollSensitivity = dropdown.options.Count / 3;
    }

    public class DropdownValue : Dropdown.OptionData
    {
        public string language;

        public DropdownValue(string lang)
        {
            language = lang;
            text = LocalizationTable.GetLocalizaitonCodeLocalization(lang);
        }
    }
}
