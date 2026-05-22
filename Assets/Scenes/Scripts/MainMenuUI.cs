using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private const string SkipMainMenuOnceKey = "CircuitPathSkipMainMenuOnce";
    private static bool skipMainMenuOnNextLoad;

    [Header("Main Menu")]
    [SerializeField] private string gameTitle = "CIRCUIT RUNNER";
    [SerializeField] private string backgroundResourceName = "MainMenuBackground";
    [SerializeField] private Color backgroundColor = new Color(0.005f, 0.035f, 0.035f, 1f);
    [SerializeField] private Color neonGreenColor = new Color(0.55f, 1f, 0.18f, 1f);
    [SerializeField] private Color cyanColor = new Color(0.08f, 0.95f, 1f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.02f, 0.18f, 0.16f, 0.18f);

    public static bool HasStartedFromMainMenu { get; private set; }

    private CanvasGroup canvasGroup;
    private RectTransform titleRect;
    private RectTransform startButtonRect;
    private RectTransform exitButtonRect;
    private bool isClosing;
    private bool usesArtworkBackground;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetMainMenuState()
    {
        ResetToMainMenuState();
    }

    public static void ResetToMainMenuState()
    {
        bool skipMainMenu = skipMainMenuOnNextLoad || PlayerPrefs.GetInt(SkipMainMenuOnceKey, 0) == 1;
        HasStartedFromMainMenu = skipMainMenu;
        skipMainMenuOnNextLoad = false;

        if (skipMainMenu)
        {
            PlayerPrefs.DeleteKey(SkipMainMenuOnceKey);
            PlayerPrefs.Save();
        }

        if (!skipMainMenu)
        {
            CharacterSelectionManager.ClearSessionSelection();
        }

        Time.timeScale = 1f;
    }

    public static void ForceShowMainMenuOnNextLoad()
    {
        skipMainMenuOnNextLoad = false;
        PlayerPrefs.DeleteKey(SkipMainMenuOnceKey);
        PlayerPrefs.Save();
        HasStartedFromMainMenu = false;
        CharacterSelectionManager.ClearSessionSelection();
        Time.timeScale = 1f;
    }

    public static void SkipMainMenuOnNextSceneLoad()
    {
        skipMainMenuOnNextLoad = true;
        PlayerPrefs.SetInt(SkipMainMenuOnceKey, 1);
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForLoadedScene()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SpawnMainMenu();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnMainMenu();
    }

    private static void SpawnMainMenu()
    {
        if (HasStartedFromMainMenu)
        {
            return;
        }

        if (FindObjectOfType<MainMenuUI>() != null)
        {
            return;
        }

        GameObject menuObject = new GameObject(nameof(MainMenuUI));
        menuObject.AddComponent<MainMenuUI>();
    }

    private void Awake()
    {
        EnsureEventSystem();
        BuildUI();
        AudioManager.Instance.PlayMenuAmbience();
    }

    private void Start()
    {
        canvasGroup.alpha = 1f;
        AudioManager.Instance.PlayMenuAmbience();
    }

    private void Update()
    {
        if (titleRect != null)
        {
            titleRect.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 2f) * 0.008f);
        }
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("Main Menu Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1800;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        UIResponsiveUtility.ApplyCanvasScaler(scaler);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        CreateBackground(canvasObject.transform);
        CreateTitle(canvasObject.transform);
        CreateButtons(canvasObject.transform);
    }

    private void CreateBackground(Transform parent)
    {
        GameObject overlayObject = new GameObject("Main Menu Solid Background");
        overlayObject.transform.SetParent(parent, false);

        RectTransform overlayRect = overlayObject.AddComponent<RectTransform>();
        StretchToParent(overlayRect);

        Image overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.color = backgroundColor;
        overlayImage.raycastTarget = true;

        Sprite backgroundSprite = LoadBackgroundSprite();
        usesArtworkBackground = backgroundSprite != null;

        if (usesArtworkBackground)
        {
            overlayImage.sprite = backgroundSprite;
            overlayImage.color = Color.white;
            overlayImage.preserveAspect = false;
            return;
        }

        for (int i = 0; i < 7; i++)
        {
            GameObject lineObject = new GameObject("Main Menu Neon Line " + i);
            lineObject.transform.SetParent(parent, false);

            RectTransform lineRect = lineObject.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 0.5f);
            lineRect.anchorMax = new Vector2(1f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = new Vector2(0f, -420f + i * 140f);
            lineRect.sizeDelta = new Vector2(0f, 2f);

            Image lineImage = lineObject.AddComponent<Image>();
            lineImage.color = i % 2 == 0 ? new Color(0.1f, 1f, 0.55f, 0.16f) : new Color(0.1f, 0.9f, 1f, 0.12f);
            lineImage.raycastTarget = false;
        }
    }

    private Sprite LoadBackgroundSprite()
    {
        Sprite sprite = Resources.Load<Sprite>(backgroundResourceName);

        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(backgroundResourceName);

        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void CreateTitle(Transform parent)
    {
        if (usesArtworkBackground)
        {
            return;
        }

        Text title = CreateText(parent, gameTitle, 96, TextAnchor.MiddleCenter, neonGreenColor);
        titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -165f);
        titleRect.sizeDelta = new Vector2(1200f, 130f);

        Shadow glow = title.gameObject.AddComponent<Shadow>();
        glow.effectColor = new Color(0.45f, 1f, 0.12f, 0.86f);
        glow.effectDistance = new Vector2(0f, -6f);
    }

    private void CreateButtons(Transform parent)
    {
        if (usesArtworkBackground)
        {
            startButtonRect = CreateMenuButton(parent, "START", new Vector2(0f, -108f), StartCharacterSelection);
            exitButtonRect = CreateMenuButton(parent, "EXIT", new Vector2(0f, -248f), ExitGame);
            return;
        }

        startButtonRect = CreateMenuButton(parent, "START", new Vector2(0f, -60f), StartCharacterSelection);
        exitButtonRect = CreateMenuButton(parent, "EXIT", new Vector2(0f, -170f), ExitGame);
    }

    private RectTransform CreateMenuButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = usesArtworkBackground ? new Vector2(700f, 150f) : new Vector2(420f, 86f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = usesArtworkBackground ? Color.clear : buttonColor;

        if (!usesArtworkBackground)
        {
            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = label == "START" ? neonGreenColor : cyanColor;
            outline.effectDistance = new Vector2(4f, -4f);
        }

        Button button = buttonObject.AddComponent<Button>();
        button.transition = usesArtworkBackground ? Selectable.Transition.None : Selectable.Transition.ColorTint;
        button.targetGraphic = buttonImage;
        if (!usesArtworkBackground)
        {
            button.colors = BuildButtonColors();
        }
        button.onClick.AddListener(action);

        if (!usesArtworkBackground)
        {
            Text buttonText = CreateText(buttonObject.transform, label, 42, TextAnchor.MiddleCenter, label == "START" ? neonGreenColor : cyanColor);
            RectTransform textRect = buttonText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        return buttonRect;
    }

    private void StartCharacterSelection()
    {
        if (isClosing)
        {
            return;
        }

        StartCoroutine(OpenCharacterSelection());
    }

    private IEnumerator OpenCharacterSelection()
    {
        isClosing = true;
        AudioManager.Instance.StopMainMenuMusic();
        HasStartedFromMainMenu = true;
        canvasGroup.blocksRaycasts = false;
        yield return FadeCanvas(1f, 0f, 0.28f);
        CharacterSelectionUI.ShowSelectionUI();
        Destroy(gameObject);
    }

    private void ExitGame()
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
        canvasGroup.blocksRaycasts = false;
        AudioManager.Instance.StopAllGameplayAudio();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private ColorBlock BuildButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.55f, 1f, 0.65f, 1f);
        colors.pressedColor = new Color(0.35f, 1f, 0.25f, 1f);
        colors.selectedColor = new Color(0.42f, 1f, 0.55f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
        colors.colorMultiplier = 0.75f;
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
