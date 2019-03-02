using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeveloperConsole;
using ProjectPorcupine.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class DialogBoxFileBase : BaseDialogBox
{
    protected InputField textField;
    protected List<DialogBoxListItem> items = new List<DialogBoxListItem>();

    public void Clicked(int indexColoured, int clickAmount)
    {
        if (clickAmount == 1)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (i != indexColoured)
                {
                    items[i].image.color = new Color32(0, 149, 217, 80);
                }
                else
                {
                    textField.text = items[i].fileName;
                    items[i].image.color = new Color32(0, 149, 217, 160);
                }
            }
        }
        else if (clickAmount >= 2 && items.Count > indexColoured && indexColoured > 0)
        {
            DoubleClick(indexColoured);
        }
    }

    protected void Delete(string file)
    {
        var data = new Dictionary<string, object>()
        {
            { "Prompt", "prompt_delete_file" },
            { "Buttons", new string[] { "button_yes", "cancel" } },
            { "PromptLocalizationData", new object[] { file } }
        };
        GameController.Instance.DialogBoxManager.ShowDialogBox("Prompt", data, (res) => {
            if (res["ExitButton"].ToString() == "button_yes")
            {
                // delete file
                File.Delete(file);
                items.Clear();
                PopulateScrollRect();
            }
        });
    }

    protected abstract void DoubleClick(int index);

    protected abstract void PopulateScrollRect();

    /// <summary>
    /// If directory doesn't exist EnsureDirectoryExists will create one.
    /// </summary>
    /// <param name="directoryPath">Full directory path.</param>
    protected void EnsureDirectoryExists(string directoryPath)
    {
        if (Directory.Exists(directoryPath) == false)
        {
            UnityDebugger.Debugger.LogWarning("DialogBoxLoadSaveGame", "Directory: " + directoryPath + " doesn't exist - creating.");
            Directory.CreateDirectory(directoryPath);
        }
    }

    protected IEnumerable<FileInfo> GetSaveList()
    {
        string saveDirectoryPath = GameController.Instance.FileSaveBasePath();
        EnsureDirectoryExists(saveDirectoryPath);

        DirectoryInfo saveDir = new DirectoryInfo(saveDirectoryPath);
        return saveDir.GetFiles("*.sav").OrderByDescending(f => f.LastWriteTime);
    }
}

public class DialogBoxListItem : MonoBehaviour, IPointerClickHandler
{
    public DialogBoxFileBase box;
    public string fileName;
    public Image image;
    public int index;
    public string fullName;

    public void OnPointerClick(PointerEventData eventData)
    {
        box.Clicked(index, eventData.clickCount);
    }
}

public class DialogBoxLoad : DialogBoxFileBase
{
    GameObject content;
    protected override void DoubleClick(int index)
    {
        LoadFile(items[index].fullName);
    }

    private void LoadFile(string fullName)
    {
        Debug.Log("Loaded: " + fullName);
    }

    protected override void PopulateScrollRect()
    {
        content.SetActive(false);
        foreach (Transform child in content.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        int count = 0;
        foreach (FileInfo file in GetSaveList())
        {
            string fileName = Path.GetFileNameWithoutExtension(file.FullName);
            string formattedName = string.Format("{0}\n<i>{1}</i>", fileName, file.LastWriteTime);
            GameObject horizontal = GetFluidHorizontalBaseElement(fileName, true, true, allocatedHeight: 60);
            horizontal.transform.SetParent(content.transform);
            Image image = horizontal.AddComponent<Image>();
            GameObject.Destroy(horizontal.GetComponent<LayoutElement>());
            horizontal.transform.localScale = Vector3.one;

            image.color = new Color32(0, 149, 217, 80);

            Text text = CreateTextCustom(formattedName, Color.black, FontAnitaSemiSquare, true, TextAnchor.UpperCenter, false);
            text.transform.SetParent(horizontal.transform);

            Button delete = CreateButton("");
            delete.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/x");
            delete.transition = Button.Transition.None;
            delete.transform.SetParent(horizontal.transform);
            delete.onClick.AddListener(() => {
                string name = file.FullName;
                Delete(name);
            });
            AspectRatioFitter ratio = delete.gameObject.AddComponent<AspectRatioFitter>();
            ratio.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;

            DialogBoxListItem item = horizontal.AddComponent<DialogBoxListItem>();
            item.box = this;
            item.fileName = fileName;
            item.fullName = file.FullName;
            item.index = count++;
            item.image = image;
            items.Add(item);
        }
        content.SetActive(true);
    }

    public override GameObject InitializeElement()
    {
        result = new Parameter();
        GameObject element = new GameObject();
        VerticalLayoutGroup group = element.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 0, 0);
        group.spacing = 0;
        group.childAlignment = TextAnchor.LowerCenter;

        GameObject textAndScroll = new GameObject();
        group = textAndScroll.AddComponent<VerticalLayoutGroup>();
        textAndScroll.transform.SetParent(element.transform);
        group.padding = new RectOffset(30, 30, 20, 5);
        group.spacing = 0;
        group.childAlignment = TextAnchor.UpperCenter;

        Text text = CreateTextCustom("load", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter);
        text.transform.SetParent(textAndScroll.transform);

        content = CreateScrollView(textAndScroll, false, true, 200, 80);
        group = content.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 0, 0);
        group.spacing = 10;

