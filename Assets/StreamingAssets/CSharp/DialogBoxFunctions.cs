using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;
using System.Linq;
using DeveloperConsole;
using ProjectPorcupine.Localization;
using ProjectPorcupine.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogBoxNewGame : BaseDialogBox
{
    private InputField CreateLabelInputField(Transform parent, string str)
    {
        GameObject go = GetFluidHorizontalBaseElement(str, true, true);
        HorizontalLayoutGroup layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 30);
        layout.spacing = 10;
        layout.transform.SetParent(parent);
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;

        Text text = CreateTextCustom(str, Color.white, FontAnitaSemiSquare, true, TextAnchor.MiddleLeft);
        text.transform.SetParent(go.transform);

        InputField field = CreateInputField("Enter value");
        field.transform.SetParent(go.transform);
        field.onValidateInput += ValidateInputForPositiveNumber;
        return field;
    }

    public string GetRandomString()
    {
        string characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        StringBuilder result = new StringBuilder(16);
        for (int i = 0; i < 16; i++) {
            result.Append(characters[UnityEngine.Random.Range(0, characters.Length)]);
        }
        return result.ToString();
    }

    public override GameObject InitializeElement()
    {
        GameObject element = new GameObject();
        VerticalLayoutGroup group = element.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 20, 0);
        group.spacing = 0;
        group.childAlignment = TextAnchor.UpperCenter;

        InputField height = CreateLabelInputField(element.transform, "height");
        height.text = "100";
        InputField width = CreateLabelInputField(element.transform, "width");
        width.text = "100";
        InputField depth = CreateLabelInputField(element.transform, "depth");
        depth.text = "5";

        InputField seed = CreateLabelInputField(element.transform, "seed");
        seed.onValidateInput -= ValidateInputForPositiveNumber;
        seed.characterValidation = InputField.CharacterValidation.Alphanumeric;
        seed.text = GetRandomString();

        string generatorDirectoryPath = GameController.Instance.GeneratorBasePath();
        DirectoryInfo generatorDir = new DirectoryInfo(generatorDirectoryPath);
        FileInfo[] worldGenerators = generatorDir.GetFiles("*.json").OrderBy(f => f.Name).ToArray();
        string[] fileNames = worldGenerators.Select(x => Path.GetFileNameWithoutExtension(x.FullName)).ToArray();

        GameObject go = GetFluidHorizontalBaseElement("generateAsteroids", true, true);
        HorizontalLayoutGroup layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 30);
        layout.spacing = 10;
        layout.transform.SetParent(element.transform);
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;

        Text text = CreateTextCustom("Generate Asteroids?", Color.white, FontAnitaSemiSquare, true, TextAnchor.MiddleLeft);
        text.transform.SetParent(go.transform);

        Toggle generateAsteroids = CreateToggle("Toggle");
        generateAsteroids.transform.SetParent(go.transform);

        go = GetFluidHorizontalBaseElement("generator", true, true);
        layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 30);
        layout.spacing = 10;
        layout.transform.SetParent(element.transform);
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;

        text = CreateTextCustom("Select Generator", Color.white, FontAnitaSemiSquare, true, TextAnchor.MiddleLeft);
        text.transform.SetParent(go.transform);

        Dropdown dropdown = CreateDropdownFromText(fileNames, 0);
        dropdown.transform.SetParent(go.transform);

        GameObject bottomBar = GetFluidHorizontalBaseElement("Bottom", true, true, allocatedHeight: 40);
        layout = bottomBar.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 30);
        layout.spacing = 10;
        layout.transform.SetParent(element.transform);
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.LowerRight;
        LayoutElement layoutElement = bottomBar.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 80;
        layoutElement.flexibleHeight = 0;

        AspectRatioFitter ratio = bottomBar.AddComponent<AspectRatioFitter>();
        ratio.aspectRatio = 4;
        ratio.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;

        bottomBar.GetComponent<RectTransform>().pivot = new Vector2(1, 0);

        Button acceptButton = CreateButton("new_world");
        acceptButton.transform.SetParent(bottomBar.transform);
        acceptButton.onClick.AddListener(() => {
            int heightVal = int.Parse(height.text);
            int widthVal = int.Parse(width.text);
            int depthVal = int.Parse(depth.text);

            int seedVal = 0;
            if (seed.text == string.Empty)
            {
                seed.text = GetRandomString();
            }

            // if we can't convert it to an integer just hash it
            if (!int.TryParse(seed.text, out seedVal) == false)
            {
                seedVal = seed.text.GetHashCode();
            }

            string generatorFile = fileNames[dropdown.value] + ".json";
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
            DialogBoxManager.FindInstance().ShowDialogBox("LoadingScreen");
            SceneController.LoadNewWorld(widthVal, heightVal, depthVal, seedVal, generatorFile, generateAsteroids.isOn);
        });

        Button cancel = CreateButton("cancel");
        cancel.transform.SetParent(bottomBar.transform);
        cancel.onClick.AddListener(() => {
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
        });

        return element;
    }

    public char ValidateInputForPositiveNumber(string text, int charIndex, char addedChar)
    {
        char output = addedChar;

        if (addedChar != '1'
            && addedChar != '2'
            && addedChar != '3'
            && addedChar != '4'
            && addedChar != '5'
            && addedChar != '6'
            && addedChar != '7'
            && addedChar != '8'
            && addedChar != '9'
            && addedChar != '0')
        {
            // return a null character
            output = '\0';
        }

        return output;
    }
}

