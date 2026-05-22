using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField] private Color overlayColor = new Color(0.01f, 0.03f, 0.04f, 0.78f);
    [SerializeField] private Color panelColor = new Color(0.02f, 0.13f, 0.13f, 0.94f);
    [SerializeField] private Color neonGreenColor = new Color(0.55f, 1f, 0.18f, 1f);
    [SerializeField] private Color cyanColor = new Color(0.08f, 0.95f, 1f, 1f);

    private CanvasGroup canvasGroup;
    private GameObject firstSelectedButton;
    private bool isPaused;

    public static bool IsPaused { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForLoadedScene()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SpawnPauseMenu();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnPauseMenu();
    }

    private static void SpawnPauseMenu()
    {
        IsPaused = false;

        if (FindObjectOfType<PauseMenuUI>() != null)
        {
            return;
        }

        GameObject pauseObject = new GameObject(nameof(PauseMenuUI));
        pauseObject.AddComponent<PauseMenuUI>();
    }

    private void Awake()
    {
        EnsureEventSystem();
        BuildUI();
        SetVisible(false);
    }

    private void Update()
    {
        if (!CanUsePauseMenu())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private bool CanUsePauseMenu()
    {
        return MainMenuUI.HasStartedFromMainMenu && CharacterSelectionManager.HasSessionSelection && StartIntroAnimator.GameStarted && ScoreManager.CurrentPhase == ScoreManager.GamePhase.Painting;
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("Pause Menu Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        UIResponsiveUtility.ApplyCanvasScaler(scaler);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        CreateOverlay(canvasObject.transform);
        CreatePanel(canvasObject.transform);
    }

    private void CreateOverlay(Transform parent)
    {
        GameObject overlayObject = new GameObject("Pause Dark Overlay");
        overlayObject.transform.SetParent(parent, false);

        RectTransform overlayRect = overlayObject.AddComponent<RectTransform>();
        StretchToParent(overlayRect);

        Image overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.color = overlayColor;
        overlayImage.raycastTarget = true;
    }

    private void CreatePanel(Transform parent)
    {
        GameObject panelObject = new GameObject("Pause Menu Panel");
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(560f, 430f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = panelColor;

        Outline panelOutline = panelObject.AddComponent<Outline>();
        panelOutline.effectColor = cyanColor;
        panelOutline.effectDistance = new Vector2(5f, -5f);

        Text title = CreateText(panelObject.transform, "PAUSED", 64, TextAnchor.MiddleCenter, neonGreenColor);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -44f);
        titleRect.sizeDelta = new Vector2(480f, 88f);

        RectTransform resumeButton = CreatePauseButton(panelObject.transform, "RESUME", new Vector2(0f, -34f), ResumeGame);
        firstSelectedButton = resumeButton.gameObject;
        CreatePauseButton(panelObject.transform, "MAIN MENU", new Vector2(0f, -142f), ReturnToMainMenu);
    }

    private RectTransform CreatePauseButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(360f, 76f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.02f, 0.18f, 0.16f, 0.96f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = cyanColor;
        outline.effectDistance = new Vector2(3f, -3f);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = buttonImage;
        button.colors = BuildButtonColors();
        button.onClick.AddListener(action);

        Text buttonText = CreateText(buttonObject.transform, label, 34, TextAnchor.MiddleCenter, neonGreenColor);
        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonRect;
    }

    private void PauseGame()
    {
        isPaused = true;
        IsPaused = true;
        CursorInputRouter.Instance.ForceRelease();
        Time.timeScale = 0f;
        SetVisible(true);

        if (EventSystem.current != null && firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    private void ResumeGame()
    {
        isPaused = false;
        IsPaused = false;
        Time.timeScale = 1f;
        SetVisible(false);
        CursorInputRouter.Instance.ForceRelease();
    }

    private void ReturnToMainMenu()
    {
        isPaused = false;
        IsPaused = false;
        Time.timeScale = 1f;
        CursorInputRouter.Instance.ForceRelease();
        AudioManager.Instance.StopAllGameplayAudio();
        MainMenuUI.ForceShowMainMenuOnNextLoad();
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }

    private ColorBlock BuildButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.65f, 1f, 0.28f, 1f);
        colors.pressedColor = neonGreenColor;
        colors.selectedColor = new Color(0.42f, 1f, 0.18f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
        colors.colorMultiplier = 1f;
        return colors;
    }

    private Text CreateText(Transform parent, string text, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(text + " Text");
        textObject.transform.SetParent(parent, false);

        Text uiText = textObject.AddComponent<Text>();
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.fontStyle = FontStyle.Bold;
        uiText.alignment = alignment;
        uiText.color = color;
        uiText.raycastTarget = false;
        UIResponsiveUtility.ConfigureText(uiText, fontSize);
        return uiText;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