        AutomaticVerticalSize sizer = content.AddComponent<AutomaticVerticalSize>();
        sizer.childHeight = 50f;

        GameObject bottom_bar = GetFluidHorizontalBaseElement("Bottom", true, true, allocatedHeight: 40);
        HorizontalLayoutGroup layout = bottom_bar.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 30);
        layout.spacing = 10;
        layout.transform.SetParent(element.transform);
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.LowerCenter;

        textField = CreateInputField("");
        textField.transform.SetParent(bottom_bar.transform);
        AllocateSpaceForGameObject(textField.gameObject, 40, 80);

        Button submit = CreateButton("submit");
        submit.transform.SetParent(bottom_bar.transform);
        submit.onClick.AddListener(() => {
            LoadFile(textField.text);
        });

        Button cancel = CreateButton("cancel");
        cancel.transform.SetParent(bottom_bar.transform);
        cancel.onClick.AddListener(() => {
            GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
        });

        PopulateScrollRect();
        return element;
    }
}

public class DialogBoxButtonMenu : BaseDialogBox
{
    public override GameObject InitializeElement()
    {
        result = new Parameter();
        GameObject element = GetFluidVerticalBaseElement("Box", true, true);
        VerticalLayoutGroup group = element.GetComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(50, 50, 30, 50);
        group.spacing = 20;

        string title = GetStringParam("Title");
        string[] buttons = GetStringArray("Buttons");

        Text text = CreateTextCustom(title, Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);

        foreach (string button in buttons)
        {
            Button obj = CreateButton(button.Trim());
            obj.transform.SetParent(element.transform);
            obj.onClick.AddListener(() => {
                string copy = button;
                result.AddParameter(new Parameter("ExitButton", copy));
                GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
            });
        }

        return element;
    }
}