public class DialogBoxQuests : BaseDialogBox
{
    public override GameObject InitializeElement()
    {
        GameObject element = new GameObject();
        VerticalLayoutGroup group = element.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 20, 0);
        group.spacing = 0;
        group.childAlignment = TextAnchor.LowerCenter;
        group.childForceExpandHeight = false;

        Text text = CreateTextCustom("menu_quests", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);

        GameObject scrollObj = new GameObject();
        group = scrollObj.AddComponent<VerticalLayoutGroup>();
        scrollObj.transform.SetParent(element.transform);
        group.padding = new RectOffset(30, 30, 20, 5);
        group.spacing = 0;
        group.childAlignment = TextAnchor.UpperCenter;

        GameObject content = CreateScrollView(scrollObj, false, true, 200, 80);
        group = content.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 0, 0);
        group.spacing = 10;
        ScrollRect scroll = content.GetComponentInParent<ScrollRect>();
        GameObject.Destroy(scroll.GetComponent<LayoutGroup>());

        AutomaticVerticalSize sizer = content.AddComponent<AutomaticVerticalSize>();
        sizer.childHeight = 270f;

        IEnumerable<Quest> quests = Quest.GetCompletableQuests();

        int i = 0;
        foreach (Quest quest in quests)
        {
            GameObject questGo = new GameObject(quest.Name);
            questGo.transform.SetParent(content.transform);
            questGo.AddComponent<Image>().color = (i++ % 2 == 0) ? new Color32(0, 149, 217, 80) : new Color32(0, 149, 217, 160);
            VerticalLayoutGroup vertical = questGo.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(20, 20, 10, 10);
            vertical.spacing = 0;
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childForceExpandHeight = false;

            GameObject textLoc = new GameObject("text");
            textLoc.transform.SetParent(questGo.transform);
            vertical = textLoc.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(0, 0, 0, 0);
            vertical.spacing = 0;
            vertical.childAlignment = TextAnchor.UpperLeft;

            text = CreateTextCustom(quest.Name, Color.white, FontAnitaSemiSquare, false, TextAnchor.UpperCenter, false);
            text.transform.SetParent(textLoc.transform);

            text = CreateTextCustom(quest.Description, Color.white, FontAnitaSemiSquare, false, TextAnchor.MiddleLeft, false);
            text.transform.SetParent(textLoc.transform);

            StringBuilder goals = new StringBuilder("Goals: ");
            foreach (QuestGoal goal in quest.Goals)
            {
                goals.AppendLine();
                goals.Append(goal.IsCompleted ? "<color=green>   - " : "<color=red>   - ");
                goals.Append(goal.Description);
                goals.Append("</color>");
            }

            text = CreateTextCustom(goals.ToString(), Color.white, FontAnitaSemiSquare, false, TextAnchor.MiddleLeft, false);
            text.transform.SetParent(textLoc.transform);

            StringBuilder rewards = new StringBuilder("Rewards: ");
            foreach (QuestReward reward in quest.Rewards)
            {
                rewards.AppendLine();
                rewards.Append("   - ");
                rewards.Append(reward.Description);
            }

            text = CreateTextCustom(rewards.ToString(), Color.white, FontAnitaSemiSquare, false, TextAnchor.MiddleLeft, false);
            text.transform.SetParent(textLoc.transform);

            GameObject buttons = new GameObject("buttons");
            buttons.transform.SetParent(questGo.transform);
            vertical = buttons.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(0, 0, 0, 0);
            vertical.spacing = 0;
            vertical.childAlignment = TextAnchor.LowerRight;
            vertical.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
            AspectRatioFitter tmpRatio = buttons.AddComponent<AspectRatioFitter>();
            tmpRatio.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            tmpRatio.aspectRatio = 4;

            Button accept = CreateButton("accept");
            accept.transform.SetParent(buttons.transform);
            accept.onClick.AddListener(() => {
                Quest copy = quest;
                copy.IsAccepted = true;
                DialogBoxManager.FindInstance().SoftCloseTopDialog();
            });
            GameObject.Destroy(accept.GetComponent<LayoutElement>());
        }

        scroll.scrollSensitivity = content.transform.childCount / 2;

        GameObject bottomBar = GetFluidHorizontalBaseElement("Bottom", true, true, allocatedHeight: 40);
        HorizontalLayoutGroup layout = bottomBar.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 30);
        layout.spacing = 10;
        layout.transform.SetParent(element.transform);
        layout.childAlignment = TextAnchor.LowerRight;
        LayoutElement layoutElement = bottomBar.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 80;
        layoutElement.flexibleHeight = 0;

        AspectRatioFitter ratio = bottomBar.AddComponent<AspectRatioFitter>();
        ratio.aspectRatio = 4;
        ratio.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;

        bottomBar.GetComponent<RectTransform>().pivot = new Vector2(1, 0);

        Button okay = CreateButton("okay");
        okay.transform.SetParent(bottomBar.transform);
        okay.onClick.AddListener(() => {
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
        });
        GameObject.Destroy(okay.GetComponent<LayoutElement>());
        scroll.normalizedPosition = new Vector2(0, 1);

        return element;
    }
}

