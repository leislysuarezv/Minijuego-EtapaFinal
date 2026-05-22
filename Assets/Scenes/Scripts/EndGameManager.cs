using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameManager : MonoBehaviour
{
    public struct CharacterResult
    {
        public string CharacterName;
        public bool IsPlayer;
        public int Score;
        public int Distance;
        public float Time;
        public int Energy;
        public string Rank;
    }

    public struct EndGameResults
    {
        public string PlayerCharacterName;
        public string AICharacterName;
        public int PlayerScore;
        public int AIScore;
        public int PlayerDistance;
        public int AIDistance;
        public float PlayerTime;
        public float AITime;
        public int PlayerEnergy;
        public int AIEnergy;
        public string PlayerRank;
        public string AIRank;
        public CharacterResult[] CharacterResults;
    }

    [Header("End Screen Theme")]
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.995f);
    [SerializeField] private Color victoryColor = new Color(0.55f, 1f, 0.18f, 1f);
    [SerializeField] private Color defeatColor = new Color(1f, 0.16f, 0.12f, 1f);
    [SerializeField] private Color cyanColor = new Color(0.08f, 0.95f, 1f, 1f);
    [SerializeField] private Color cardColor = new Color(0.018f, 0.085f, 0.09f, 0.94f);

    [Header("Animation")]
    [SerializeField] private float panelFadeDuration = 0.55f;

    private static EndGameManager activeInstance;

    private EndGameResults results;
    private CharacterResult[] rankedResults;
    private bool playerWon;
    private bool isInitialized;
    private bool isTransitioning;
    private Camera mainCameraCache;
    private Color previousCameraBackgroundColor;
    private CameraClearFlags previousCameraClearFlags;
    private CanvasGroup rootGroup;
    private CanvasGroup messageGroup;
    private CanvasGroup comparisonGroup;
    private RectTransform messageRect;
    private RectTransform comparisonPanelRect;
    private Text resultMessageText;
    private Image flashImage;
    private Image fadeImage;
    private RectTransform[] scanlines;
    private RectTransform[] particles;
    private readonly List<DebugTarget> debugTargets = new List<DebugTarget>();
    private Text debugNameText;
    private Text debugPositionText;
    private int selectedDebugTargetIndex;

    public static void ShowResults(EndGameResults endGameResults)
    {
        if (activeInstance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(EndGameManager));
        activeInstance = managerObject.AddComponent<EndGameManager>();
        activeInstance.results = endGameResults;
        activeInstance.Initialize();
    }

    private void Awake()
    {
        activeInstance = this;
    }

    private void OnDestroy()
    {
        RestoreGameplayCameraBackground();

        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        EnsureFallbackStats();
        rankedResults = BuildRankedResults();
        playerWon = rankedResults.Length == 0 || rankedResults[0].IsPlayer;
        HideGameplayCameraBackground();
        EnsureEventSystem();
        BuildUI();
        StartCoroutine(PlayEndSequence());
    }

    private void Update()
    {
        AnimateScanlines();
        AnimateParticles();
        AnimateMessageGlitch();

        if (comparisonPanelRect != null && comparisonGroup != null && comparisonGroup.alpha > 0.01f)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * 2.2f) * 0.005f;
            comparisonPanelRect.localScale = Vector3.one * (1f + pulse);
        }
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("End Game Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2400;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        UIResponsiveUtility.ApplyCanvasScaler(scaler);

        canvasObject.AddComponent<GraphicRaycaster>();
        rootGroup = canvasObject.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;
        rootGroup.blocksRaycasts = true;

        CreateBackground(canvasObject.transform);
        CreateScanlines(canvasObject.transform);
        CreateParticles(canvasObject.transform);
        CreateResultMessage(canvasObject.transform);
        CreateComparisonPanel(canvasObject.transform);
        CreateFadeLayer(canvasObject.transform);
    }

    private void CreateBackground(Transform parent)
    {
        Image overlayImage = CreateFullScreenImage(parent, "Completion Dark Overlay", overlayColor);
        overlayImage.raycastTarget = true;

        Image blackoutImage = CreateFullScreenImage(parent, "Completion Background Blackout", new Color(0f, 0f, 0f, 0.82f));
        blackoutImage.raycastTarget = false;

        CreateVignetteEdge(parent, "Top Vignette", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1920f, 220f));
        CreateVignetteEdge(parent, "Bottom Vignette", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1920f, 240f));
        CreateVignetteEdge(parent, "Left Vignette", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(280f, 1080f));
        CreateVignetteEdge(parent, "Right Vignette", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(280f, 1080f));

        Color flashColor = playerWon ? victoryColor : defeatColor;
        flashColor.a = 0f;
        flashImage = CreateFullScreenImage(parent, "Completion Flash", flashColor);
        flashImage.raycastTarget = false;
    }

    private void CreateVignetteEdge(Transform parent, string objectName, Vector2 anchor, Vector2 pivot, Vector2 size)
    {
        GameObject edgeObject = new GameObject(objectName);
        edgeObject.transform.SetParent(parent, false);

        RectTransform edgeRect = edgeObject.AddComponent<RectTransform>();
        edgeRect.anchorMin = anchor;
        edgeRect.anchorMax = anchor;
        edgeRect.pivot = pivot;
        edgeRect.anchoredPosition = Vector2.zero;
        edgeRect.sizeDelta = size;

        Image edgeImage = edgeObject.AddComponent<Image>();
        edgeImage.color = new Color(0f, 0f, 0f, 0.68f);
        edgeImage.raycastTarget = false;
    }

    private void CreateScanlines(Transform parent)
    {
        scanlines = new RectTransform[10];

        for (int i = 0; i < scanlines.Length; i++)
        {
            GameObject lineObject = new GameObject("Result Scanline " + i);
            lineObject.transform.SetParent(parent, false);

            RectTransform lineRect = lineObject.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 0.5f);
            lineRect.anchorMax = new Vector2(1f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = new Vector2(0f, -540f + i * 120f);
            lineRect.sizeDelta = new Vector2(0f, i % 2 == 0 ? 3f : 2f);

            Image lineImage = lineObject.AddComponent<Image>();
            lineImage.color = i % 2 == 0 ? new Color(0.25f, 1f, 0.35f, 0.08f) : new Color(0.08f, 0.95f, 1f, 0.07f);
            lineImage.raycastTarget = false;
            scanlines[i] = lineRect;
        }
    }

    private void CreateParticles(Transform parent)
    {
        particles = new RectTransform[48];

        for (int i = 0; i < particles.Length; i++)
        {
            GameObject particleObject = new GameObject("Result Particle " + i);
            particleObject.transform.SetParent(parent, false);

            RectTransform particleRect = particleObject.AddComponent<RectTransform>();
            particleRect.anchorMin = new Vector2(0.5f, 0.5f);
            particleRect.anchorMax = new Vector2(0.5f, 0.5f);
            particleRect.pivot = new Vector2(0.5f, 0.5f);
            particleRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-930f, 930f), UnityEngine.Random.Range(-505f, 505f));
            particleRect.sizeDelta = Vector2.one * UnityEngine.Random.Range(3f, 8f);

            Image particleImage = particleObject.AddComponent<Image>();
            particleImage.color = playerWon
                ? new Color(UnityEngine.Random.value > 0.45f ? 0.5f : 0.12f, 1f, UnityEngine.Random.value > 0.5f ? 0.28f : 1f, 0.18f)
                : new Color(1f, UnityEngine.Random.Range(0.1f, 0.28f), UnityEngine.Random.Range(0.05f, 0.18f), 0.2f);
            particleImage.raycastTarget = false;
            particles[i] = particleRect;
        }
    }

    private void CreateResultMessage(Transform parent)
    {
        GameObject messageObject = new GameObject("Victory Defeat Message");
        messageObject.transform.SetParent(parent, false);

        messageGroup = messageObject.AddComponent<CanvasGroup>();
        messageGroup.alpha = 0f;

        messageRect = messageObject.AddComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.anchoredPosition = Vector2.zero;
        messageRect.sizeDelta = new Vector2(1200f, 320f);

        string message = playerWon ? "VICTORY" : "DEFEAT";
        Color messageColor = playerWon ? victoryColor : defeatColor;
        resultMessageText = CreateText(messageObject.transform, message, 142, TextAnchor.MiddleCenter, messageColor);
        SetRect(resultMessageText.rectTransform, Vector2.zero, new Vector2(1180f, 210f));

        Shadow glow = resultMessageText.gameObject.AddComponent<Shadow>();
        glow.effectColor = messageColor;
        glow.effectDistance = new Vector2(0f, -9f);

        Text subtitle = CreateText(messageObject.transform, playerWon ? "CIRCUIT STABILIZED" : "SYSTEM OVERRIDDEN", 34, TextAnchor.MiddleCenter, cyanColor);
        SetRect(subtitle.rectTransform, new Vector2(0f, -120f), new Vector2(820f, 58f));
    }

    private void CreateComparisonPanel(Transform parent)
    {
        GameObject panelObject = new GameObject("Run Performance Comparison");
        panelObject.transform.SetParent(parent, false);

        comparisonGroup = panelObject.AddComponent<CanvasGroup>();
        comparisonGroup.alpha = 0f;
        comparisonGroup.interactable = false;
        comparisonGroup.blocksRaycasts = false;

        comparisonPanelRect = panelObject.AddComponent<RectTransform>();
        comparisonPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        comparisonPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        comparisonPanelRect.pivot = new Vector2(0.5f, 0.5f);
        comparisonPanelRect.anchoredPosition = Vector2.zero;
        Vector2 panelSize = UIResponsiveUtility.GetReferenceSafePanelSize(1420f, 780f, 180f, 80f);
        float titleY = panelSize.y * 0.5f - 108f;
        float badgeY = titleY - 86f;
        float cardY = -42f;
        float separatorY = -panelSize.y * 0.5f + 128f;
        float buttonY = -panelSize.y * 0.5f + 105f;
        float maxCardWidth = Mathf.Max(470f, (panelSize.x - 220f) * 0.5f);
        float cardScale = Mathf.Clamp(maxCardWidth / 590f, 0.88f, 1f);
        float cardOffsetX = Mathf.Clamp(panelSize.x * 0.222f, 300f, 350f);
        float buttonOffsetX = Mathf.Min(285f, panelSize.x * 0.2f);

        comparisonPanelRect.sizeDelta = panelSize;
        comparisonPanelRect.localScale = Vector3.one * 0.88f;
        RegisterDebugTarget("MAIN PANEL", comparisonPanelRect);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = Color.clear;

        panelImage.color = new Color(0.018f, 0.004f, 0.008f, 1f);
        comparisonPanelRect.sizeDelta = panelSize;

        Shadow panelShadow = panelObject.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        panelShadow.effectDistance = new Vector2(0f, -10f);

        Outline panelOutline = panelObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(victoryColor.r, victoryColor.g, victoryColor.b, 0.62f);
        panelOutline.effectDistance = new Vector2(3f, -3f);

        CreatePanelLayer(panelObject.transform, "Outer Metallic Depth", Vector2.zero, panelSize - new Vector2(75f, 72f), new Color(0.002f, 0.015f, 0.018f, 0.82f), cyanColor, 1f);
        CreatePanelLayer(panelObject.transform, "Inner Red Holographic Surface", new Vector2(0f, -8f), panelSize - new Vector2(155f, 140f), new Color(0.05f, 0.006f, 0.012f, 0.82f), victoryColor, 1f);
        CreateFrameDetails(panelObject.transform, new Vector2(panelSize.x * 0.5f - 80f, panelSize.y * 0.5f - 60f), victoryColor, cyanColor);

        CharacterResult winnerResult = rankedResults.Length > 0 ? rankedResults[0] : new CharacterResult { CharacterName = "Winner" };
        string winnerTitle = winnerResult.CharacterName.ToUpperInvariant() + " WON";
        int playerPlace = GetPlayerPlace();
        bool playerWonResult = playerPlace == 1;
        string playerPlaceLabel = GetPlaceLabel(playerPlace);
        Color playerPlaceColor = playerWonResult ? victoryColor : defeatColor;

        CreateTitleWings(panelObject.transform, new Vector2(0f, titleY + 18f));

        Text title = CreateText(panelObject.transform, winnerTitle, 64, TextAnchor.MiddleCenter, victoryColor);
        SetRect(title.rectTransform, new Vector2(0f, titleY), new Vector2(Mathf.Min(860f, panelSize.x - 420f), 70f));
        RegisterDebugTarget("WINNER TITLE", title.rectTransform);

        Shadow titleGlow = title.gameObject.AddComponent<Shadow>();
        titleGlow.effectColor = new Color(0.45f, 1f, 0.12f, 0.95f);
        titleGlow.effectDistance = new Vector2(0f, -4f);

        RectTransform badgeRect = CreateResultBadge(panelObject.transform, playerPlaceLabel, new Vector2(0f, badgeY), playerPlaceColor);
        RegisterDebugTarget("WINNER BADGE", badgeRect);

        int visiblePlaces = Mathf.Min(2, rankedResults.Length);

        for (int i = 0; i < visiblePlaces; i++)
        {
            Vector2 position = visiblePlaces == 1 ? new Vector2(0f, cardY) : new Vector2(i == 0 ? -cardOffsetX : cardOffsetX, cardY);
            bool isWinnerCard = i == 0;
            Color accent = isWinnerCard ? victoryColor : defeatColor;
            RectTransform cardRect = CreateRankingCard(panelObject.transform, rankedResults[i], i + 1, position, accent, isWinnerCard);
            cardRect.localScale = Vector3.one * cardScale;
            RegisterDebugTarget((isWinnerCard ? "WINNER CARD" : "LOSER CARD"), cardRect);
        }

        CreateHolographicSeparator(panelObject.transform, new Vector2(0f, separatorY), cyanColor);
        RectTransform restartRect = CreateEndButton(panelObject.transform, "RESTART", new Vector2(-buttonOffsetX, buttonY), RestartRun);
        RectTransform menuRect = CreateEndButton(panelObject.transform, "MAIN MENU", new Vector2(buttonOffsetX, buttonY), ReturnToMainMenu);
        RegisterDebugTarget("RESTART BUTTON", restartRect);
        RegisterDebugTarget("MAIN MENU BUTTON", menuRect);
    }

    private RectTransform CreateResultBadge(Transform parent, string label, Vector2 position, Color accentColor)
    {
        GameObject badgeObject = new GameObject("Result Badge");
        badgeObject.transform.SetParent(parent, false);

        RectTransform badgeRect = badgeObject.AddComponent<RectTransform>();
        SetRect(badgeRect, position, new Vector2(320f, 64f));

        Image badgeImage = badgeObject.AddComponent<Image>();
        badgeImage.color = new Color(0.08f, 0.012f, 0.018f, 0.96f);
        badgeImage.raycastTarget = false;

        Outline outline = badgeObject.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);

        Shadow shadow = badgeObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.28f);
        shadow.effectDistance = new Vector2(0f, -5f);

        CreateCircuitAccent(badgeObject.transform, new Vector2(-132f, 0f), new Vector2(38f, 3f), accentColor);
        CreateCircuitAccent(badgeObject.transform, new Vector2(132f, 0f), new Vector2(38f, 3f), accentColor);

        Text iconText = CreateText(badgeObject.transform, "*", 24, TextAnchor.MiddleCenter, accentColor);
        SetRect(iconText.rectTransform, new Vector2(-106f, 0f), new Vector2(36f, 36f));

        Text badgeText = CreateText(badgeObject.transform, label, 24, TextAnchor.MiddleCenter, accentColor);
        SetRect(badgeText.rectTransform, new Vector2(22f, 0f), new Vector2(220f, 38f));
        return badgeRect;
    }

    private void CreateHolographicSeparator(Transform parent, Vector2 position, Color accentColor)
    {
        CreateCircuitAccent(parent, position, new Vector2(250f, 2f), accentColor);
        CreateCircuitAccent(parent, position + new Vector2(-150f, 0f), new Vector2(28f, 4f), victoryColor);
        CreateCircuitAccent(parent, position + new Vector2(150f, 0f), new Vector2(28f, 4f), victoryColor);
    }

    private void CreatePanelLayer(Transform parent, string objectName, Vector2 position, Vector2 size, Color fillColor, Color outlineColor, float outlineDistance)
    {
        GameObject layerObject = new GameObject(objectName);
        layerObject.transform.SetParent(parent, false);

        RectTransform layerRect = layerObject.AddComponent<RectTransform>();
        SetRect(layerRect, position, size);

        Image layerImage = layerObject.AddComponent<Image>();
        layerImage.color = fillColor;
        layerImage.raycastTarget = false;

        Outline outline = layerObject.AddComponent<Outline>();
        outline.effectColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.36f);
        outline.effectDistance = new Vector2(outlineDistance, -outlineDistance);
    }

    private void CreateTitleWings(Transform parent, Vector2 centerPosition)
    {
        CreateCircuitAccent(parent, centerPosition + new Vector2(-465f, 0f), new Vector2(160f, 8f), victoryColor);
        CreateCircuitAccent(parent, centerPosition + new Vector2(465f, 0f), new Vector2(160f, 8f), victoryColor);
        CreateCircuitAccent(parent, centerPosition + new Vector2(-490f, -18f), new Vector2(130f, 7f), victoryColor);
        CreateCircuitAccent(parent, centerPosition + new Vector2(490f, -18f), new Vector2(130f, 7f), victoryColor);
        CreateCircuitAccent(parent, centerPosition + new Vector2(-515f, -36f), new Vector2(92f, 6f), victoryColor);
        CreateCircuitAccent(parent, centerPosition + new Vector2(515f, -36f), new Vector2(92f, 6f), victoryColor);
    }

    private void CreateFrameDetails(Transform parent, Vector2 cornerOffset, Color primaryColor, Color secondaryColor)
    {
        CreateFrameCorner(parent, new Vector2(-cornerOffset.x, cornerOffset.y), primaryColor, true, true);
        CreateFrameCorner(parent, new Vector2(cornerOffset.x, cornerOffset.y), primaryColor, false, true);
        CreateFrameCorner(parent, new Vector2(-cornerOffset.x, -cornerOffset.y), secondaryColor, true, false);
        CreateFrameCorner(parent, new Vector2(cornerOffset.x, -cornerOffset.y), secondaryColor, false, false);
        CreateCircuitAccent(parent, new Vector2(0f, cornerOffset.y), new Vector2(620f, 2f), new Color(0.15f, 1f, 0.72f, 1f));
        CreateCircuitAccent(parent, new Vector2(0f, -cornerOffset.y), new Vector2(620f, 2f), new Color(0.08f, 0.9f, 1f, 1f));
    }

    private void CreateFrameCorner(Transform parent, Vector2 cornerPosition, Color color, bool leftSide, bool topSide)
    {
        float horizontalSign = leftSide ? 1f : -1f;
        float verticalSign = topSide ? -1f : 1f;
        CreateCircuitAccent(parent, cornerPosition + new Vector2(horizontalSign * 72f, 0f), new Vector2(145f, 4f), color);
        CreateCircuitAccent(parent, cornerPosition + new Vector2(0f, verticalSign * 58f), new Vector2(4f, 116f), color);
        CreateCircuitAccent(parent, cornerPosition + new Vector2(horizontalSign * 118f, verticalSign * 18f), new Vector2(55f, 3f), color);
    }

    private void CreateCardFrameDetails(Transform parent, Vector2 cornerOffset, Color color)
    {
        CreateCardCorner(parent, new Vector2(-cornerOffset.x, cornerOffset.y), color, true, true);
        CreateCardCorner(parent, new Vector2(cornerOffset.x, cornerOffset.y), color, false, true);
        CreateCardCorner(parent, new Vector2(-cornerOffset.x, -cornerOffset.y), color, true, false);
        CreateCardCorner(parent, new Vector2(cornerOffset.x, -cornerOffset.y), color, false, false);
    }

    private void CreateCardCorner(Transform parent, Vector2 cornerPosition, Color color, bool leftSide, bool topSide)
    {
        float horizontalSign = leftSide ? 1f : -1f;
        float verticalSign = topSide ? -1f : 1f;
        CreateCircuitAccent(parent, cornerPosition + new Vector2(horizontalSign * 42f, 0f), new Vector2(84f, 2f), color);
        CreateCircuitAccent(parent, cornerPosition + new Vector2(0f, verticalSign * 30f), new Vector2(2f, 60f), color);
        CreateCircuitAccent(parent, cornerPosition + new Vector2(horizontalSign * 76f, verticalSign * 10f), new Vector2(30f, 1.5f), color);
    }

    private void CreatePanelCornerAccents(Transform parent, Vector2 cornerOffset, Color accentColor)
    {
        Vector2[] corners =
        {
            new Vector2(-cornerOffset.x, cornerOffset.y),
            new Vector2(cornerOffset.x, cornerOffset.y),
            new Vector2(-cornerOffset.x, -cornerOffset.y),
            new Vector2(cornerOffset.x, -cornerOffset.y)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            CreateCircuitAccent(parent, corners[i], new Vector2(90f, 5f), accentColor);
            CreateCircuitAccent(parent, corners[i] + new Vector2(i % 2 == 0 ? -42f : 42f, i < 2 ? -42f : 42f), new Vector2(5f, 90f), accentColor);
        }
    }

    private void CreateCircuitAccent(Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject accentObject = new GameObject("Circuit Accent");
        accentObject.transform.SetParent(parent, false);

        RectTransform accentRect = accentObject.AddComponent<RectTransform>();
        SetRect(accentRect, position, size);

        Image accentImage = accentObject.AddComponent<Image>();
        Color accentColor = color;
        accentColor.a = 0.5f;
        accentImage.color = accentColor;
        accentImage.raycastTarget = false;
    }

    private RectTransform CreateRankingCard(Transform parent, CharacterResult characterResult, int place, Vector2 position, Color accentColor, bool isWinnerCard)
    {
        string placeLabel = place == 1 ? "1ST PLACE" : "2ND PLACE";

        GameObject cardObject = new GameObject(placeLabel + " Card");
        cardObject.transform.SetParent(parent, false);

        RectTransform cardRect = cardObject.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = position;
        cardRect.sizeDelta = new Vector2(590f, 330f);

        Image cardImage = cardObject.AddComponent<Image>();
        cardImage.color = isWinnerCard ? new Color(0.02f, 0.085f, 0.04f, 0.95f) : new Color(0.105f, 0.012f, 0.014f, 0.94f);

        Outline outline = cardObject.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, isWinnerCard ? 0.78f : 0.64f);
        outline.effectDistance = new Vector2(2f, -2f);

        Shadow shadow = cardObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, isWinnerCard ? 0.5f : 0.34f);
        shadow.effectDistance = new Vector2(0f, isWinnerCard ? -8f : -5f);

        CreatePanelLayer(cardObject.transform, "Card Inner Holographic Layer", Vector2.zero, new Vector2(540f, 280f), new Color(0f, 0f, 0f, 0.24f), accentColor, 1f);
        CreateCardFrameDetails(cardObject.transform, new Vector2(254f, 122f), accentColor);
        CreateCircuitAccent(cardObject.transform, new Vector2(105f, 54f), new Vector2(292f, 1.5f), accentColor);

        Text placeText = CreateText(cardObject.transform, placeLabel, 24, TextAnchor.MiddleCenter, accentColor);
        SetRect(placeText.rectTransform, new Vector2(-122f, 112f), new Vector2(260f, 36f));

        Text cardNameText = CreateText(cardObject.transform, characterResult.CharacterName.ToUpperInvariant(), 38, TextAnchor.MiddleLeft, Color.white);
        SetRect(cardNameText.rectTransform, new Vector2(112f, 81f), new Vector2(260f, 50f));
        RegisterDebugTarget(characterResult.CharacterName.ToUpperInvariant() + " NAME", cardNameText.rectTransform);

        RectTransform portraitRect = CreateCharacterPortrait(cardObject.transform, characterResult.CharacterName, new Vector2(-140f, -30f), accentColor);
        RegisterDebugTarget(characterResult.CharacterName.ToUpperInvariant() + " PORTRAIT", portraitRect);

        CreateStatLine(cardObject.transform, "FINAL SCORE", characterResult.Score.ToString(), 8f, accentColor);
        CreateStatLine(cardObject.transform, "DISTANCE", characterResult.Distance + " / 100", -47f, accentColor);
        CreateStatLine(cardObject.transform, "TIME", FormatTime(characterResult.Time), -102f, accentColor);
        return cardRect;
    }

    private void CreateStatLine(Transform parent, string label, string value, float y, Color accentColor)
    {
        Text labelText = CreateText(parent, label, 18, TextAnchor.MiddleLeft, new Color(0.78f, 1f, 0.92f, 1f));
        SetRect(labelText.rectTransform, new Vector2(72f, y), new Vector2(170f, 30f));

        Text valueText = CreateText(parent, value, 25, TextAnchor.MiddleRight, accentColor);
        SetRect(valueText.rectTransform, new Vector2(164f, y), new Vector2(112f, 32f));

        CreateCircuitAccent(parent, new Vector2(82f, y - 27f), new Vector2(216f, 1.25f), accentColor);
    }

    private RectTransform CreateCharacterPortrait(Transform parent, string characterName, Vector2 position, Color accentColor)
    {
        GameObject frameObject = new GameObject(characterName + " Portrait Frame");
        frameObject.transform.SetParent(parent, false);

        RectTransform frameRect = frameObject.AddComponent<RectTransform>();
        SetRect(frameRect, position, new Vector2(198f, 198f));

        Image frameImage = frameObject.AddComponent<Image>();
        frameImage.color = new Color(0f, 0.08f, 0.075f, 0.88f);
        frameImage.raycastTarget = false;

        Outline frameOutline = frameObject.AddComponent<Outline>();
        frameOutline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.72f);
        frameOutline.effectDistance = new Vector2(2f, -2f);

        Shadow frameShadow = frameObject.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.32f);
        frameShadow.effectDistance = new Vector2(0f, -7f);

        GameObject portraitObject = new GameObject(characterName + " Portrait");
        portraitObject.transform.SetParent(frameObject.transform, false);

        RectTransform portraitRect = portraitObject.AddComponent<RectTransform>();
        StretchToParent(portraitRect);
        portraitRect.offsetMin = new Vector2(12f, 12f);
        portraitRect.offsetMax = new Vector2(-12f, -12f);

        Image portraitImage = portraitObject.AddComponent<Image>();
        portraitImage.sprite = LoadCharacterPortrait(characterName);
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
        return frameRect;
    }

    private RectTransform CreateEndButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = label == "RESTART" ? new Vector2(350f, 48f) : new Vector2(380f, 58f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = label == "RESTART" ? new Color(0.025f, 0.15f, 0.055f, 0.96f) : new Color(0.015f, 0.105f, 0.13f, 0.96f);

        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(0f, -5f);

        Outline outline = buttonObject.AddComponent<Outline>();
        Color buttonAccent = label == "RESTART" ? victoryColor : cyanColor;
        outline.effectColor = new Color(buttonAccent.r, buttonAccent.g, buttonAccent.b, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);

        CreatePanelLayer(buttonObject.transform, "Button Inner Layer", Vector2.zero, new Vector2(344f, 42f), new Color(0f, 0f, 0f, 0.12f), buttonAccent, 1f);
        CreateCircuitAccent(buttonObject.transform, new Vector2(-154f, 0f), new Vector2(34f, 3f), buttonAccent);
        CreateCircuitAccent(buttonObject.transform, new Vector2(154f, 0f), new Vector2(34f, 3f), buttonAccent);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = buttonImage;
        button.colors = BuildButtonColors();
        button.onClick.AddListener(action);
        AddButtonAnimation(buttonObject, buttonRect);

        Text buttonText = CreateText(buttonObject.transform, label, 29, TextAnchor.MiddleCenter, buttonAccent);
        StretchToParent(buttonText.rectTransform);
        return buttonRect;
    }

    private void AddButtonAnimation(GameObject buttonObject, RectTransform buttonRect)
    {
        EventTrigger trigger = buttonObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => buttonRect.localScale = Vector3.one * 1.035f);
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => buttonRect.localScale = Vector3.one);
        trigger.triggers.Add(exitEntry);

        EventTrigger.Entry downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback.AddListener(_ => buttonRect.localScale = Vector3.one * 0.965f);
        trigger.triggers.Add(downEntry);

        EventTrigger.Entry upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upEntry.callback.AddListener(_ => buttonRect.localScale = Vector3.one * 1.035f);
        trigger.triggers.Add(upEntry);
    }

    private void CreateResultsDebugPanel(Transform parent)
    {
        GameObject debugObject = new GameObject("Results Layout Debug Panel");
        debugObject.transform.SetParent(parent, false);

        RectTransform debugRect = debugObject.AddComponent<RectTransform>();
        debugRect.anchorMin = new Vector2(0f, 1f);
        debugRect.anchorMax = new Vector2(0f, 1f);
        debugRect.pivot = new Vector2(0f, 1f);
        debugRect.anchoredPosition = new Vector2(18f, -18f);
        debugRect.sizeDelta = new Vector2(310f, 245f);

        Image debugImage = debugObject.AddComponent<Image>();
        debugImage.color = new Color(0f, 0.02f, 0.025f, 0.9f);

        Outline debugOutline = debugObject.AddComponent<Outline>();
        debugOutline.effectColor = cyanColor;
        debugOutline.effectDistance = new Vector2(2f, -2f);

        Text titleText = CreateText(debugObject.transform, "RESULT UI DEBUG", 18, TextAnchor.MiddleCenter, victoryColor);
        SetRect(titleText.rectTransform, new Vector2(0f, 98f), new Vector2(280f, 30f));

        debugNameText = CreateText(debugObject.transform, "", 16, TextAnchor.MiddleCenter, Color.white);
        SetRect(debugNameText.rectTransform, new Vector2(0f, 66f), new Vector2(280f, 28f));

        debugPositionText = CreateText(debugObject.transform, "", 14, TextAnchor.MiddleCenter, cyanColor);
        SetRect(debugPositionText.rectTransform, new Vector2(0f, 38f), new Vector2(290f, 28f));

        CreateDebugButton(debugObject.transform, "<", new Vector2(-72f, 5f), new Vector2(54f, 32f), SelectPreviousDebugTarget);
        CreateDebugButton(debugObject.transform, ">", new Vector2(72f, 5f), new Vector2(54f, 32f), SelectNextDebugTarget);
        CreateDebugButton(debugObject.transform, "UP", new Vector2(0f, -32f), new Vector2(64f, 30f), () => MoveSelectedDebugTarget(Vector2.up * 5f));
        CreateDebugButton(debugObject.transform, "LEFT", new Vector2(-76f, -68f), new Vector2(66f, 30f), () => MoveSelectedDebugTarget(Vector2.left * 5f));
        CreateDebugButton(debugObject.transform, "RIGHT", new Vector2(76f, -68f), new Vector2(66f, 30f), () => MoveSelectedDebugTarget(Vector2.right * 5f));
        CreateDebugButton(debugObject.transform, "DOWN", new Vector2(0f, -104f), new Vector2(72f, 30f), () => MoveSelectedDebugTarget(Vector2.down * 5f));
        CreateDebugButton(debugObject.transform, "W+", new Vector2(-116f, -140f), new Vector2(54f, 28f), () => ResizeSelectedDebugTarget(new Vector2(10f, 0f)));
        CreateDebugButton(debugObject.transform, "W-", new Vector2(-58f, -140f), new Vector2(54f, 28f), () => ResizeSelectedDebugTarget(new Vector2(-10f, 0f)));
        CreateDebugButton(debugObject.transform, "H+", new Vector2(58f, -140f), new Vector2(54f, 28f), () => ResizeSelectedDebugTarget(new Vector2(0f, 10f)));
        CreateDebugButton(debugObject.transform, "H-", new Vector2(116f, -140f), new Vector2(54f, 28f), () => ResizeSelectedDebugTarget(new Vector2(0f, -10f)));

        UpdateDebugReadout();
    }

    private void CreateDebugButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject("Debug " + label + " Button");
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        SetRect(buttonRect, position, size);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.01f, 0.12f, 0.13f, 0.95f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = cyanColor;
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreateText(buttonObject.transform, label, 13, TextAnchor.MiddleCenter, victoryColor);
        StretchToParent(text.rectTransform);
    }

    private void RegisterDebugTarget(string targetName, RectTransform targetRect)
    {
        if (targetRect == null)
        {
            return;
        }

        debugTargets.Add(new DebugTarget { Name = targetName, Rect = targetRect });
    }

    private void SelectPreviousDebugTarget()
    {
        if (debugTargets.Count == 0)
        {
            return;
        }

        selectedDebugTargetIndex = (selectedDebugTargetIndex - 1 + debugTargets.Count) % debugTargets.Count;
        UpdateDebugReadout();
    }

    private void SelectNextDebugTarget()
    {
        if (debugTargets.Count == 0)
        {
            return;
        }

        selectedDebugTargetIndex = (selectedDebugTargetIndex + 1) % debugTargets.Count;
        UpdateDebugReadout();
    }

    private void MoveSelectedDebugTarget(Vector2 delta)
    {
        RectTransform target = GetSelectedDebugRect();

        if (target == null)
        {
            return;
        }

        target.anchoredPosition += delta;
        UpdateDebugReadout();
    }

    private void ResizeSelectedDebugTarget(Vector2 delta)
    {
        RectTransform target = GetSelectedDebugRect();

        if (target == null)
        {
            return;
        }

        target.sizeDelta += delta;
        UpdateDebugReadout();
    }

    private RectTransform GetSelectedDebugRect()
    {
        if (debugTargets.Count == 0)
        {
            return null;
        }

        selectedDebugTargetIndex = Mathf.Clamp(selectedDebugTargetIndex, 0, debugTargets.Count - 1);
        return debugTargets[selectedDebugTargetIndex].Rect;
    }

    private void UpdateDebugReadout()
    {
        if (debugNameText == null || debugPositionText == null)
        {
            return;
        }

        if (debugTargets.Count == 0)
        {
            debugNameText.text = "NO TARGETS";
            debugPositionText.text = "";
            return;
        }

        DebugTarget target = debugTargets[selectedDebugTargetIndex];
        Vector2 position = target.Rect.anchoredPosition;
        Vector2 size = target.Rect.sizeDelta;
        debugNameText.text = (selectedDebugTargetIndex + 1) + "/" + debugTargets.Count + "  " + target.Name;
        debugPositionText.text = "X " + position.x.ToString("0.0") + "  Y " + position.y.ToString("0.0") + "  W " + size.x.ToString("0.0") + "  H " + size.y.ToString("0.0");
    }

    private void CreateFadeLayer(Transform parent)
    {
        fadeImage = CreateFullScreenImage(parent, "End Screen Fade To Black", new Color(0f, 0f, 0f, 0f));
        fadeImage.raycastTarget = false;
    }

    private IEnumerator PlayEndSequence()
    {
        AudioManager.Instance.FadeGameplayAudio(0.18f, 0.45f);

        Time.timeScale = 0.6f;
        yield return ShakeCamera(0.12f, playerWon ? 0.06f : 0.1f);

        float elapsed = 0f;

        messageGroup.alpha = 0f;

        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / panelFadeDuration);
            rootGroup.alpha = t;
            comparisonGroup.alpha = t;
            comparisonPanelRect.localScale = Vector3.one * Mathf.LerpUnclamped(0.88f, 1f, EaseOutBack(t));
            SetImageAlpha(flashImage, Mathf.Sin(t * Mathf.PI) * 0.18f);
            yield return null;
        }

        rootGroup.alpha = 1f;
        messageGroup.alpha = 0f;
        SetImageAlpha(flashImage, 0f);
        comparisonGroup.alpha = 1f;
        comparisonGroup.interactable = true;
        comparisonGroup.blocksRaycasts = true;
        Time.timeScale = 0f;
    }

    private IEnumerator ShakeCamera(float duration, float strength)
    {
        Camera targetCamera = Camera.main;

        if (targetCamera == null)
            yield break;

        Vector3 originalPosition = targetCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * strength;
            targetCamera.transform.position = originalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        targetCamera.transform.position = originalPosition;
    }

    private void RestartRun()
    {
        if (isTransitioning)
            return;

        AudioManager.Instance.PlayUIClickSound();
        StartCoroutine(TransitionToScene(true));
    }

    private void ReturnToMainMenu()
    {
        if (isTransitioning)
            return;

        AudioManager.Instance.PlayUIClickSound();
        StartCoroutine(TransitionToScene(false));
    }

    private IEnumerator TransitionToScene(bool restartRun)
    {
        isTransitioning = true;
        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetImageAlpha(fadeImage, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        AudioManager.Instance.StopAllGameplayAudio();
        Time.timeScale = 1f;

        if (restartRun)
        {
            MainMenuUI.SkipMainMenuOnNextSceneLoad();
            CharacterSelectionManager.UseSavedSelectionOnNextSceneLoad();
        }
        else
        {
            MainMenuUI.ForceShowMainMenuOnNextLoad();
        }

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private void AnimateScanlines()
    {
        if (scanlines == null)
            return;

        for (int i = 0; i < scanlines.Length; i++)
        {
            Vector2 position = scanlines[i].anchoredPosition;
            position.y += Time.unscaledDeltaTime * (playerWon ? 58f + i * 6f : 84f + i * 9f);

            if (position.y > 560f)
                position.y = -560f;

            scanlines[i].anchoredPosition = position;
        }
    }

    private void AnimateParticles()
    {
        if (particles == null)
            return;

        for (int i = 0; i < particles.Length; i++)
        {
            Vector2 position = particles[i].anchoredPosition;
            position.y += Time.unscaledDeltaTime * (12f + i % 7);
            position.x += Mathf.Sin(Time.unscaledTime * 1.2f + i) * Time.unscaledDeltaTime * 7f;

            if (position.y > 540f)
                position.y = -540f;

            particles[i].anchoredPosition = position;
        }
    }

    private void AnimateMessageGlitch()
    {
        if (messageGroup == null || messageGroup.alpha <= 0f || resultMessageText == null)
            return;

        float glitch = Mathf.PerlinNoise(Time.unscaledTime * 18f, 0.35f);
        float offset = glitch > 0.78f ? UnityEngine.Random.Range(-14f, 14f) : 0f;
        resultMessageText.rectTransform.anchoredPosition = new Vector2(offset, 0f);
        resultMessageText.color = playerWon ? victoryColor : Color.Lerp(defeatColor, cyanColor, glitch * 0.28f);
    }

    private ColorBlock BuildButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.6f, 1f, 0.65f, 1f);
        colors.pressedColor = victoryColor;
        colors.selectedColor = new Color(0.42f, 1f, 0.55f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
        colors.colorMultiplier = 0.78f;
        return colors;
    }

    private void EnsureFallbackStats()
    {
        if (string.IsNullOrEmpty(results.PlayerRank))
            results.PlayerRank = GetRank(results.PlayerScore);

        if (string.IsNullOrEmpty(results.AIRank))
            results.AIRank = GetRank(results.AIScore);

        if (string.IsNullOrEmpty(results.PlayerCharacterName))
            results.PlayerCharacterName = "Player";

        if (string.IsNullOrEmpty(results.AICharacterName))
            results.AICharacterName = "AI";
    }

    private void HideGameplayCameraBackground()
    {
        mainCameraCache = Camera.main;

        if (mainCameraCache == null)
        {
            return;
        }

        previousCameraBackgroundColor = mainCameraCache.backgroundColor;
        previousCameraClearFlags = mainCameraCache.clearFlags;
        mainCameraCache.clearFlags = CameraClearFlags.SolidColor;
        mainCameraCache.backgroundColor = Color.black;
    }

    private void RestoreGameplayCameraBackground()
    {
        if (mainCameraCache == null)
        {
            return;
        }

        mainCameraCache.clearFlags = previousCameraClearFlags;
        mainCameraCache.backgroundColor = previousCameraBackgroundColor;
    }

    private CharacterResult[] BuildRankedResults()
    {
        CharacterResult[] sourceResults = results.CharacterResults;

        if (sourceResults == null || sourceResults.Length == 0)
        {
            sourceResults = new CharacterResult[]
            {
                new CharacterResult
                {
                    CharacterName = results.PlayerCharacterName,
                    IsPlayer = true,
                    Score = results.PlayerScore,
                    Distance = results.PlayerDistance,
                    Time = results.PlayerTime,
                    Energy = results.PlayerEnergy,
                    Rank = results.PlayerRank
                },
                new CharacterResult
                {
                    CharacterName = results.AICharacterName,
                    IsPlayer = false,
                    Score = results.AIScore,
                    Distance = results.AIDistance,
                    Time = results.AITime,
                    Energy = results.AIEnergy,
                    Rank = results.AIRank
                }
            };
        }

        CharacterResult[] sortedResults = new CharacterResult[sourceResults.Length];

        for (int i = 0; i < sourceResults.Length; i++)
        {
            sortedResults[i] = NormalizeCharacterResult(sourceResults[i]);
        }

        Array.Sort(sortedResults, CompareCharacterResults);
        return sortedResults;
    }

    private CharacterResult NormalizeCharacterResult(CharacterResult characterResult)
    {
        if (string.IsNullOrEmpty(characterResult.CharacterName))
        {
            characterResult.CharacterName = characterResult.IsPlayer ? "Player" : "AI";
        }

        characterResult.Score = Mathf.Clamp(characterResult.Score, 0, 100);
        characterResult.Distance = Mathf.Clamp(characterResult.Distance, 0, 100);
        characterResult.Energy = Mathf.Clamp(characterResult.Energy, 0, 100);

        if (string.IsNullOrEmpty(characterResult.Rank))
        {
            characterResult.Rank = GetRank(characterResult.Score);
        }

        return characterResult;
    }

    private int GetPlayerPlace()
    {
        for (int i = 0; i < rankedResults.Length; i++)
        {
            if (rankedResults[i].IsPlayer)
            {
                return i + 1;
            }
        }

        return 1;
    }

    private string GetPlaceLabel(int place)
    {
        if (place == 1)
        {
            return "1ST PLACE";
        }

        if (place == 2)
        {
            return "2ND PLACE";
        }

        if (place == 3)
        {
            return "3RD PLACE";
        }

        return place + "TH PLACE";
    }

    private static int CompareCharacterResults(CharacterResult first, CharacterResult second)
    {
        int scoreComparison = second.Score.CompareTo(first.Score);

        if (scoreComparison != 0)
        {
            return scoreComparison;
        }

        int timeComparison = first.Time.CompareTo(second.Time);

        if (timeComparison != 0)
        {
            return timeComparison;
        }

        if (first.IsPlayer == second.IsPlayer)
        {
            return 0;
        }

        return first.IsPlayer ? -1 : 1;
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

    private Image CreateFullScreenImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        RectTransform imageRect = imageObject.AddComponent<RectTransform>();
        StretchToParent(imageRect);

        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Sprite LoadSprite(string resourceName)
    {
        Sprite sprite = Resources.Load<Sprite>(resourceName);

        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourceName);

        if (texture == null)
        {
            Debug.LogWarning("EndGameManager could not load Resources/" + resourceName + ".");
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite LoadCharacterPortrait(string characterName)
    {
        if (string.Equals(characterName, "Bob", StringComparison.OrdinalIgnoreCase))
        {
            Sprite bobSprite = Resources.Load<Sprite>("p1");

            if (bobSprite != null)
            {
                return bobSprite;
            }
        }

        if (string.Equals(characterName, "Alya", StringComparison.OrdinalIgnoreCase))
        {
            Sprite alyaPreview = CharacterSelectionManager.GetPreviewSprite(0);

            if (alyaPreview != null)
            {
                return alyaPreview;
            }

            Texture2D alyaTexture = Resources.Load<Texture2D>("AlyaSprites");

            if (alyaTexture != null)
            {
                int frameWidth = Mathf.Max(1, alyaTexture.width / 3);
                return Sprite.Create(alyaTexture, new Rect(0f, 0f, frameWidth, alyaTexture.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        Sprite fallbackSprite = Resources.Load<Sprite>(characterName);

        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        return CharacterSelectionManager.GetPreviewSprite(0);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
        return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
    }

    private static string GetRank(int score)
    {
        if (score >= 95)
            return "S";

        if (score >= 85)
            return "A";

        if (score >= 70)
            return "B";

        if (score >= 55)
            return "C";

        return "D";
    }

    private class DebugTarget
    {
        public string Name;
        public RectTransform Rect;
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        t -= 1f;
        return 1f + t * t * ((overshoot + 1f) * t + overshoot);
    }
}