public class DialogBoxOptions : BaseDialogBox
{
    public override GameObject InitializeElement()
    {
        result = new Parameter();
        GameObject element = GetFluidVerticalBaseElement("Box", true, true);
        VerticalLayoutGroup group = element.GetComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(50, 50, 30, 30);
        group.spacing = 10;
        group.childForceExpandHeight = false;

        Text text = CreateTextCustom("menu_options", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);

        Button resume = CreateButton("menu_resume");
        resume.transform.SetParent(element.transform);
        resume.onClick.AddListener(GameController.Instance.DialogBoxManager.SoftCloseTopDialog);

        Button newWorld = CreateButton("new_world");
        newWorld.transform.SetParent(element.transform);
        newWorld.onClick.AddListener(() => {
            var data = new Dictionary<string, object>()
            {
                { "Prompt", "prompt_save_before_creating_new_world" },
                { "Buttons", new string[] { "button_yes", "button_no", "cancel" } }
            };
            GameController.Instance.DialogBoxManager.ShowDialogBox("Prompt", data, (res) => {
                if (res["ExitButton"].ToString() == "button_yes")
                {
                    // save game
                    GameController.Instance.DialogBoxManager.ShowDialogBox("Save", null, (_) => {
                        GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
                        GameController.Instance.DialogBoxManager.ShowDialogBox("LoadingScreen");
                        SceneController.ConfigureNewWorld();
                    });
                }
                else if (res["ExitButton"].ToString() == "button_no")
                {
                    // dont save game
                    // so just load
                    GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
                    GameController.Instance.DialogBoxManager.ShowDialogBox("LoadingScreen");
                    SceneController.ConfigureNewWorld();
                }
                // else we don't have to do anything
                // the cancel will just close the prompt save window
                // and since it won't then open the load window
                // it'll just go back to the options menu as normal
            });
        });

        Button save = CreateButton("save");
        save.transform.SetParent(element.transform);
        save.onClick.AddListener(() => {
            GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
            GameController.Instance.DialogBoxManager.ShowDialogBox("Save", null, null);
        });

        Button load = CreateButton("load");
        load.transform.SetParent(element.transform);
        load.onClick.AddListener(() => {
            var data = new Dictionary<string, object>()
            {
                { "Prompt", "prompt_save_before_loading_new_game" },
                { "Buttons", new string[] { "button_yes", "button_no", "cancel" } }
            };
            GameController.Instance.DialogBoxManager.ShowDialogBox("Prompt", data, (res) => {
                if (res["ExitButton"].ToString() == "button_yes")
                {
                    // save game
                    GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
                    GameController.Instance.DialogBoxManager.ShowDialogBox("Save", null, (_) => {
                        GameController.Instance.DialogBoxManager.ShowDialogBox("Load", null, null);
                    });
                }
                else if (res["ExitButton"].ToString() == "button_no")
                {
                    // dont save game
                    // so just load
                    GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
                    GameController.Instance.DialogBoxManager.ShowDialogBox("Load", null, null);
                }
                // else we don't have to do anything
                // the cancel will just close the prompt save window
                // and since it won't then open the load window
                // it'll just go back to the options menu as normal
            });
        });

        Button settings = CreateButton("menu_settings");
        settings.transform.SetParent(element.transform);
        settings.onClick.AddListener(() => {
            // we hide the menu but don't close it yet
            // this is so that the IsModal flag is still on for the entirety
            // of settings menu being open.
            // @TODO: Make settings menu a dialog
            element.SetActive(false);
            SettingsMenu.Open();
            GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
        });

        Button quit = CreateButton("menu_quit");
        quit.transform.SetParent(element.transform);
        quit.onClick.AddListener(() => {
            GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
            var data = new Dictionary<string, object>()
            {
                { "Title", "menu_options" },
                { "Buttons", new string[] { "menu_resume", "menu_quit_to_menu", "menu_quit_game" } }
            };
            GameController.Instance.DialogBoxManager.ShowDialogBox("ButtonMenu", data, (Parameter res) => {
                if (res["ExitButton"].ToString() == "menu_resume")
                {
                    GameController.Instance.DialogBoxManager.SoftCloseAllDialogs();
                }
                else if (res["ExitButton"].ToString() == "menu_quit_to_menu")
                {
                    GameController.Instance.DialogBoxManager.SoftCloseAllDialogs();
                    SceneController.LoadMainMenu();
                }
                else if (res["ExitButton"].ToString() == "menu_quit_game")
                {
                    SceneController.QuitGame();
                }
            });
        });

        return element;
    }
}

public class DialogBoxPrompt : BaseDialogBox
{
    public override GameObject InitializeElement()
    {
        result = new Parameter();
        GameObject element = GetFluidVerticalBaseElement("Box", true, true);
        VerticalLayoutGroup group = element.GetComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(30, 30, 30, 30);
        group.spacing = 10;

        string prompt = GetStringParam("Prompt");
        string[] buttons = GetStringArray("Buttons");
        object[] extraData = GetObjectArray("PromptLocalizationData", false) ?? new object[0];

        Text text = CreateTextCustom(prompt, Color.white, FontAnitaSemiSquare, true, TextAnchor.MiddleCenter, true, extraData);
        text.transform.SetParent(element.transform);

        GameObject horizontal = GetFluidHorizontalBaseElement("Buttons", true, true, allocatedHeight: 40);
        horizontal.transform.SetParent(element.transform);
        HorizontalLayoutGroup button_layout = horizontal.GetComponent<HorizontalLayoutGroup>();
        button_layout.padding = new RectOffset(5, 5, 5, 5);
        button_layout.spacing = 5;

        foreach (string button in buttons)
        {
            Button obj = CreateButton(button.Trim());
            obj.transform.SetParent(horizontal.transform);
            obj.onClick.AddListener(() => {
                string copy = button;
                result.AddParameter(new Parameter("ExitButton", copy));
                GameController.Instance.DialogBoxManager.SoftCloseTopDialog();
            });
        }

        return element;
    }
}