public class DialogBoxJobListItem : MonoBehaviour, IPointerClickHandler
{
    public DialogBoxJobList box;
    public int index;
    public Character character;

    public void OnPointerClick(PointerEventData eventData)
    {
        box.Clicked(index, character);
    }
}

public class DialogBoxJobList : BaseDialogBox
{
    private Color primaryColor = new Color32(0, 149, 217, 80);
    private Color secondaryColor = new Color32(0, 149, 217, 160);
    private Color selectedColor = new Color32(0, 149, 217, 160);
    private GameObject content;

    public void Clicked(int index, Character character)
    {
        Image[] images = content.GetComponentsInChildren<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            if (i != index)
            {
                images[i].color = i % 2 == 0 ? primaryColor : secondaryColor;
            }
            else
            {
                images[i].color = selectedColor;
            }
            images[i].GetComponentInChildren<Button>().gameObject.SetActive(i == index);
        }

        // center on character
        Vector3 charPosition = new Vector3(character.X, character.Y, Camera.main.transform.position.z);
        Camera.main.transform.position = charPosition;
    }

    public override GameObject InitializeElement()
    {
        GameObject element = new GameObject();
        VerticalLayoutGroup group = element.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 20, 0);
        group.spacing = 0;
        group.childAlignment = TextAnchor.LowerCenter;
        group.childForceExpandHeight = false;

        Text text = CreateTextCustom("job_list", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);

        GameObject scrollObj = new GameObject();
        group = scrollObj.AddComponent<VerticalLayoutGroup>();
        scrollObj.transform.SetParent(element.transform);
        group.padding = new RectOffset(30, 30, 20, 5);
        group.spacing = 0;
        group.childAlignment = TextAnchor.UpperCenter;

        content = CreateScrollView(scrollObj, false, true, 200, 80);
        group = content.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 0, 0);
        group.spacing = 10;
        ScrollRect scroll = content.GetComponentInParent<ScrollRect>();
        GameObject.Destroy(scroll.GetComponent<LayoutGroup>());

        AutomaticVerticalSize sizer = content.AddComponent<AutomaticVerticalSize>();
        sizer.childHeight = 50f;

        int i = 0;
        foreach (Character character in World.Current.CharacterManager)
        {
            GameObject job = new GameObject(i.ToString());
            job.transform.SetParent(content.transform);
            HorizontalLayoutGroup horizGroup = job.AddComponent<HorizontalLayoutGroup>();
            horizGroup.padding = new RectOffset(10, 0, 0, 0);
            horizGroup.spacing = 10;
            horizGroup.childAlignment = TextAnchor.MiddleLeft;

            Image image = job.AddComponent<Image>();
            GameObject.Destroy(job.GetComponent<LayoutElement>());
            job.transform.localScale = Vector3.one;
            image.color = i % 2 == 0 ? primaryColor : secondaryColor;

            string jobDescription = LocalizationTable.GetLocalization(character.GetJobDescription());

            text = CreateTextCustom(string.Format("<b>{0}</b> - {1}", character.GetName(), jobDescription), Color.white, FontAnitaSemiSquare, false, localize: false);
            text.transform.SetParent(job.transform);

            Button delete = CreateTextButton("X");
            delete.transform.SetParent(job.transform);
            delete.onClick.AddListener(() => {
                Character copy = character;
                GameObject copyGo = job;
                copy.InterruptState();
                GameObject.Destroy(copyGo);
            });
            delete.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
            delete.gameObject.SetActive(false);

            DialogBoxJobListItem item = job.AddComponent<DialogBoxJobListItem>();
            item.box = this;
            item.character = character;
            item.index = i;
            i++;
        }

        scroll.scrollSensitivity = content.transform.childCount / 2;

        GameObject bottomBar = GetFluidHorizontalBaseElement("Bottom", true, true, allocatedHeight: 40);
        HorizontalLayoutGroup layout = bottomBar.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 30);
        layout.spacing = 10;
        layout.transform.SetParent(element.transform);
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.LowerCenter;

        Button okay = CreateButton("okay");
        okay.transform.SetParent(bottomBar.transform);
        okay.onClick.AddListener(() => {
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
        });


        return element;
    }
}

public class TradeItemElement
{
    public Text playerStock;
    public InputField tradeAmount;
    public Text tradeStock;
    public TradeItem item;

    public void UpdateInterface()
    {
        playerStock.text = (item.PlayerStock + item.TradeAmount).ToString();
        tradeStock.text = (item.TraderStock - item.TradeAmount).ToString();
        tradeAmount.text = item.TradeAmount.ToString();
    }
}

