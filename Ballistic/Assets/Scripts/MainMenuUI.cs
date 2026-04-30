using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private const string InfoCopy =
        "HOW TO PLAY\n\n" +
        "Aim with the mouse and release to shoot a series of balls.\n\n" +
        "Balls bounce off walls and blocks. Each hit reduces a block's HP. When HP reaches zero, the block is destroyed.\n\n" +
        "After all balls return, the board moves down and a new row appears at the top.\n\n" +
        "Collect pickups to gain more balls, trigger special attacks, speed up balls, or change their direction.\n\n" +
        "The game ends when blocks reach the bottom of the board.";

    [Header("Panels")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject infoPanel;

    [Header("Leaderboard")]
    [SerializeField] private TMP_Text leaderboardText;

    [Header("Info")]
    [SerializeField] private TMP_Text infoText;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Auto Setup")]
    [SerializeField] private bool autoWireScene = true;
    [SerializeField] private bool autoLayoutScene = true;

    private Button playButton;
    private Button leaderboardButton;
    private Button infoButton;
    private Button quitButton;
    private Button leaderboardBackButton;
    private Button infoBackButton;

    private void Awake()
    {
        ResolveReferences();

        if (autoWireScene)
        {
            WireButtons();
        }

        if (autoLayoutScene)
        {
            ConfigureSceneLayout();
        }
    }

    private void Start()
    {
        ShowMain();
    }

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        RefreshLeaderboard();
    }

    public void ShowInfo()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
        }

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }

    public void ShowMain()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResolveReferences()
    {
        if (leaderboardPanel == null)
        {
            leaderboardPanel = FindChildObject("LeaderboardPanel");
        }

        if (infoPanel == null)
        {
            infoPanel = FindChildObject("InfoPanel");
        }

        playButton = FindButton("PlayButton");
        leaderboardButton = FindButton("LeaderboardButton");
        infoButton = FindButton("InfoButton");
        quitButton = FindButton("QuitButton");
        leaderboardBackButton = FindButtonInPanel(leaderboardPanel, "BackButton");
        infoBackButton = FindButtonInPanel(infoPanel, "BackButton");

        leaderboardText = FindOrCreatePanelText(leaderboardPanel, leaderboardText, "LeaderboardText");
        infoText = FindOrCreatePanelText(infoPanel, infoText, "InfoText");
    }

    private void WireButtons()
    {
        BindButton(playButton, Play);
        BindButton(leaderboardButton, ShowLeaderboard);
        BindButton(infoButton, ShowInfo);
        BindButton(quitButton, Quit);
        BindButton(leaderboardBackButton, ShowMain);
        BindButton(infoBackButton, ShowMain);
    }

    private void BindButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void ConfigureSceneLayout()
    {
        CanvasScaler canvasScaler = GetComponent<CanvasScaler>();

        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
        }

        TMP_Text titleText = FindText("TitleText");

        if (titleText == null)
        {
            titleText = FindText("TittleText");
        }

        ConfigureTitle(titleText);
        ConfigureButton(playButton, "PLAY", new Vector2(-610f, 260f));
        ConfigureButton(leaderboardButton, "LEADERBOARD", new Vector2(-610f, 175f));
        ConfigureButton(infoButton, "INFO", new Vector2(-610f, 90f));
        ConfigureButton(quitButton, "QUIT", new Vector2(-610f, 5f));
        ConfigurePanel(leaderboardPanel, "LEADERBOARD");
        ConfigurePanel(infoPanel, "INFO");
        ConfigureInfoPanel();
        ConfigurePanelBackButton(leaderboardBackButton);
        ConfigurePanelBackButton(infoBackButton, -280f);
        ConfigureLeaderboardText();
        ConfigureInfoText();
    }

    private void ConfigureTitle(TMP_Text titleText)
    {
        if (titleText == null)
        {
            return;
        }

        titleText.text = "BALLISTIC";
        titleText.fontSize = 118f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;

        RectTransform rectTransform = titleText.rectTransform;
        SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-585f, 390f), new Vector2(760f, 165f));
    }

    private void ConfigureButton(Button button, string label, Vector2 anchoredPosition)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.transform as RectTransform;
        SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(390f, 64f));

        Image image = button.GetComponent<Image>();
        TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);

        if (image != null)
        {
            image.color = new Color(0.94f, 0.96f, 0.99f, 1f);
            image.raycastTarget = true;
            button.targetGraphic = image;
        }
        else if (labelText != null)
        {
            labelText.raycastTarget = true;
            button.targetGraphic = labelText;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.94f, 0.96f, 0.99f, 1f);
        colors.highlightedColor = new Color(0.82f, 0.91f, 1f, 1f);
        colors.pressedColor = new Color(0.64f, 0.78f, 0.95f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
        button.colors = colors;

        if (labelText != null)
        {
            labelText.text = label;
            labelText.fontSize = 30f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = image != null ? new Color(0.08f, 0.11f, 0.16f, 1f) : Color.white;
            labelText.raycastTarget = image == null;
        }
    }

    private void ConfigurePanel(GameObject panel, string title)
    {
        if (panel == null)
        {
            return;
        }

        RectTransform rectTransform = panel.transform as RectTransform;
        SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 540f));

        Image image = panel.GetComponent<Image>();

        if (image == null)
        {
            image = panel.AddComponent<Image>();
        }

        image.color = new Color(0.06f, 0.08f, 0.11f, 0.94f);

        TMP_Text titleText = FindOrCreatePanelText(panel, null, title + "Title");
        titleText.text = title;
        titleText.fontSize = 42f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.raycastTarget = false;
        SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(640f, 74f));
    }

    private void ConfigureInfoPanel()
    {
        if (infoPanel == null)
        {
            return;
        }

        SetRect(infoPanel.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 740f));
    }

    private void ConfigurePanelBackButton(Button button, float yPosition = -214f)
    {
        if (button == null)
        {
            return;
        }

        ConfigureButton(button, "BACK", new Vector2(0f, yPosition));
        SetRect(button.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, yPosition), new Vector2(240f, 56f));
    }

    private void ConfigureLeaderboardText()
    {
        if (leaderboardText == null)
        {
            return;
        }

        leaderboardText.fontSize = 32f;
        leaderboardText.alignment = TextAlignmentOptions.Center;
        leaderboardText.color = Color.white;
        leaderboardText.raycastTarget = false;
        SetRect(leaderboardText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(610f, 270f));
        RefreshLeaderboard();
    }

    private void ConfigureInfoText()
    {
        if (infoText == null)
        {
            return;
        }

        infoText.text = InfoCopy;
        infoText.fontSize = 24f;
        infoText.alignment = TextAlignmentOptions.TopLeft;
        infoText.color = Color.white;
        infoText.raycastTarget = false;
        infoText.enableWordWrapping = true;
        SetRect(infoText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(620f, 520f));
    }

    private void RefreshLeaderboard()
    {
        if (leaderboardText == null)
        {
            return;
        }

        leaderboardText.text = LeaderboardService.GetFormattedLeaderboard();
    }

    private GameObject FindChildObject(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == objectName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private TMP_Text FindText(string objectName)
    {
        GameObject target = FindChildObject(objectName);

        if (target == null)
        {
            return null;
        }

        return target.GetComponent<TMP_Text>();
    }

    private Button FindButton(string objectName)
    {
        GameObject target = FindChildObject(objectName);

        if (target == null)
        {
            return null;
        }

        return EnsureButton(target);
    }

    private Button FindButtonInPanel(GameObject panel, string objectName)
    {
        if (panel == null)
        {
            return null;
        }

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button.name == objectName)
            {
                return button;
            }
        }

        Transform[] children = panel.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == objectName)
            {
                return EnsureButton(child.gameObject);
            }
        }

        return null;
    }

    private Button EnsureButton(GameObject buttonObject)
    {
        Button button = buttonObject.GetComponent<Button>();

        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        if (button.targetGraphic == null)
        {
            button.targetGraphic = buttonObject.GetComponent<Graphic>();
        }

        return button;
    }

    private TMP_Text FindOrCreatePanelText(GameObject panel, TMP_Text currentText, string objectName)
    {
        if (currentText != null)
        {
            return currentText;
        }

        if (panel == null)
        {
            return null;
        }

        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text.name == objectName)
            {
                return text;
            }
        }

        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);

        return textObject.GetComponent<TMP_Text>();
    }

    private void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }
}
