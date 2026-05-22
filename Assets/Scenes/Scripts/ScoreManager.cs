using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    private static readonly string[] FinishLetterResources =
    {
        "F",
        "F1",
        "N",
        "F2",
        "SF",
        "H",
        "FS"
    };

    public enum GamePhase
    {
        Painting,
        Finish,
        Results
    }

    public static GamePhase CurrentPhase { get; private set; } = GamePhase.Painting;
    public static event Action<GamePhase> PhaseChanged;

    public int score = 0;
    public int maxScore = 200;

    public GameObject worldScoreGroup;
    public TextMesh mainText;
    public TextMesh shadowText;
    public GameObject finalText;
    public Camera mainCamera;
    public CameraFollow cameraFollow;
    public PlayerFollowMouse playerMovement;
    public Animator playerAnimator;

    public Vector3 finalCamPosition = new Vector3(21f, 3.2f, -10f);
    public float finalCamSize = 8f;
    public float scoreCounterCamSize = 10.5f;
    public Vector2 playerScoreStartWorld = new Vector2(-30.69f, 6.78f);
    public Vector2 playerScoreEndWorld = new Vector2(33.4f, 6.78f);
    public Vector2 rivalScoreStartWorld = new Vector2(-30.69f, -0.08f);
    public Vector2 rivalScoreEndWorld = new Vector2(33.4f, -0.08f);
    public Vector2 scorePanelWorldOffset = new Vector2(-2.1f, 1.35f);

    private bool hasStartedFinalSequence;
    private bool playerReachedFinish;
    private bool rivalReachedFinish;
    private Transform rivalTransform;
    private float playerPaintAccuracyPercent;
    private float gameplayStartTime = -1f;
    private float playerFinishTime = -1f;
    private float rivalFinishTime = -1f;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraFollow == null && mainCamera != null)
            cameraFollow = mainCamera.GetComponent<CameraFollow>();

        SetPhase(GamePhase.Painting);

        if (worldScoreGroup != null)
            worldScoreGroup.SetActive(false);

        if (finalText != null)
            finalText.SetActive(false);

        ConfigureCameraFollowTargets();
    }

    void Update()
    {
        if (gameplayStartTime < 0f && StartIntroAnimator.GameStarted)
        {
            gameplayStartTime = Time.time;
        }
    }

    public void AddScore(int amount)
    {
        if (CurrentPhase != GamePhase.Painting || playerReachedFinish)
            return;

        score += amount;
    }

    public void ReportPlayerPaintAccuracy(float accuracyPercent)
    {
        if (CurrentPhase != GamePhase.Painting || playerReachedFinish)
            return;

        playerPaintAccuracyPercent = Mathf.Clamp(accuracyPercent, 0f, 100f);
    }

    public void RegisterPlayerFinished()
    {
        if (playerReachedFinish || hasStartedFinalSequence)
            return;

        playerReachedFinish = true;
        playerFinishTime = GetElapsedGameplayTime();
        CursorInputRouter.Instance.ForceRelease();

        if (playerMovement != null)
            playerMovement.canMove = false;

        if (playerAnimator != null)
            playerAnimator.enabled = false;

        SelectedCharacterSpriteAnimator selectedSpriteAnimator = playerMovement != null ? playerMovement.GetComponent<SelectedCharacterSpriteAnimator>() : null;

        if (selectedSpriteAnimator != null)
            selectedSpriteAnimator.FreezeToFirstFrame();

        TryStartFinalSequence();
    }

    public void RegisterRivalFinished()
    {
        if (rivalReachedFinish || hasStartedFinalSequence)
            return;

        rivalReachedFinish = true;
        rivalFinishTime = GetElapsedGameplayTime();
        TryStartFinalSequence();
    }

    public void ShowFinalScore()
    {
        playerReachedFinish = true;
        rivalReachedFinish = true;

        if (playerFinishTime < 0f)
            playerFinishTime = GetElapsedGameplayTime();

        if (rivalFinishTime < 0f)
            rivalFinishTime = GetElapsedGameplayTime();

        TryStartFinalSequence();
    }

    private void TryStartFinalSequence()
    {
        if (hasStartedFinalSequence)
            return;

        if (!playerReachedFinish || !rivalReachedFinish)
            return;

        // The finish state begins as soon as painting is no longer allowed.
        hasStartedFinalSequence = true;
        SetPhase(GamePhase.Finish);

        CursorInputRouter.Instance.ForceRelease();

        StartCoroutine(ShowFinalSequence());
    }

    IEnumerator ShowFinalSequence()
    {
        if (playerMovement != null)
            playerMovement.canMove = false;

        if (playerAnimator != null)
            playerAnimator.enabled = false;

        SelectedCharacterSpriteAnimator selectedSpriteAnimator = playerMovement != null ? playerMovement.GetComponent<SelectedCharacterSpriteAnimator>() : null;

        if (selectedSpriteAnimator != null)
            selectedSpriteAnimator.FreezeToFirstFrame();

        ConfigureCameraFollowTargets();

        if (cameraFollow != null)
            cameraFollow.followPlayer = true;

        yield return new WaitForSeconds(0.5f);

        if (finalText != null)
            finalText.SetActive(false);

        AudioManager.Instance.PlayFinishSound();
        yield return LetterSequenceAnimator.Play(FinishLetterResources, 270f, -8f, 0.75f, 0.3f);

        if (worldScoreGroup != null)
            worldScoreGroup.SetActive(false);

        int playerFinalScore = Mathf.RoundToInt(Mathf.Clamp(playerPaintAccuracyPercent, 0f, 100f));
        int rivalFinalScore = Mathf.RoundToInt(Mathf.Clamp(RivalAIController.RivalScorePercent, 0f, 100f));

        if (cameraFollow != null)
            cameraFollow.followPlayer = false;

        if (mainCamera != null)
        {
            mainCamera.transform.position = finalCamPosition;
            mainCamera.orthographicSize = scoreCounterCamSize;
        }

        yield return PlayFinalScorePanels(playerFinalScore, rivalFinalScore);

        if (mainCamera != null)
        {
            mainCamera.transform.position = finalCamPosition;
            mainCamera.orthographicSize = finalCamSize;
        }

        SetPhase(GamePhase.Results);
        ShowEndGameResults(playerFinalScore, rivalFinalScore);

        Debug.Log("Score: " + playerFinalScore);
    }

    private void ShowEndGameResults(int playerFinalScore, int rivalFinalScore)
    {
        float timeSurvived = gameplayStartTime > 0f ? Time.time - gameplayStartTime : Time.timeSinceLevelLoad;
        float playerTime = playerFinishTime >= 0f ? playerFinishTime : timeSurvived;
        float aiTime = rivalFinishTime >= 0f ? rivalFinishTime : timeSurvived;
        string playerCharacterName = CharacterSelectionManager.SelectedCharacterIndex == 0 ? "Alya" : "Bob";
        string aiCharacterName = CharacterSelectionManager.RivalCharacterIndex == 0 ? "Alya" : "Bob";

        EndGameManager.CharacterResult playerResult = new EndGameManager.CharacterResult
        {
            CharacterName = playerCharacterName,
            IsPlayer = true,
            Score = playerFinalScore,
            Distance = 100,
            Time = playerTime,
            Energy = Mathf.Clamp(playerFinalScore, 0, 100),
            Rank = GetRank(playerFinalScore)
        };

        EndGameManager.CharacterResult aiResult = new EndGameManager.CharacterResult
        {
            CharacterName = aiCharacterName,
            IsPlayer = false,
            Score = rivalFinalScore,
            Distance = 100,
            Time = aiTime,
            Energy = Mathf.Clamp(rivalFinalScore, 0, 100),
            Rank = GetRank(rivalFinalScore)
        };

        EndGameManager.ShowResults(new EndGameManager.EndGameResults
        {
            PlayerCharacterName = playerCharacterName,
            AICharacterName = aiCharacterName,
            PlayerScore = playerFinalScore,
            AIScore = rivalFinalScore,
            PlayerDistance = 100,
            AIDistance = 100,
            PlayerTime = playerTime,
            AITime = aiTime,
            PlayerEnergy = Mathf.Clamp(playerFinalScore, 0, 100),
            AIEnergy = Mathf.Clamp(rivalFinalScore, 0, 100),
            PlayerRank = GetRank(playerFinalScore),
            AIRank = GetRank(rivalFinalScore),
            CharacterResults = new EndGameManager.CharacterResult[] { playerResult, aiResult }
        });
    }

    private float GetElapsedGameplayTime()
    {
        return gameplayStartTime > 0f ? Time.time - gameplayStartTime : Time.timeSinceLevelLoad;
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

    void SetPhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;

        if (PhaseChanged != null)
            PhaseChanged.Invoke(CurrentPhase);
    }

    private IEnumerator PlayFinalScorePanels(int playerFinalScore, int rivalFinalScore)
    {
        GameObject canvasObject = new GameObject("Final Score Panels");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        UIResponsiveUtility.ApplyCanvasScaler(scaler);

        canvasObject.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

        string playerScoreResource = CharacterSelectionManager.SelectedCharacterIndex == 0 ? "scoreprincipal" : "scoreia";
        string rivalScoreResource = CharacterSelectionManager.RivalCharacterIndex == 0 ? "scoreprincipal" : "scoreia";
        FinalScorePanelView playerPanel = CreateScorePanel(canvasObject.transform, playerScoreResource, "Jugador", playerScoreStartWorld + scorePanelWorldOffset, playerScoreEndWorld + scorePanelWorldOffset, canvasRect);
        FinalScorePanelView rivalPanel = CreateScorePanel(canvasObject.transform, rivalScoreResource, "IA", rivalScoreStartWorld + scorePanelWorldOffset, rivalScoreEndWorld + scorePanelWorldOffset, canvasRect);

        AudioManager.Instance.PlayResultsSound();
        yield return AnimateScorePanels(playerPanel, rivalPanel, playerFinalScore, rivalFinalScore, canvasRect);
        yield return FadeScorePanels(canvasGroup, 1f, 0f, 0.22f);

        Destroy(canvasObject);
    }

    private FinalScorePanelView CreateScorePanel(Transform parent, string resourceName, string label, Vector2 startWorldPosition, Vector2 targetWorldPosition, RectTransform canvasRect)
    {
        GameObject panelObject = new GameObject(label + " Score Panel");
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = GetCanvasPoint(canvasRect, startWorldPosition);
        panelRect.sizeDelta = new Vector2(560f, 260f);
        panelRect.localScale = Vector3.one * 0.82f;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.sprite = LoadSprite(resourceName);
        panelImage.preserveAspect = true;
        panelImage.raycastTarget = false;

        PixelNumberDisplay scoreNumber = new PixelNumberDisplay(panelObject.transform, new Vector2(48f, -8f));
        scoreNumber.SetValue(0);

        return new FinalScorePanelView
        {
            Rect = panelRect,
            StartWorldPosition = startWorldPosition,
            TargetWorldPosition = targetWorldPosition,
            ScoreNumber = scoreNumber
        };
    }

    private IEnumerator AnimateScorePanels(FinalScorePanelView playerPanel, FinalScorePanelView rivalPanel, int playerFinalScore, int rivalFinalScore, RectTransform canvasRect)
    {
        float duration = 2.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBack(t);
            int playerValue = Mathf.RoundToInt(Mathf.Lerp(0f, playerFinalScore, t));
            int rivalValue = Mathf.RoundToInt(Mathf.Lerp(0f, rivalFinalScore, t));
            Vector2 playerWorldPosition = Vector2.LerpUnclamped(playerPanel.StartWorldPosition, playerPanel.TargetWorldPosition, eased);
            Vector2 rivalWorldPosition = Vector2.LerpUnclamped(rivalPanel.StartWorldPosition, rivalPanel.TargetWorldPosition, eased);

            playerPanel.Rect.anchoredPosition = GetCanvasPoint(canvasRect, playerWorldPosition);
            rivalPanel.Rect.anchoredPosition = GetCanvasPoint(canvasRect, rivalWorldPosition);
            playerPanel.Rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.82f, 1f, eased);
            rivalPanel.Rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.82f, 1f, eased);
            playerPanel.ScoreNumber.SetValue(playerValue);
            rivalPanel.ScoreNumber.SetValue(rivalValue);
            yield return null;
        }

        playerPanel.Rect.anchoredPosition = GetCanvasPoint(canvasRect, playerPanel.TargetWorldPosition);
        rivalPanel.Rect.anchoredPosition = GetCanvasPoint(canvasRect, rivalPanel.TargetWorldPosition);
        playerPanel.Rect.localScale = Vector3.one;
        rivalPanel.Rect.localScale = Vector3.one;
        playerPanel.ScoreNumber.SetValue(playerFinalScore);
        rivalPanel.ScoreNumber.SetValue(rivalFinalScore);

        yield return new WaitForSeconds(1.25f);
    }

    private IEnumerator FadeScorePanels(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
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
            Debug.LogWarning("ScoreManager could not load Resources/" + resourceName + ".");
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Vector2 GetCanvasPoint(RectTransform canvasRect, Vector2 worldPosition)
    {
        Camera scoreCamera = mainCamera != null ? mainCamera : Camera.main;

        if (scoreCamera == null)
        {
            return Vector2.zero;
        }

        Vector2 localPoint;
        Vector2 screenPoint = scoreCamera.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);
        return localPoint;
    }

    private void ConfigureCameraFollowTargets()
    {
        if (cameraFollow == null)
            return;

        if (playerMovement != null)
            cameraFollow.player = playerMovement.transform;

        RivalAIController rival = FindObjectOfType<RivalAIController>();
        rivalTransform = rival != null ? rival.transform : rivalTransform;
        cameraFollow.secondaryTarget = rivalTransform;
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        t -= 1f;
        return 1f + t * t * ((overshoot + 1f) * t + overshoot);
    }

    private class FinalScorePanelView
    {
        public RectTransform Rect;
        public Vector2 StartWorldPosition;
        public Vector2 TargetWorldPosition;
        public PixelNumberDisplay ScoreNumber;
    }

    private class PixelNumberDisplay
    {
        private static readonly string[] DigitPatterns =
        {
            "111101101101111",
            "010110010010111",
            "111001111100111",
            "111001111001111",
            "101101111001001",
            "111100111001111",
            "111100111101111",
            "111001001001001",
            "111101111101111",
            "111101111001111"
        };

        private readonly RectTransform root;
        private readonly float pixelSize = 7.5f;
        private readonly float pixelGap = 2f;
        private readonly Color pixelColor = new Color(0.55f, 1f, 0.24f, 1f);
        private int currentValue = -1;

        public PixelNumberDisplay(Transform parent, Vector2 anchoredPosition)
        {
            GameObject rootObject = new GameObject("Pixel Score Number");
            rootObject.transform.SetParent(parent, false);
            root = rootObject.AddComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = new Vector2(220f, 90f);
        }

        public void SetValue(int value)
        {
            value = Mathf.Clamp(value, 0, 100);

            if (value == currentValue)
            {
                return;
            }

            currentValue = value;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
            }

            string valueText = value.ToString();
            float digitWidth = 3f * pixelSize + 2f * pixelGap;
            float digitSpacing = 10f;
            float totalWidth = valueText.Length * digitWidth + (valueText.Length - 1) * digitSpacing;
            float cursorX = -totalWidth * 0.5f;

            for (int i = 0; i < valueText.Length; i++)
            {
                int digit = valueText[i] - '0';
                DrawDigit(digit, cursorX, 0f);
                cursorX += digitWidth + digitSpacing;
            }
        }

        private void DrawDigit(int digit, float startX, float startY)
        {
            string pattern = DigitPatterns[Mathf.Clamp(digit, 0, 9)];

            for (int row = 0; row < 5; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    int index = row * 3 + column;

                    if (pattern[index] != '1')
                    {
                        continue;
                    }

                    GameObject pixelObject = new GameObject("Pixel");
                    pixelObject.transform.SetParent(root, false);
                    RectTransform pixelRect = pixelObject.AddComponent<RectTransform>();
                    pixelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    pixelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    pixelRect.pivot = new Vector2(0.5f, 0.5f);
                    pixelRect.sizeDelta = new Vector2(pixelSize, pixelSize);
                    pixelRect.anchoredPosition = new Vector2(startX + column * (pixelSize + pixelGap), startY + (2f - row) * (pixelSize + pixelGap));

                    Image pixelImage = pixelObject.AddComponent<Image>();
                    pixelImage.color = pixelColor;
                    pixelImage.raycastTarget = false;
                }
            }
        }
    }
}