public class DialogBoxTrade : BaseDialogBox
{
    private GameObject content;
    private Text playerCurrencyBalance;
    private Text traderCurrencyBalance;
    private Text tradeCost;
    private Button acceptButton;
    public Trade currentTrade;
    private Color primaryColor = new Color32(0, 149, 217, 80);
    private Color secondaryColor = new Color32(0, 149, 217, 160);
    public void BuildHeader()
    {
        float tradeAmount = currentTrade.TradeCurrencyBalanceForPlayer;
        playerCurrencyBalance.text = string.Format(
            "{0:N2} {1}", 
            currentTrade.Player.Currency.Balance + currentTrade.TradeCurrencyBalanceForPlayer, 
            currentTrade.Player.Currency.ShortName);
        traderCurrencyBalance.text = string.Format(
            "{0:N2} {1}", 
            currentTrade.Trader.Currency.Balance - currentTrade.TradeCurrencyBalanceForPlayer, 
            currentTrade.Trader.Currency.ShortName);
        tradeCost.text = tradeAmount.ToString("N2");
        acceptButton.interactable = currentTrade.IsValid();
    }

    public DialogBoxTrade(Trade tradeItem)
    {
        this.currentTrade = tradeItem;
    }

    public override GameObject InitializeElement()
    {
        GameObject main = new GameObject("Main");
        VerticalLayoutGroup group = main.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(30, 30, 20, 5);
        group.spacing = 20;
        group.childAlignment = TextAnchor.LowerCenter;
        group.childForceExpandHeight = false;

        GameObject header = new GameObject("Header");
        group = header.AddComponent<VerticalLayoutGroup>();
        header.transform.SetParent(main.transform);
        group.padding = new RectOffset(0, 0, 0, 0);
        group.spacing = 10;
        group.childAlignment = TextAnchor.UpperCenter;
        group.childForceExpandHeight = false;

        Text text = CreateTextCustom("trade", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter);
        text.transform.SetParent(header.transform);

        GameObject headerPanel = new GameObject("Header Panel");
        HorizontalLayoutGroup horizGroup = headerPanel.AddComponent<HorizontalLayoutGroup>();
        headerPanel.transform.SetParent(header.transform);
        horizGroup.padding = new RectOffset(0, 0, 0, 0);
        horizGroup.spacing = 0;
        horizGroup.childAlignment = TextAnchor.UpperCenter;
        horizGroup.childForceExpandHeight = false;

        Text playerName = CreateTextCustom(currentTrade.Player.Name, Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperLeft, false);
        playerName.transform.SetParent(headerPanel.transform);

        playerCurrencyBalance = CreateTextCustom("", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperLeft, false);
        playerCurrencyBalance.transform.SetParent(headerPanel.transform);

        tradeCost = CreateTextCustom("", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter, false);
        tradeCost.transform.SetParent(headerPanel.transform);

        traderCurrencyBalance = CreateTextCustom("", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperRight, true);
        traderCurrencyBalance.transform.SetParent(headerPanel.transform);

        Text traderName = CreateTextCustom(currentTrade.Trader.Name, Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperRight, false);
        traderName.transform.SetParent(headerPanel.transform);

        GameObject scrollview = new GameObject("scrollview");
        group = scrollview.AddComponent<VerticalLayoutGroup>();
        scrollview.transform.SetParent(main.transform);
        group.padding = new RectOffset(0, 0, 10, 0);
        group.spacing = 0;
        group.childAlignment = TextAnchor.MiddleCenter;

        content = CreateScrollView(scrollview, false, true, 200, 80);
        horizGroup = content.AddComponent<HorizontalLayoutGroup>();
        horizGroup.padding = new RectOffset(0, 0, 0, 0);
        horizGroup.spacing = 0;

        // we have 6 columns
        VerticalLayoutGroup[] groups = new VerticalLayoutGroup[6];
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i] = new GameObject(i.ToString()).AddComponent<VerticalLayoutGroup>();
            groups[i].transform.SetParent(horizGroup.transform);
            AutomaticVerticalSize sizer = groups[i].gameObject.AddComponent<AutomaticVerticalSize>();
            sizer.childHeight = 50f;
            groups[i].padding = new RectOffset(0, 0, 0, 0);
            groups[i].spacing = 0;
            groups[i].transform.localScale = Vector3.one;
            Image image = groups[i].gameObject.AddComponent<Image>();
            image.color = i % 2 == 0 ? primaryColor : secondaryColor;
        }

        foreach (TradeItem tradeItem in currentTrade.TradeItems)
        {
            TradeItemElement element = new TradeItemElement();
            element.item = tradeItem;

            text = CreateTextCustom(tradeItem.Type, Color.white, FontAnitaSemiSquare, true, TextAnchor.MiddleCenter);
            text.transform.SetParent(groups[0].transform);

            element.playerStock = CreateTextCustom("", Color.white, FontAnitaSemiSquare, true, TextAnchor.MiddleCenter);
            element.playerStock.transform.SetParent(groups[1].transform);

            text = CreateTextCustom(tradeItem.PlayerSellItemPrice.ToString("N2"), Color.white, FontAnitaSemiSquare, true, TextAnchor.MiddleCenter);
            text.transform.SetParent(groups[2].transform);

            // Buttons
            GameObject buttons = GetFluidHorizontalBaseElement("Buttons", true, true);
            buttons.transform.SetParent(groups[3].transform);
            GameObject.Destroy(buttons.GetComponent<LayoutElement>());
            buttons.transform.localScale = Vector3.one;
            buttons.GetComponent<HorizontalLayoutGroup>().spacing = 0;

            Button buyAll = CreateTextButton("<<", false);
            buyAll.transform.SetParent(buttons.transform);
            buyAll.onClick.AddListener(() => {
                TradeItemElement copy = element;
                copy.item.TradeAmount = copy.item.TraderStock;
                copy.UpdateInterface();
                BuildHeader();
            });

            Button buyOne = CreateTextButton("<", false);
            buyOne.transform.SetParent(buttons.transform);
            buyOne.onClick.AddListener(() => {
                TradeItemElement copy = element;
                if (copy.item.TradeAmount < int.MaxValue)
                {
                    copy.item.TradeAmount++;
                }
                copy.UpdateInterface();
                BuildHeader();
            });

            element.tradeAmount = CreateInputField("0");
            element.tradeAmount.transform.SetParent(buttons.transform);
            element.tradeAmount.contentType = InputField.ContentType.IntegerNumber;
            element.tradeAmount.onEndEdit.AddListener((string val) => {
                TradeItemElement copy = element;
                int tmp;
                if (!int.TryParse(val, out tmp))
                {
                    element.tradeAmount.text = int.MaxValue.ToString();
                    tmp = int.MaxValue;
                }
                copy.UpdateInterface();
                BuildHeader();
            });
            element.tradeAmount.textComponent.alignment = TextAnchor.MiddleCenter;
            element.tradeAmount.gameObject.AddComponent<LayoutElement>().minWidth = 60;
            element.tradeAmount.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            element.tradeAmount.textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            element.tradeAmount.textComponent.resizeTextForBestFit = true;
            element.tradeAmount.textComponent.resizeTextMaxSize = 30;

            Button sellOne = CreateTextButton(">", false);
            sellOne.transform.SetParent(buttons.transform);
            sellOne.onClick.AddListener(() => {
                TradeItemElement copy = element;
                if (copy.item.TradeAmount > int.MinValue)
                {
                    copy.item.TradeAmount--;
                }
                copy.UpdateInterface();
                BuildHeader();
            });

            Button sellAll = CreateTextButton(">>", false);
            sellAll.transform.SetParent(buttons.transform);
            sellAll.onClick.AddListener(() => {
                TradeItemElement copy = element;
                copy.item.TradeAmount = -copy.item.PlayerStock;
                copy.UpdateInterface();
                BuildHeader();
            });

            text = CreateTextCustom(tradeItem.TraderSellItemPrice.ToString("N2"), Color.white, FontAnitaSemiSquare, true, TextAnchor.MiddleCenter);
            text.transform.SetParent(groups[4].transform);

            element.tradeStock = CreateTextCustom("", Color.white, FontAnitaSemiSquare, true, TextAnchor.MiddleCenter);
            element.tradeStock.transform.SetParent(groups[5].transform);
            element.UpdateInterface();
        }

        GameObject bottomBar = GetFluidHorizontalBaseElement("Bottom", true, true, allocatedHeight: 40);
        HorizontalLayoutGroup layout = bottomBar.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 30);
        layout.spacing = 10;
        layout.transform.SetParent(main.transform);
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.LowerRight;
        LayoutElement layoutElement = bottomBar.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 80;
        layoutElement.flexibleHeight = 0;

        AspectRatioFitter ratio = bottomBar.AddComponent<AspectRatioFitter>();
        ratio.aspectRatio = 4;
        ratio.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;

        bottomBar.GetComponent<RectTransform>().pivot = new Vector2(1, 0);

        acceptButton = CreateButton("accept");
        acceptButton.transform.SetParent(bottomBar.transform);
        acceptButton.onClick.AddListener(() => {
            result = ActionResult.Accept;
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
        });

        Button cancel = CreateButton("cancel");
        cancel.transform.SetParent(bottomBar.transform);
        cancel.onClick.AddListener(() => {
            result = ActionResult.Cancel;
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
        });

        BuildHeader();
        return main;
    }
}

