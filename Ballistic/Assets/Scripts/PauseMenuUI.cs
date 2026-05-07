using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateMissingUI = true;

    private GameManager gameManager;

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

    public void Initialize(GameManager manager)
    {
        gameManager = manager;

        if (autoCreateMissingUI)
        {
            CreateMissingUI();
        }

        ResolveReferences();
        WireButtons();
    }

    public void Show()
    {
        if (panel == null)
        {
            return;
        }

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        if (continueButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void ContinueGame()
    {
        if (gameManager != null)
        {
            gameManager.SetPaused(false);
            return;
        }

        Time.timeScale = 1f;
        Hide();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
            panel = FindChildObject("PausePanel");
        }

        if (panel == null)
        {
            panel = CreateOverlay(canvas.transform);
        }

        Transform content = FindChildTransform(panel.transform, "PauseContent");

        if (content == null)
        {
            content = CreateContent(panel.transform);
        }

        TMP_Text titleText = FindOrCreateText(content, "PauseTitleText", new Vector2(0f, 160f), new Vector2(420f, 70f), 52f, TextAlignmentOptions.Center);
        titleText.text = "PAUSED";
        titleText.fontStyle = FontStyles.Bold;

        continueButton = continueButton != null ? continueButton : FindOrCreateButton(content, "ContinueButton", "CONTINUE", new Vector2(0f, 70f), new Vector2(300f, 58f));
        restartButton = restartButton != null ? restartButton : FindOrCreateButton(content, "RestartButton", "RESTART", new Vector2(0f, -5f), new Vector2(300f, 58f));
        mainMenuButton = mainMenuButton != null ? mainMenuButton : FindOrCreateButton(content, "MainMenuButton", "MAIN MENU", new Vector2(0f, -80f), new Vector2(300f, 58f));
        quitButton = quitButton != null ? quitButton : FindOrCreateButton(content, "QuitButton", "QUIT", new Vector2(0f, -155f), new Vector2(300f, 58f));
    }

    private void ResolveReferences()
    {
        if (panel == null)
        {
            panel = FindChildObject("PausePanel");
        }

        if (panel == null)
        {
            return;
        }

        continueButton = continueButton != null ? continueButton : FindChildComponent<Button>(panel.transform, "ContinueButton");
        restartButton = restartButton != null ? restartButton : FindChildComponent<Button>(panel.transform, "RestartButton");
        mainMenuButton = mainMenuButton != null ? mainMenuButton : FindChildComponent<Button>(panel.transform, "MainMenuButton");
        quitButton = quitButton != null ? quitButton : FindChildComponent<Button>(panel.transform, "QuitButton");
    }

    private void WireButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ContinueGame);
            continueButton.onClick.AddListener(ContinueGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private GameObject CreateOverlay(Transform parent)
    {
        GameObject overlayObject = new GameObject("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(parent, false);

        RectTransform rectTransform = overlayObject.GetComponent<RectTransform>();
        SetStretchRect(rectTransform, Vector2.zero, Vector2.zero);

        Image image = overlayObject.GetComponent<Image>();
        image.color = new Color(0.02f, 0.025f, 0.03f, 0.72f);
        image.raycastTarget = true;

        return overlayObject;
    }

    private Transform CreateContent(Transform parent)
    {
        GameObject contentObject = new GameObject("PauseContent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        contentObject.transform.SetParent(parent, false);

        RectTransform rectTransform = contentObject.GetComponent<RectTransform>();
        SetRect(rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 450f));

        Image image = contentObject.GetComponent<Image>();
        image.color = new Color(0.06f, 0.08f, 0.11f, 0.96f);
        image.raycastTarget = true;

        return contentObject.transform;
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

    private Button FindOrCreateButton(Transform parent, string objectName, string label, Vector2 anchoredPosition, Vector2 size)
    {
        Button button = FindChildComponent<Button>(parent, objectName);

        if (button == null)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            button = buttonObject.GetComponent<Button>();
        }

        TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);

        if (labelText == null)
        {
            GameObject labelObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(button.transform, false);
            labelText = labelObject.GetComponent<TMP_Text>();
        }

        SetRect(button.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, size);
        SetStretchRect(labelText.rectTransform, Vector2.zero, Vector2.zero);

        Image image = button.GetComponent<Image>();

        if (image == null)
        {
            image = button.gameObject.AddComponent<Image>();
        }

        image.color = new Color(0.94f, 0.96f, 0.99f, 1f);
        image.raycastTarget = true;
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

    private Transform FindChildTransform(Transform parent, string objectName)
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
                return child;
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
