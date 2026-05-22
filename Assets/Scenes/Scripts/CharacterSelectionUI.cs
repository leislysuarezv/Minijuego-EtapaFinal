using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    private const int CharacterCount = 2;

    [Header("Character Data")]
    [SerializeField] private string[] characterNames = { "Alya", "Bob" };

    [Header("Neon Theme")]
    [SerializeField] private Color overlayColor = new Color(0.01f, 0.04f, 0.04f, 0.88f);
    [SerializeField] private Color panelColor = new Color(0.02f, 0.13f, 0.13f, 0.82f);
    [SerializeField] private Color cyanColor = new Color(0.08f, 0.95f, 1f, 1f);
    [SerializeField] private Color neonGreenColor = new Color(0.55f, 1f, 0.18f, 1f);
    [SerializeField] private Color dimColor = new Color(1f, 1f, 1f, 0.48f);

    [Header("Animation")]
    [SerializeField] private float cardSelectedScale = 1.06f;
    [SerializeField] private float cardHoverScale = 1.035f;
    [SerializeField] private float cardFloatAmount = 8f;
    [SerializeField] private float startButtonPulseAmount = 0.045f;

    private readonly bool[] hoveredCards = new bool[CharacterCount];
    private readonly bool[] hoveredSelectButtons = new bool[CharacterCount];
    private readonly RectTransform[] cardRects = new RectTransform[CharacterCount];
    private readonly CanvasGroup[] cardGroups = new CanvasGroup[CharacterCount];
    private readonly Image[] cardImages = new Image[CharacterCount];
    private readonly Outline[] cardOutlines = new Outline[CharacterCount];
    private readonly Button[] selectButtons = new Button[CharacterCount];
    private readonly Image[] selectButtonImages = new Image[CharacterCount];
    private readonly RectTransform[] selectButtonRects = new RectTransform[CharacterCount];
    private readonly RectTransform[] scanlines = new RectTransform[6];
    private readonly RectTransform[] particles = new RectTransform[28];

    private CanvasGroup canvasGroup;
    private RectTransform titleRect;
    private Text titleText;
    private Button startButton;
    private Image startButtonImage;
    private RectTransform startButtonRect;
    private int selectedIndex = -1;
    private bool isStartingGame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SpawnSelectionUI()
    {
        if (!MainMenuUI.HasStartedFromMainMenu)
        {
            return;
        }

        if (CharacterSelectionManager.HasSessionSelection)
        {
            return;
        }

        ShowSelectionUI();
    }

    public static void ShowSelectionUI()
    {
        if (FindObjectOfType<CharacterSelectionUI>() != null)
        {
            return;
        }

        GameObject selectionObject = new GameObject(nameof(CharacterSelectionUI));
        selectionObject.AddComponent<CharacterSelectionUI>();
    }

    private void Awake()
    {
        EnsureEventSystem();
        CacheCharacterPreviews();
        BuildUI();
    }

    private void Start()
    {
        StartCoroutine(PlayIntroAnimation());
    }

    private void Update()
    {
        HandleKeyboardNavigation();
        AnimateScanlines();
        AnimateParticles();
        AnimateCards();
        AnimateTitleFlicker();
        AnimateStartButton();
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("Character Selection Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        UIResponsiveUtility.ApplyCanvasScaler(scaler);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 0f;

        CreateBackground(canvasObject.transform);
        CreateScanlines(canvasObject.transform);
        CreateParticles(canvasObject.transform);
        CreateTitle(canvasObject.transform);
        CreateCharacterCards(canvasObject.transform);
        CreateStartButton(canvasObject.transform);
        UpdateSelectionVisuals();
    }

    private void CreateBackground(Transform parent)
    {
        GameObject backgroundObject = new GameObject("Dark Sci-Fi Overlay");
        backgroundObject.transform.SetParent(parent, false);

        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        StretchToParent(backgroundRect);

        Image backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.color = overlayColor;
        backgroundImage.raycastTarget = true;

        GameObject gridObject = new GameObject("Digital Grid Tint");
        gridObject.transform.SetParent(parent, false);

        RectTransform gridRect = gridObject.AddComponent<RectTransform>();
        StretchToParent(gridRect);

        Image gridImage = gridObject.AddComponent<Image>();
        gridImage.color = new Color(0.02f, 0.55f, 0.42f, 0.08f);
        gridImage.raycastTarget = false;
    }

    private void CreateScanlines(Transform parent)
    {
        for (int i = 0; i < scanlines.Length; i++)
        {
            GameObject lineObject = new GameObject("Animated Scanline " + i);
            lineObject.transform.SetParent(parent, false);

            RectTransform lineRect = lineObject.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 0.5f);
            lineRect.anchorMax = new Vector2(1f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = new Vector2(0f, -520f + i * 190f);
            lineRect.sizeDelta = new Vector2(0f, 3f);

            Image lineImage = lineObject.AddComponent<Image>();
            lineImage.color = i % 2 == 0 ? new Color(0.1f, 1f, 0.55f, 0.13f) : new Color(0.1f, 0.9f, 1f, 0.1f);
            lineImage.raycastTarget = false;
            scanlines[i] = lineRect;
        }
    }

    private void CreateParticles(Transform parent)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            GameObject particleObject = new GameObject("Digital Particle " + i);
            particleObject.transform.SetParent(parent, false);

            RectTransform particleRect = particleObject.AddComponent<RectTransform>();
            particleRect.anchorMin = new Vector2(0.5f, 0.5f);
            particleRect.anchorMax = new Vector2(0.5f, 0.5f);
            particleRect.pivot = new Vector2(0.5f, 0.5f);
            particleRect.anchoredPosition = new Vector2(Random.Range(-890f, 890f), Random.Range(-470f, 470f));
            particleRect.sizeDelta = Vector2.one * Random.Range(3f, 7f);

            Image particleImage = particleObject.AddComponent<Image>();
            particleImage.color = Random.value > 0.5f ? new Color(0.4f, 1f, 0.35f, 0.32f) : new Color(0.2f, 0.95f, 1f, 0.28f);
            particleImage.raycastTarget = false;
            particles[i] = particleRect;
        }
    }

    private void CreateTitle(Transform parent)
    {
        titleText = CreateText(parent, "CHOOSE YOUR RUNNER", 76, TextAnchor.MiddleCenter, neonGreenColor);
        titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -54f);
        titleRect.sizeDelta = new Vector2(1060f, 110f);

        Shadow titleGlow = titleText.gameObject.AddComponent<Shadow>();
        titleGlow.effectColor = new Color(0.45f, 1f, 0.12f, 0.82f);
        titleGlow.effectDistance = new Vector2(0f, -5f);
    }

    private void CreateCharacterCards(Transform parent)
    {
        for (int i = 0; i < CharacterCount; i++)
        {
            int cardIndex = i;
            GameObject cardObject = new GameObject(characterNames[i] + " Holographic Card");
            cardObject.transform.SetParent(parent, false);

            RectTransform cardRect = cardObject.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(i == 0 ? -390f : 390f, 5f);
            cardRect.sizeDelta = new Vector2(470f, 610f);
            cardRects[i] = cardRect;

            CanvasGroup group = cardObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            cardGroups[i] = group;

            Image cardImage = cardObject.AddComponent<Image>();
            cardImage.color = panelColor;
            cardImages[i] = cardImage;

            Outline outline = cardObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(6f, -6f);
            outline.effectColor = new Color(0.12f, 1f, 0.9f, 0.72f);
            cardOutlines[i] = outline;

            Shadow shadow = cardObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 1f, 0.75f, 0.32f);
            shadow.effectDistance = new Vector2(0f, -9f);

            Button cardButton = cardObject.AddComponent<Button>();
            cardButton.transition = Selectable.Transition.None;
            cardButton.onClick.AddListener(() => SelectCharacter(cardIndex));
            AddHoverEvents(cardObject, () => hoveredCards[cardIndex] = true, () => hoveredCards[cardIndex] = false);

            CreateCharacterPreview(cardObject.transform, i);
            CreateCharacterName(cardObject.transform, characterNames[i]);
            selectButtons[i] = CreateSelectButton(cardObject.transform, cardIndex);
        }
    }

    private void CreateCharacterPreview(Transform parent, int characterIndex)
    {
        GameObject previewObject = new GameObject("Character Preview");
        previewObject.transform.SetParent(parent, false);

        RectTransform previewRect = previewObject.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 1f);
        previewRect.anchorMax = new Vector2(0.5f, 1f);
        previewRect.pivot = new Vector2(0.5f, 1f);
        previewRect.anchoredPosition = new Vector2(0f, -92f);
        previewRect.sizeDelta = new Vector2(290f, 270f);

        Image previewImage = previewObject.AddComponent<Image>();
        previewImage.sprite = GetPreviewSprite(characterIndex);
        previewImage.preserveAspect = true;
        previewImage.raycastTarget = false;
    }

    private void CreateCharacterName(Transform parent, string characterName)
    {
        Text nameText = CreateText(parent, characterName.ToUpperInvariant(), 48, TextAnchor.MiddleCenter, Color.white);
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameRect.pivot = new Vector2(0.5f, 0.5f);
        nameRect.anchoredPosition = new Vector2(0f, -84f);
        nameRect.sizeDelta = new Vector2(360f, 70f);
    }

    private Button CreateSelectButton(Transform parent, int characterIndex)
    {
        GameObject buttonObject = new GameObject("SELECT Button");
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 68f);
        buttonRect.sizeDelta = new Vector2(250f, 68f);
        selectButtonRects[characterIndex] = buttonRect;

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.02f, 0.18f, 0.16f, 0.96f);
        selectButtonImages[characterIndex] = buttonImage;

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = cyanColor;
        outline.effectDistance = new Vector2(3f, -3f);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() =>
        {
            SelectCharacter(characterIndex);
            StartCoroutine(FlashButton(buttonImage));
        });
        AddHoverEvents(buttonObject, () => hoveredSelectButtons[characterIndex] = true, () => hoveredSelectButtons[characterIndex] = false);

        Text buttonText = CreateText(buttonObject.transform, "SELECT", 32, TextAnchor.MiddleCenter, neonGreenColor);
        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private void CreateStartButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("START Button");
        buttonObject.transform.SetParent(parent, false);

        startButtonRect = buttonObject.AddComponent<RectTransform>();
        startButtonRect.anchorMin = new Vector2(0.5f, 0f);
        startButtonRect.anchorMax = new Vector2(0.5f, 0f);
        startButtonRect.pivot = new Vector2(0.5f, 0f);
        startButtonRect.anchoredPosition = new Vector2(0f, 54f);
        startButtonRect.sizeDelta = new Vector2(430f, 86f);

        startButtonImage = buttonObject.AddComponent<Image>();
        startButtonImage.color = new Color(0.04f, 0.22f, 0.13f, 0.55f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 1f, 0.15f, 0.38f);
        outline.effectDistance = new Vector2(4f, -4f);

        startButton = buttonObject.AddComponent<Button>();
        startButton.transition = Selectable.Transition.None;
        startButton.interactable = false;
        startButton.onClick.AddListener(() =>
        {
            if (!isStartingGame)
            {
                StartCoroutine(StartGameAfterTransition());
            }
        });

        Text buttonText = CreateText(buttonObject.transform, "START", 42, TextAnchor.MiddleCenter, Color.black);
        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
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

    private Sprite GetPreviewSprite(int characterIndex)
    {
        Sprite cachedSprite = CharacterSelectionManager.GetPreviewSprite(characterIndex);

        if (cachedSprite != null)
        {
            return cachedSprite;
        }

        return characterIndex == 1 ? Resources.Load<Sprite>("p1") : null;
    }

    private void CacheCharacterPreviews()
    {
        PlayerFollowMouse playerMovement = FindObjectOfType<PlayerFollowMouse>();
        SpriteRenderer renderer = playerMovement != null ? playerMovement.GetComponent<SpriteRenderer>() : null;

        if (renderer != null)
        {
            CharacterSelectionManager.SetPreviewSprite(0, renderer.sprite);
        }

        CharacterSelectionManager.SetPreviewSprite(1, Resources.Load<Sprite>("p1"));
    }

    private void SelectCharacter(int characterIndex)
    {
        selectedIndex = Mathf.Clamp(characterIndex, 0, CharacterCount - 1);
        startButton.interactable = true;
        UpdateSelectionVisuals();

        if (selectButtons[selectedIndex] != null)
        {
            EventSystem.current.SetSelectedGameObject(selectButtons[selectedIndex].gameObject);
        }
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < CharacterCount; i++)
        {
            bool isSelected = i == selectedIndex;

            if (cardImages[i] != null)
            {
                cardImages[i].color = isSelected ? new Color(0.04f, 0.2f, 0.16f, 0.96f) : panelColor;
            }

            if (cardOutlines[i] != null)
            {
                cardOutlines[i].effectColor = isSelected ? neonGreenColor : new Color(0.12f, 1f, 0.9f, 0.55f);
            }

            if (cardGroups[i] != null)
            {
                cardGroups[i].alpha = isSelected || selectedIndex < 0 ? 1f : dimColor.a;
            }

        }

        if (startButtonImage != null)
        {
            startButtonImage.color = selectedIndex >= 0 ? neonGreenColor : new Color(0.04f, 0.22f, 0.13f, 0.55f);
        }
    }

    private void HandleKeyboardNavigation()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            SelectCharacter(selectedIndex <= 0 ? 0 : selectedIndex - 1);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            SelectCharacter(selectedIndex < 0 ? 0 : Mathf.Min(CharacterCount - 1, selectedIndex + 1));
        }

        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) && selectedIndex >= 0 && !isStartingGame)
        {
            StartCoroutine(StartGameAfterTransition());
        }
    }

    private IEnumerator PlayIntroAnimation()
    {
        float elapsed = 0f;
        float duration = 0.55f;
        Vector2[] cardTargets = new Vector2[CharacterCount];

        for (int i = 0; i < CharacterCount; i++)
        {
            cardTargets[i] = cardRects[i].anchoredPosition;
            cardRects[i].anchoredPosition += Vector2.down * 60f;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutQuad(t);
            canvasGroup.alpha = eased;

            for (int i = 0; i < CharacterCount; i++)
            {
                cardGroups[i].alpha = eased;
                cardRects[i].anchoredPosition = Vector2.Lerp(cardTargets[i] + Vector2.down * 60f, cardTargets[i], eased);
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;
        UpdateSelectionVisuals();
    }

    private void AnimateCards()
    {
        for (int i = 0; i < CharacterCount; i++)
        {
            if (cardRects[i] == null)
            {
                continue;
            }

            bool isSelected = i == selectedIndex;
            float pulse = isSelected ? Mathf.Sin(Time.time * 5.8f) * 0.018f : 0f;
            float hover = hoveredCards[i] ? cardHoverScale - 1f : 0f;
            float targetScale = 1f + hover + (isSelected ? cardSelectedScale - 1f : 0f) + pulse;
            float floatOffset = Mathf.Sin(Time.time * 1.4f + i * 1.5f) * cardFloatAmount;
            Vector2 basePosition = new Vector2(i == 0 ? -390f : 390f, 5f);

            cardRects[i].localScale = Vector3.Lerp(cardRects[i].localScale, Vector3.one * targetScale, Time.deltaTime * 9f);
            cardRects[i].anchoredPosition = Vector2.Lerp(cardRects[i].anchoredPosition, basePosition + Vector2.up * floatOffset, Time.deltaTime * 3f);
        }

        for (int i = 0; i < CharacterCount; i++)
        {
            if (selectButtonRects[i] != null)
            {
                float scale = hoveredSelectButtons[i] ? 1.06f : 1f;
                selectButtonRects[i].localScale = Vector3.Lerp(selectButtonRects[i].localScale, Vector3.one * scale, Time.deltaTime * 10f);
            }
        }
    }

    private void AnimateStartButton()
    {
        if (startButtonRect == null)
        {
            return;
        }

        float pulse = selectedIndex >= 0 ? Mathf.Sin(Time.time * 4.5f) * startButtonPulseAmount : 0f;
        startButtonRect.localScale = Vector3.one * (1f + pulse);
    }

    private void AnimateTitleFlicker()
    {
        if (titleText == null)
        {
            return;
        }

        float flicker = 0.86f + Mathf.PerlinNoise(Time.time * 4.2f, 1.7f) * 0.14f;
        Color titleColor = neonGreenColor;
        titleColor.a = flicker;
        titleText.color = titleColor;
        titleRect.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 2f) * 0.006f);
    }

    private void AnimateScanlines()
    {
        for (int i = 0; i < scanlines.Length; i++)
        {
            if (scanlines[i] == null)
            {
                continue;
            }

            Vector2 position = scanlines[i].anchoredPosition;
            position.y += Time.deltaTime * (42f + i * 8f);

            if (position.y > 560f)
            {
                position.y = -560f;
            }

            scanlines[i].anchoredPosition = position;
        }
    }

    private void AnimateParticles()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
            {
                continue;
            }

            Vector2 position = particles[i].anchoredPosition;
            position.y += Time.deltaTime * (8f + i % 5);
            position.x += Mathf.Sin(Time.time + i) * Time.deltaTime * 4f;

            if (position.y > 520f)
            {
                position.y = -520f;
            }

            particles[i].anchoredPosition = position;
        }
    }

    private IEnumerator FlashButton(Image buttonImage)
    {
        if (buttonImage == null)
        {
            yield break;
        }

        Color originalColor = buttonImage.color;
        buttonImage.color = neonGreenColor;
        yield return new WaitForSeconds(0.08f);
        buttonImage.color = originalColor;
    }

    private IEnumerator StartGameAfterTransition()
    {
        isStartingGame = true;
        CharacterSelectionManager.SaveSelection(selectedIndex);
        SelectedCharacterSpawner.ApplySelectedCharacterToCurrentScene();
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        float duration = 0.32f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = 1f - t;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void AddHoverEvents(GameObject target, System.Action onEnter, System.Action onExit)
    {
        EventTrigger trigger = target.AddComponent<EventTrigger>();
        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => onEnter());
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => onExit());
        trigger.triggers.Add(exitEntry);
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

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