public abstract class DialogBoxFileBase : BaseDialogBox
{
    protected GameObject content;
    protected ScrollRect scroll;
    protected InputField textField;
    protected List<DialogBoxListItem> items = new List<DialogBoxListItem>();
    protected Color primaryColor = new Color32(0, 149, 217, 80);
    protected Color secondaryColor = new Color32(0, 149, 217, 160);
    protected Color selectedColor = new Color32(0, 149, 217, 255);

    public void PopulateScrollRect()
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
            string formattedName = string.Format("{0}\n<i><size=11>{1}</size></i>", fileName, file.LastWriteTime);
            GameObject horizontal = GetFluidHorizontalBaseElement(fileName, true, true, allocatedHeight: 60);
            horizontal.transform.SetParent(content.transform);
            HorizontalLayoutGroup group = horizontal.GetComponent<HorizontalLayoutGroup>();
            group.spacing = 10;
            group.padding = new RectOffset(0, 10, 0, 0);

            Image image = horizontal.AddComponent<Image>();
            GameObject.Destroy(horizontal.GetComponent<LayoutElement>());
            horizontal.transform.localScale = Vector3.one;

            image.color = count % 2 == 0 ? primaryColor : secondaryColor;

            Text text = CreateTextCustom(formattedName, Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter, false);
            text.transform.SetParent(horizontal.transform);

