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

/// <summary>
/// A heading for the settings menu.
/// </summary>
public class SettingsHeading : MonoBehaviour
{
    /// <summary>
    /// The root object.
    /// </summary>
    [SerializeField]
    private GameObject root;

    /// <summary>
    /// The heading text to display.
    /// </summary>
    [SerializeField]
    private Text headingText;

    /// <summary>
    /// Sets the text for the heading.
    /// </summary>
    /// <param name="text"></param>
    public void SetText(string text)
    {
        headingText.text = LocalizationTable.GetLocalization(text);
    }

    /// <summary>
    /// Adds the object to the root object.
    /// </summary>
    /// <param name="go"> The object to add. </param>
    public void AddObjectToRoot(GameObject go)
    {
        go.transform.SetParent(root.transform);
    }

    /// <summary>
    /// Removes all objects from root.
    /// </summary>
    public void RemoveObjectsFromRoot()
    {
        foreach (Transform child in root.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
