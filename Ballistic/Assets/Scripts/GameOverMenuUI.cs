using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverMenuUI : MonoBehaviour
{
    private const string LastPlayerNameKey = "last_player_name";

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text saveStatusText;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateMissingUI = true;

    private int pendingScore;
    private int pendingLevel;
    private bool scoreSaved;

    private void Awake()
    {
        if (autoCreateMissingUI)
        {
            CreateMissingUI();
        }

        ResolveReferences();
        WireButtons();
        Hide();
    }

    public void Show(int score, int level)
    {
        pendingScore = score;
        pendingLevel = level;
        scoreSaved = false;

        if (panel != null)
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        if (summaryText != null)
        {
            summaryText.text = $"Score: {score}\nLevel: {level}";
        }

        if (saveStatusText != null)
        {
            saveStatusText.text = "";
        }

        if (nicknameInput != null)
        {
            nicknameInput.interactable = true;
            nicknameInput.text = PlayerPrefs.GetString(LastPlayerNameKey, "");

            if (nicknameInput.placeholder != null)
            {
                nicknameInput.placeholder.gameObject.SetActive(string.IsNullOrEmpty(nicknameInput.text));
            }
        }

        if (saveButton != null)
        {
            saveButton.interactable = true;
            SetButtonText(saveButton, "SAVE SCORE");
        }

        FocusNicknameInput();
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void SaveScore()
    {
        if (scoreSaved)
        {
            return;
        }

        string playerName = nicknameInput != null ? nicknameInput.text : "";
        playerName = LeaderboardService.NormalizePlayerName(playerName);

        LeaderboardService.SubmitScore(playerName, pendingScore, pendingLevel);
        PlayerPrefs.SetString(LastPlayerNameKey, playerName);
        PlayerPrefs.Save();

        scoreSaved = true;

        if (nicknameInput != null)
        {
            nicknameInput.text = playerName;
            nicknameInput.interactable = false;
        }

        if (saveButton != null)
        {
            saveButton.interactable = false;
            SetButtonText(saveButton, "SAVED");
        }

        if (saveStatusText != null)
        {
            saveStatusText.text = "Saved to leaderboard.";
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void CreateMissingUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            return;
        }

        if (panel == null)
        {
            panel = FindChildObject("GameOverPanel");
        }

        if (panel == null)
        {
            panel = CreatePanel(canvas.transform);
        }

        summaryText = summaryText != null ? summaryText : FindOrCreateText(panel.transform, "GameOverSummaryText", new Vector2(0f, 100f), new Vector2(520f, 95f), 34f, TextAlignmentOptions.Center);
        saveStatusText = saveStatusText != null ? saveStatusText : FindOrCreateText(panel.transform, "GameOverSaveStatusText", new Vector2(0f, -86f), new Vector2(520f, 40f), 22f, TextAlignmentOptions.Center);
        nicknameInput = nicknameInput != null ? nicknameInput : FindOrCreateInput(panel.transform);
        saveButton = saveButton != null ? saveButton : FindOrCreateButton(panel.transform, "SaveScoreButton", "SAVE SCORE", new Vector2(0f, -154f), new Vector2(260f, 56f));
        mainMenuButton = mainMenuButton != null ? mainMenuButton : FindOrCreateButton(panel.transform, "MainMenuButton", "MAIN MENU", new Vector2(0f, -224f), new Vector2(260f, 56f));

        TMP_Text titleText = FindOrCreateText(panel.transform, "GameOverTitleText", new Vector2(0f, 206f), new Vector2(520f, 70f), 48f, TextAlignmentOptions.Center);
        titleText.text = "GAME OVER";
        titleText.fontStyle = FontStyles.Bold;

        TMP_Text nicknameLabel = FindOrCreateText(panel.transform, "NicknameLabelText", new Vector2(0f, 18f), new Vector2(520f, 34f), 24f, TextAlignmentOptions.Center);
        nicknameLabel.text = "Nickname";
    }

    private void ResolveReferences()
    {
        if (panel == null)
        {
            panel = FindChildObject("GameOverPanel");
        }

        if (panel == null)
        {
            return;
        }

        summaryText = summaryText != null ? summaryText : FindChildComponent<TMP_Text>(panel.transform, "GameOverSummaryText");
        saveStatusText = saveStatusText != null ? saveStatusText : FindChildComponent<TMP_Text>(panel.transform, "GameOverSaveStatusText");
        nicknameInput = nicknameInput != null ? nicknameInput : FindChildComponent<TMP_InputField>(panel.transform, "NicknameInput");
        saveButton = saveButton != null ? saveButton : FindChildComponent<Button>(panel.transform, "SaveScoreButton");
        mainMenuButton = mainMenuButton != null ? mainMenuButton : FindChildComponent<Button>(panel.transform, "MainMenuButton");
    }

    private void WireButtons()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(SaveScore);
            saveButton.onClick.AddListener(SaveScore);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (nicknameInput != null)
        {
            nicknameInput.onSubmit.RemoveListener(SaveScoreFromInput);
            nicknameInput.onSubmit.AddListener(SaveScoreFromInput);
        }
    }

    private void SaveScoreFromInput(string _)
    {
        SaveScore();
    }

    private GameObject CreatePanel(Transform parent)
    {
        GameObject panelObject = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 560f));

        Image image = panelObject.GetComponent<Image>();
        image.color = new Color(0.06f, 0.08f, 0.11f, 0.96f);
        image.raycastTarget = true;

        return panelObject;
    }

    private TMP_Text FindOrCreateText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        TMP_Text text = FindChildComponent<TMP_Text>(parent, objectName);

        if (text == null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TMP_Text>();
        }

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);

        return text;
    }

    private TMP_InputField FindOrCreateInput(Transform parent)
    {
        TMP_InputField input = FindChildComponent<TMP_InputField>(parent, "NicknameInput");

        if (input != null)
        {
            return input;
        }

        GameObject inputObject = new GameObject("NicknameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(parent, false);
        SetRect(inputObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(360f, 54f));

        Image image = inputObject.GetComponent<Image>();
        image.color = new Color(0.94f, 0.96f, 0.99f, 1f);

        GameObject textAreaObject = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textAreaObject.transform.SetParent(inputObject.transform, false);
        RectTransform textArea = textAreaObject.GetComponent<RectTransform>();
        SetStretchRect(textArea, new Vector2(16f, 6f), new Vector2(-16f, -6f));

        TMP_Text inputText = CreateInputText(textAreaObject.transform, "Text", new Color(0.08f, 0.11f, 0.16f, 1f));
        TMP_Text placeholderText = CreateInputText(textAreaObject.transform, "Placeholder", new Color(0.08f, 0.11f, 0.16f, 0.55f));
        placeholderText.text = "Player";
        placeholderText.fontStyle = FontStyles.Italic;

        input = inputObject.GetComponent<TMP_InputField>();
        input.textViewport = textArea;
        input.textComponent = inputText;
        input.placeholder = placeholderText;
        input.characterLimit = 16;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.selectionColor = new Color(0.25f, 0.55f, 1f, 0.35f);

        return input;
    }

    private TMP_Text CreateInputText(Transform parent, string objectName, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = 26f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = color;
        text.raycastTarget = false;
        SetStretchRect(text.rectTransform, Vector2.zero, Vector2.zero);

        return text;
    }

    private Button FindOrCreateButton(Transform parent, string objectName, string label, Vector2 anchoredPosition, Vector2 size)
    {
        Button button = FindChildComponent<Button>(parent, objectName);

        if (button == null)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            button = buttonObject.GetComponent<Button>();

            GameObject labelObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            SetStretchRect(labelObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        }

        SetRect(button.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);

        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            image = button.gameObject.AddComponent<Image>();
        }

        image.color = new Color(0.94f, 0.96f, 0.99f, 1f);
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.94f, 0.96f, 0.99f, 1f);
        colors.highlightedColor = new Color(0.82f, 0.91f, 1f, 1f);
        colors.pressedColor = new Color(0.64f, 0.78f, 0.95f, 1f);
        colors.disabledColor = new Color(0.55f, 0.59f, 0.64f, 0.65f);
        button.colors = colors;

        SetButtonText(button, label);
        return button;
    }

    private void SetButtonText(Button button, string text)
    {
        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);

        if (buttonText == null)
        {
            return;
        }

        buttonText.text = text;
        buttonText.fontSize = 25f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = new Color(0.08f, 0.11f, 0.16f, 1f);
        buttonText.raycastTarget = false;
    }

    private void FocusNicknameInput()
    {
        if (nicknameInput == null || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(nicknameInput.gameObject);
        nicknameInput.ActivateInputField();
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

    private T FindChildComponent<T>(Transform parent, string objectName) where T : Component
    {
        if (parent == null)
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == objectName)
            {
                return child.GetComponent<T>();
            }
        }

        return null;
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

    private void SetStretchRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }
}