            Button delete = CreateTextButton("X");
            delete.transform.SetParent(horizontal.transform);
            delete.onClick.AddListener(() => {
                string name = file.FullName;
                Delete(name);
            });
            // delete.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
            delete.gameObject.SetActive(false);

            DialogBoxListItem item = horizontal.AddComponent<DialogBoxListItem>();
            item.box = this;
            item.fileName = fileName;
            item.fullName = file.FullName;
            item.index = count++;
            item.image = image;
            item.deleteButton = delete;
            items.Add(item);
        }
        scroll.scrollSensitivity = count / 2;
        content.SetActive(true);
    }

    public void Clicked(int indexColoured, int clickAmount)
    {
        if (clickAmount == 1)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (i != indexColoured)
                {
                    items[i].image.color = i % 2 == 0 ? primaryColor : secondaryColor;
                }
                else
                {
                    textField.text = items[i].fileName;
                    items[i].image.color = selectedColor;
                }
                items[i].deleteButton.gameObject.SetActive(i == indexColoured);
            }
        }
        else if (clickAmount >= 2 && items.Count > indexColoured && indexColoured > 0)
        {
            DoubleClick(indexColoured);
        }
    }

    protected void Delete(string file)
    {
        DialogBoxManager.FindInstance().ShowDialogBox("Prompt", DialogBoxActionBuilder.YesCancel(),
            (ActionResult res) => {
                if (res == ActionResult.Yes)
                {
                    File.Delete(file);
                    items.Clear();
                    PopulateScrollRect();
                }
            }, "prompt_delete_file", file);
    }

    protected abstract void DoubleClick(int index);

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
    public Button deleteButton;

    public void OnPointerClick(PointerEventData eventData)
    {
        box.Clicked(index, eventData.clickCount);
    }
}

public class DialogBoxSave : DialogBoxFileBase
{
    protected override void DoubleClick(int index)
    {
        SaveFile(items[index].fileName);
    }

    private void ActuallySaveFile(string filePath)
    {
        DialogBoxManager.FindInstance().SoftCloseTopDialog();
        DialogBoxManager.FindInstance().ShowDialogBox("Prompt", (res) => {
            // Just show a quick 'game saved' then close it
            DialogBoxManager.FindInstance().ShowTemporaryDialogBox("Prompt", 1.5f, "message_game_saved");
        }, "message_saving_game");

        Thread thread = WorldController.Instance.SaveWorld(filePath, () => {
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
        });
    }

    private void SaveFile(string fileName)
    {
        if (fileName == string.Empty)
        {
            DialogBoxManager.FindInstance().ShowDialogBox("Prompt", new DialogBoxActionBuilder().OK(),
                "message_name_or_file_needed_for_save");
            return;
        }

        // Right now fileName is just what was in the dialog box.  We need to pad this out to the full
        // path, plus an extension!
        // In the end, we're looking for something that's going to be similar to this (depending on OS)
        //    <User>\ApplicationData\MyCompanyName\MyGameName\Saves\SaveGameName123.sav
        string path = GameController.Instance.FileSaveBasePath();
        EnsureDirectoryExists(path);
        string filePath = System.IO.Path.Combine(path, fileName + ".sav");

        if (File.Exists(filePath))
        {
            // Give overwrite confirmation
            DialogBoxManager.FindInstance().ShowDialogBox("Prompt", DialogBoxActionBuilder.YesNo(),
            (ActionResult res) => {
                if (res == ActionResult.Yes)
                {
                    // save
                    ActuallySaveFile(filePath);
                }
            }, "prompt_overwrite_existing_file", filePath);
        }
        else
        {
            ActuallySaveFile(filePath);
        }
    }

    public override GameObject InitializeElement()
    {
        GameObject element = new GameObject("Main");
        VerticalLayoutGroup group = element.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 30, 0);
        group.spacing = 0;
        group.childAlignment = TextAnchor.LowerCenter;
        group.childForceExpandHeight = false;

        Text text = CreateTextCustom("save", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);

        GameObject scrollGo = new GameObject("Scroll");
        group = scrollGo.AddComponent<VerticalLayoutGroup>();
        scrollGo.transform.SetParent(element.transform);
        group.padding = new RectOffset(30, 30, 20, 5);
        group.spacing = 0;
        group.childAlignment = TextAnchor.UpperCenter;

