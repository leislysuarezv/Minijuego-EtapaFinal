using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RivalAIController : MonoBehaviour
{
    private const string RivalSpriteResource = "RivalAICharacter";
    private const float AlyaRoleScale = 0.41423255f;
    private const float BobRoleScale = 0.97f;
    private static readonly string[] AnimationFrameResources =
    {
        "p1",
        "p2",
        "p3"
    };

    public static int RivalScore { get; private set; }
    public static float RivalScorePercent { get; private set; }

    [SerializeField] private Vector2 startPosition = new Vector2(-32.24f, -0.08f);
    [SerializeField] private Vector2 paintStartPosition = new Vector2(-30.69f, -0.08f);
    [SerializeField] private Vector2 finishPosition = new Vector2(36.35f, -0.48f);
    [SerializeField] private float moveSpeed = 5.25f;
    [SerializeField] private float paintPointSpacing = 0.12f;
    [SerializeField] private Vector2 rivalAccuracyRange = new Vector2(65f, 70f);
    [SerializeField] private float paintImprecisionAt65Percent = 1.05f;
    [SerializeField] private float paintImprecisionAt70Percent = 0.85f;
    [SerializeField] private float startDelay = 0.12f;
    [SerializeField] private float targetScale = 0.97f;
    [SerializeField] private int maxScore = 300;

    private SpriteRenderer spriteRenderer;
    private LineRenderer paintLine;
    private readonly List<Vector3> paintPoints = new List<Vector3>();
    private Sprite[] animationFrames;
    private ScoreManager scoreManager;
    private Vector2[] routePoints;
    private float[] routeDistances;
    private float routeLength;
    private float travelledDistance;
    private float progress;
    private float animationTimer;
    private int animationFrameIndex;
    private bool isRunning;
    private bool hasFinished;
    private bool hasStartedPainting;
    private bool hasConfiguredRivalVisual;
    private float selectedRivalAccuracyPercent;
    private float selectedPaintImprecision;
    private Vector3 baseScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForLoadedScene()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Spawn();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Spawn();
    }

    private static void Spawn()
    {
        if (FindObjectOfType<RivalAIController>() != null)
        {
            return;
        }

        GameObject rivalObject = new GameObject("Rival AI");
        rivalObject.AddComponent<RivalAIController>();
    }

    private void Awake()
    {
        RivalScore = 0;
        RivalScorePercent = 0f;
        selectedRivalAccuracyPercent = Random.Range(rivalAccuracyRange.x, rivalAccuracyRange.y);
        selectedPaintImprecision = GetPaintImprecisionForAccuracy(selectedRivalAccuracyPercent);
        scoreManager = FindObjectOfType<ScoreManager>();
        BuildRoute();
        BuildVisual();
        transform.position = startPosition;
    }

    private void Update()
    {
        if (hasFinished)
        {
            return;
        }

        if (!hasConfiguredRivalVisual && CharacterSelectionManager.HasSessionSelection)
        {
            ConfigureRivalVisualForUnselectedCharacter();
        }

        if (!StartIntroAnimator.GameStarted || ScoreManager.CurrentPhase != ScoreManager.GamePhase.Painting)
        {
            isRunning = false;
            return;
        }

        if (!isRunning)
        {
            isRunning = true;
            StartCoroutine(RunRace());
        }

        AnimateVisual();
    }

    private IEnumerator RunRace()
    {
        yield return new WaitForSeconds(startDelay);

        while (StartIntroAnimator.GameStarted && ScoreManager.CurrentPhase == ScoreManager.GamePhase.Painting)
        {
            travelledDistance = Mathf.Min(routeLength, travelledDistance + moveSpeed * Time.deltaTime);
            progress = routeLength > 0f ? Mathf.Clamp01(travelledDistance / routeLength) : 1f;
            UpdateRivalScore();

            Vector2 basePosition = GetRoutePosition(travelledDistance);
            Vector3 newPosition = new Vector3(basePosition.x, basePosition.y, 0f);
            transform.position = newPosition;

            if (progress > 0f && newPosition.x >= paintStartPosition.x)
            {
                if (!hasStartedPainting)
                {
                    hasStartedPainting = true;
                    AudioManager.Instance.PlayPaintGestureAudio();
                }

                AddPaintPoint(newPosition);
            }

            if (!hasFinished && progress >= 1f)
            {
                hasFinished = true;
                isRunning = false;
                progress = 1f;
                transform.position = finishPosition;
                UpdateRivalScore();
                FreezeVisual();
                AudioManager.Instance.StopPaintGestureAudioFromGame();

                if (scoreManager != null)
                {
                    scoreManager.RegisterRivalFinished();
                }

                yield break;
            }

            yield return null;
        }
    }

    private void BuildRoute()
    {
        routePoints = new[]
        {
            startPosition,
            paintStartPosition,
            new Vector2(-27.76f, -0.08f),
            new Vector2(-25.46f, -0.08f),
            new Vector2(-24.63f, -0.08f),
            new Vector2(-24.10f, -0.35f),
            new Vector2(-23.69f, -1.54f),
            new Vector2(-20.55f, -1.54f),
            new Vector2(-17.42f, -1.54f),
            new Vector2(-17.21f, -0.62f),
            new Vector2(-17.06f, 0.39f),
            new Vector2(-14.75f, 0.39f),
            new Vector2(-12.09f, 0.39f),
            new Vector2(-11.72f, -0.38f),
            new Vector2(-11.47f, -1.41f),
            new Vector2(-7.24f, -1.41f),
            new Vector2(-3.52f, -1.41f),
            new Vector2(-3.25f, -0.55f),
            new Vector2(-3.00f, 0.39f),
            new Vector2(1.65f, 0.39f),
            new Vector2(5.83f, 0.39f),
            new Vector2(9.07f, 0.39f),
            new Vector2(9.38f, -0.38f),
            new Vector2(9.74f, -1.47f),
            new Vector2(15.49f, -1.47f),
            new Vector2(21.34f, -1.47f),
            new Vector2(21.58f, -0.98f),
            new Vector2(21.86f, -0.48f),
            new Vector2(26.57f, -0.08f),
            new Vector2(30.23f, 0.02f),
            new Vector2(33.36f, 0.06f),
            new Vector2(35.87f, -0.10f),
            finishPosition
        };

        routeDistances = new float[routePoints.Length];
        routeLength = 0f;

        for (int i = 1; i < routePoints.Length; i++)
        {
            routeLength += Vector2.Distance(routePoints[i - 1], routePoints[i]);
            routeDistances[i] = routeLength;
        }
    }

    private Vector2 GetRoutePosition(float distance)
    {
        if (routePoints == null || routePoints.Length == 0)
        {
            return Vector2.Lerp(startPosition, finishPosition, progress);
        }

        if (distance <= 0f)
        {
            return routePoints[0];
        }

        for (int i = 1; i < routePoints.Length; i++)
        {
            if (distance <= routeDistances[i])
            {
                float segmentStart = routeDistances[i - 1];
                float segmentLength = Mathf.Max(0.01f, routeDistances[i] - segmentStart);
                float t = Mathf.Clamp01((distance - segmentStart) / segmentLength);
                return Vector2.Lerp(routePoints[i - 1], routePoints[i], t);
            }
        }

        return routePoints[routePoints.Length - 1];
    }

    private void BuildVisual()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        animationFrames = LoadAnimationFrames();
        spriteRenderer.sprite = animationFrames.Length > 0 ? animationFrames[0] : LoadSprite(RivalSpriteResource);
        spriteRenderer.sortingOrder = 3;
        baseScale = Vector3.one * targetScale;
        transform.localScale = baseScale;

        BuildPaintLine();
    }

    private void ConfigureRivalVisualForUnselectedCharacter()
    {
        hasConfiguredRivalVisual = true;
        int rivalCharacterIndex = CharacterSelectionManager.RivalCharacterIndex;

        if (rivalCharacterIndex == 0)
        {
            animationFrames = LoadAlyaAnimationFrames();
            Sprite alyaSprite = animationFrames.Length > 0 ? animationFrames[0] : CharacterSelectionManager.GetPreviewSprite(0);

            if (spriteRenderer != null && alyaSprite != null)
            {
                spriteRenderer.sprite = alyaSprite;
            }

            baseScale = Vector3.one * AlyaRoleScale;
            transform.localScale = baseScale;
            return;
        }

        animationFrames = LoadAnimationFrames();
        baseScale = Vector3.one * BobRoleScale;
        transform.localScale = baseScale;

        if (spriteRenderer != null && animationFrames.Length > 0)
        {
            spriteRenderer.sprite = animationFrames[0];
        }
    }

    private void BuildPaintLine()
    {
        GameObject lineObject = new GameObject("Rival AI Paint");
        paintLine = lineObject.AddComponent<LineRenderer>();
        paintLine.useWorldSpace = true;
        paintLine.positionCount = 0;
        paintLine.startWidth = 0.2f;
        paintLine.endWidth = 0.2f;
        paintLine.numCapVertices = 4;
        paintLine.numCornerVertices = 4;
        paintLine.sortingOrder = 2;
        paintLine.material = new Material(Shader.Find("Sprites/Default"));
        paintLine.startColor = new Color(1f, 0.24f, 0.95f, 1f);
        paintLine.endColor = new Color(1f, 0.24f, 0.95f, 1f);
    }

    private void AddPaintPoint(Vector3 position)
    {
        if (paintLine == null)
        {
            return;
        }

        if (paintPoints.Count > 0 && Vector3.Distance(paintPoints[paintPoints.Count - 1], position) <= paintPointSpacing)
        {
            return;
        }

        paintPoints.Add(GetSlightlyImprecisePaintPosition(position));
        paintLine.positionCount = paintPoints.Count;
        paintLine.SetPositions(paintPoints.ToArray());
    }

    private Vector3 GetSlightlyImprecisePaintPosition(Vector3 position)
    {
        if (progress <= 0.02f || progress >= 0.98f)
        {
            return position;
        }

        float xNoise = Mathf.PerlinNoise(position.x * 0.35f, 3.7f) - 0.5f;
        float yNoise = Mathf.PerlinNoise(position.x * 0.28f, 8.4f) - 0.5f;
        return position + new Vector3(xNoise * selectedPaintImprecision, yNoise * selectedPaintImprecision, 0f);
    }

    private float GetPaintImprecisionForAccuracy(float accuracyPercent)
    {
        float accuracyT = Mathf.InverseLerp(rivalAccuracyRange.x, rivalAccuracyRange.y, accuracyPercent);
        return Mathf.Lerp(paintImprecisionAt65Percent, paintImprecisionAt70Percent, accuracyT);
    }

    private Sprite[] LoadAnimationFrames()
    {
        Sprite[] frames = new Sprite[AnimationFrameResources.Length];
        int loadedCount = 0;

        for (int i = 0; i < AnimationFrameResources.Length; i++)
        {
            frames[i] = LoadSprite(AnimationFrameResources[i]);

            if (frames[i] != null)
            {
                loadedCount++;
            }
        }

        if (loadedCount == frames.Length)
        {
            return frames;
        }

        return new Sprite[0];
    }

    private Sprite[] LoadAlyaAnimationFrames()
    {
        Texture2D texture = Resources.Load<Texture2D>("AlyaSprites");

        if (texture == null)
        {
            Sprite previewSprite = CharacterSelectionManager.GetPreviewSprite(0);
            return previewSprite != null ? new[] { previewSprite } : new Sprite[0];
        }

        int frameCount = 3;
        int frameWidth = texture.width / frameCount;
        Sprite[] frames = new Sprite[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            Rect frameRect = new Rect(i * frameWidth, 0f, frameWidth, texture.height);
            frames[i] = Sprite.Create(texture, frameRect, new Vector2(0.5f, 0.5f), 100f);
        }

        return frames;
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
            Debug.LogWarning("RivalAIController could not load Resources/" + resourceName + ".");
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void AnimateVisual()
    {
        if (hasFinished)
        {
            return;
        }

        if (animationFrames != null && animationFrames.Length > 0)
        {
            animationTimer += Time.deltaTime;

            if (animationTimer >= 0.14f)
            {
                animationTimer = 0f;
                animationFrameIndex = (animationFrameIndex + 1) % animationFrames.Length;
                spriteRenderer.sprite = animationFrames[animationFrameIndex];
            }
        }

        transform.localScale = baseScale;
    }

    private void FreezeVisual()
    {
        animationTimer = 0f;

        if (spriteRenderer != null && animationFrames != null && animationFrames.Length > 0)
        {
            spriteRenderer.sprite = animationFrames[0];
        }

        transform.localScale = baseScale;
    }

    private void UpdateRivalScore()
    {
        float imperfectScoreMultiplier = Mathf.Clamp01(selectedRivalAccuracyPercent / 100f);
        RivalScore = Mathf.RoundToInt(maxScore * progress * imperfectScoreMultiplier);
        RivalScorePercent = Mathf.Clamp01(RivalScore / (float)maxScore) * 100f;
    }
}