        content = CreateScrollView(scrollGo, false, true, 200, 80);
        group = content.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 0, 0);
        group.spacing = 10;
        scroll = content.GetComponentInParent<ScrollRect>();
        GameObject.Destroy(scroll.GetComponent<LayoutGroup>());

        AutomaticVerticalSize sizer = content.AddComponent<AutomaticVerticalSize>();
        sizer.childHeight = 50f;

        GameObject bottomBar = GetFluidHorizontalBaseElement("Bottom", true, true, allocatedHeight: 40);
        HorizontalLayoutGroup layout = bottomBar.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 30);
        layout.spacing = 10;
        layout.transform.SetParent(element.transform);
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.LowerCenter;

        textField = CreateInputField("");
        textField.transform.SetParent(bottomBar.transform);
        AllocateSpaceForGameObject(textField.gameObject, 40, 80);

        Button submit = CreateButton("save");
        submit.transform.SetParent(bottomBar.transform);
        submit.onClick.AddListener(() => {
            SaveFile(textField.text);
        });

        Button cancel = CreateButton("cancel");
        cancel.transform.SetParent(bottomBar.transform);
        cancel.onClick.AddListener(() => {
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
        });

        PopulateScrollRect();
        return element;
    }
}

public class DialogBoxLoad : DialogBoxFileBase
{
    protected override void DoubleClick(int index)
    {
        LoadFile(items[index].fileName);
    }

    private void LoadFile(string fileName)
    {
        if (fileName == string.Empty)
        {
            DialogBoxManager.FindInstance().ShowDialogBox("Prompt", new DialogBoxActionBuilder().OK(), "message_file_needed_for_load");
            return;
        }

        // Right now fileName is just what was in the dialog box.  We need to pad this out to the full
        // path, plus an extension!
        // In the end, we're looking for something that's going to be similar to this (depending on OS)
        //    <User>\ApplicationData\MyCompanyName\MyGameName\Saves\SaveGameName123.sav
        string loc = GameController.Instance.FileSaveBasePath();
        EnsureDirectoryExists(loc);
        string filePath = System.IO.Path.Combine(loc, fileName + ".sav");
        if (!File.Exists(filePath))
        {
            DialogBoxManager.FindInstance().ShowDialogBox("Prompt", new DialogBoxActionBuilder().OK(), "message_file_doesn't_exist", filePath);
        }
        else
        {
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
            DialogBoxManager.FindInstance().ShowDialogBox("LoadingScreen");
            SceneController.LoadWorld(filePath);
        }
    }

    public override GameObject InitializeElement()
    {
        GameObject element = new GameObject("Main");
        VerticalLayoutGroup group = element.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 30, 0);
        group.spacing = 0;
        group.childAlignment = TextAnchor.LowerCenter;
        group.childForceExpandHeight = false;

        Text text = CreateTextCustom("load", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);

        GameObject scrollGo = new GameObject("Scroll");
        group = scrollGo.AddComponent<VerticalLayoutGroup>();
        scrollGo.transform.SetParent(element.transform);
        group.padding = new RectOffset(30, 30, 20, 5);
        group.spacing = 0;
        group.childAlignment = TextAnchor.UpperCenter;

        content = CreateScrollView(scrollGo, false, true, 200, 80);
        group = content.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(0, 0, 0, 0);
        group.spacing = 10;
        scroll = content.GetComponentInParent<ScrollRect>();
        GameObject.Destroy(scroll.GetComponent<LayoutGroup>());

        AutomaticVerticalSize sizer = content.AddComponent<AutomaticVerticalSize>();
        sizer.childHeight = 50f;

        GameObject bottomBar = GetFluidHorizontalBaseElement("Bottom", true, true, allocatedHeight: 40);
        HorizontalLayoutGroup layout = bottomBar.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 0, 30);
        layout.spacing = 10;
        layout.transform.SetParent(element.transform);
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.LowerCenter;

        textField = CreateInputField("");
        textField.transform.SetParent(bottomBar.transform);
        AllocateSpaceForGameObject(textField.gameObject, 40, 80);

        Button submit = CreateButton("load");
        submit.transform.SetParent(bottomBar.transform);
        submit.onClick.AddListener(() => {
            LoadFile(textField.text);
        });

        Button cancel = CreateButton("cancel");
        cancel.transform.SetParent(bottomBar.transform);
        cancel.onClick.AddListener(() => {
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
        });

        PopulateScrollRect();
        return element;
    }
}

public class DialogBoxButtonMenu : BaseDialogBox
{
    private string title;

    public DialogBoxButtonMenu(string title)
    {
        this.title = title;
    }

    public override GameObject InitializeElement()
    {
        GameObject element = GetFluidVerticalBaseElement("Box", true, true);
        VerticalLayoutGroup group = element.GetComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(50, 50, 30, 50);
        group.spacing = 20;

        Text text = CreateTextCustom(title, Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);

        foreach (Actionable button in this.actionableData)
        {
            Button obj = CreateButton(button.Name);
            obj.transform.SetParent(element.transform);
            obj.onClick.AddListener(() => {
                Actionable copy = button;
                result = copy.Return;
                DialogBoxManager.FindInstance().SoftCloseTopDialog();
            });
        }

        return element;
    }
}

public class DialogBoxOptions : BaseDialogBox
{
    public override GameObject InitializeElement()
    {
        GameObject element = GetFluidVerticalBaseElement("Box", true, true);
        VerticalLayoutGroup group = element.GetComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(50, 50, 30, 30);
        group.spacing = 10;
        group.childAlignment = TextAnchor.UpperCenter;

        Text text = CreateTextCustom("menu_options", Color.white, FontAnitaSemiSquare, true, TextAnchor.UpperCenter);
        text.transform.SetParent(element.transform);

        Button resume = CreateButton("menu_resume");
        resume.transform.SetParent(element.transform);
        resume.onClick.AddListener(DialogBoxManager.FindInstance().SoftCloseTopDialog);

        Button newWorld = CreateButton("new_world");
        newWorld.transform.SetParent(element.transform);
        newWorld.onClick.AddListener(() => {
            DialogBoxManager.FindInstance().ShowDialogBox("Prompt", DialogBoxActionBuilder.YesNoCancel(),
                (ActionResult res) => {
                    if (res == ActionResult.Yes)
                    {
                        // save game
                        DialogBoxManager.FindInstance().ShowDialogBox("Save", null, (_) => {
                            DialogBoxManager.FindInstance().SoftCloseTopDialog();
                            DialogBoxManager.FindInstance().ShowDialogBox("LoadingScreen");
                            SceneController.ConfigureNewWorld();
                        });
                    }
                    else if (res == ActionResult.No)
                    {
                        // dont save game
                        // so just load
                        DialogBoxManager.FindInstance().SoftCloseTopDialog();
                        DialogBoxManager.FindInstance().ShowDialogBox("LoadingScreen");
                        SceneController.ConfigureNewWorld();
                    }
                }, "prompt_save_before_creating_new_world");
        });

        Button save = CreateButton("save");
        save.transform.SetParent(element.transform);
        save.onClick.AddListener(() => {
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
            DialogBoxManager.FindInstance().ShowDialogBox("Save");
        });

        Button load = CreateButton("load");
        load.transform.SetParent(element.transform);
        load.onClick.AddListener(() => {
            DialogBoxManager.FindInstance().ShowDialogBox("Prompt", DialogBoxActionBuilder.YesNoCancel(),
                (ActionResult res) => {
                    if (res == ActionResult.Yes)
                    {
                        // save game
                    DialogBoxManager.FindInstance().SoftCloseTopDialog();
                    DialogBoxManager.FindInstance().ShowDialogBox("Save", (_) => {
                        DialogBoxManager.FindInstance().ShowDialogBox("Load");
                    });
                    }
                    else if (res == ActionResult.No)
                    {
                        // dont save game
                        // so just load
                        DialogBoxManager.FindInstance().SoftCloseTopDialog();
                        DialogBoxManager.FindInstance().ShowDialogBox("Load");
                    }
                }, "prompt_save_before_loading_new_game");
        });

        Button settings = CreateButton("menu_settings");
        settings.transform.SetParent(element.transform);
        settings.onClick.AddListener(() => {
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
            SettingsMenu.Open();
        });

        Button quit = CreateButton("menu_quit");
        quit.transform.SetParent(element.transform);
        quit.onClick.AddListener(() => {
            DialogBoxManager.FindInstance().SoftCloseTopDialog();
            DialogBoxActionBuilder actions = new DialogBoxActionBuilder()
                .Cancel("menu_resume")
                .Quit("menu_quit_to_menu")
                .Abort("menu_quit_game");

            DialogBoxManager.FindInstance().ShowDialogBox("ButtonMenu", actions,
                (ActionResult res) => {
                    if (res == ActionResult.Cancel)
                    {
                        DialogBoxManager.FindInstance().SoftCloseAllDialogs();
                    }
                    else if (res == ActionResult.Quit)
                    {
                        DialogBoxManager.FindInstance().SoftCloseAllDialogs();
                        SceneController.LoadMainMenu();
                    }
                    else if (res == ActionResult.Abort)
                    {
                        SceneController.QuitGame();
                    }
                }, "menu_options");
        });

        return element;
    }
}

public class DialogBoxPrompt : BaseDialogBox
{
    string prompt;
    object[] localizationData;
    public DialogBoxPrompt(string prompt, params object[] localizationData)
    {
        this.prompt = prompt;
        this.localizationData = localizationData;
    }

    public override GameObject InitializeElement()
    {
        GameObject element = GetFluidVerticalBaseElement("Box", true, true);
        VerticalLayoutGroup group = element.GetComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(30, 30, 30, 30);
        group.spacing = 10;

        Text text = CreateTextCustom(prompt, Color.white, FontAnitaSemiSquare, true, TextAnchor.MiddleCenter, true, localizationData);
        text.transform.SetParent(element.transform);

        GameObject horizontal = GetFluidHorizontalBaseElement("Buttons", true, true, allocatedHeight: 40);
        horizontal.transform.SetParent(element.transform);
        HorizontalLayoutGroup button_layout = horizontal.GetComponent<HorizontalLayoutGroup>();
        button_layout.padding = new RectOffset(5, 5, 5, 5);
        button_layout.spacing = 5;

        foreach (Actionable button in this.actionableData)
        {
            Button obj = CreateButton(button.Name);
            obj.transform.SetParent(element.transform);
            obj.onClick.AddListener(() => {
                Actionable copy = button;
                result = copy.Return;
                DialogBoxManager.FindInstance().SoftCloseTopDialog();
            });
        }

        return element;
    }
}
