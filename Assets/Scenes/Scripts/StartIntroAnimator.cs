using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartIntroAnimator : MonoBehaviour
{
    private static readonly string[] LetterSpriteResources =
    {
        "StartS",
        "StartT",
        "StartA",
        "StartR",
        "StartTt",
        "StartSigno"
    };

    public static bool GameStarted { get; private set; }

    [SerializeField] private PlayerFollowMouse playerMovement;
    [SerializeField] private float letterEnterDuration = 0.32f;
    [SerializeField] private float letterEnterStagger = 0.055f;
    [SerializeField] private float bounceDuration = 0.2f;
    [SerializeField] private float visibleDuration = 0.42f;
    [SerializeField] private float exitDuration = 0.3f;
    [SerializeField] private float letterHeight = 270f;
    [SerializeField] private float letterSpacing = -8f;

    private CanvasGroup canvasGroup;
    private RectTransform wordRect;
    private LetterView[] letters;
    private Image[] sparkImages;

    private class LetterView
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Center;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForLoadedScene()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SpawnIntro();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SpawnIntro();
    }

    private static void SpawnIntro()
    {
        GameStarted = false;

        if (FindObjectOfType<StartIntroAnimator>() != null)
        {
            return;
        }

        GameObject introObject = new GameObject(nameof(StartIntroAnimator));
        introObject.AddComponent<StartIntroAnimator>();
    }

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerFollowMouse>();
        }

        BuildIntroCanvas();
    }

    private void Start()
    {
        StartCoroutine(PlayStartSequence());
    }

    private IEnumerator PlayStartSequence()
    {
        while (!CharacterSelectionManager.HasSessionSelection)
        {
            SetPlayerControl(false);
            yield return null;
        }

        SetPlayerControl(false);
        CursorInputRouter.Instance.ForceRelease();

        canvasGroup.alpha = 1f;
        wordRect.anchoredPosition = Vector2.zero;
        SetSparkAlpha(0f);
        SetLettersVisible(false);

        AudioManager.Instance.PlayStartIntroSoundOnce();
        yield return AnimateLettersIn();
        yield return AnimateWordScale(1f, 1.1f, bounceDuration * 0.45f, EaseOutQuad);
        yield return AnimateWordScale(1.1f, 1f, bounceDuration * 0.55f, EaseOutQuad);
        yield return PlaySparkFlash(0.22f);
        yield return new WaitForSeconds(visibleDuration);

        yield return AnimateExit();

        GameStarted = true;
        SetPlayerControl(true);
        CursorInputRouter.Instance.ForceRelease();
        Destroy(gameObject);
    }

    private void BuildIntroCanvas()
    {
        GameObject canvasObject = new GameObject("StartIntroCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        UIResponsiveUtility.ApplyCanvasScaler(scaler);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;

        GameObject wordObject = new GameObject("START! Letters");
        wordObject.transform.SetParent(canvasObject.transform, false);

        wordRect = wordObject.AddComponent<RectTransform>();
        wordRect.anchorMin = new Vector2(0.5f, 0.5f);
        wordRect.anchorMax = new Vector2(0.5f, 0.5f);
        wordRect.pivot = new Vector2(0.5f, 0.5f);
        wordRect.anchoredPosition = Vector2.zero;

        BuildLetterImages(wordRect);

        sparkImages = new[]
        {
            CreateSpark(canvasObject.transform, new Vector2(488f, 118f), 62f),
            CreateSpark(canvasObject.transform, new Vector2(-508f, 104f), 42f),
            CreateSpark(canvasObject.transform, new Vector2(175f, -120f), 36f)
        };
    }

    private void BuildLetterImages(RectTransform parent)
    {
        letters = new LetterView[LetterSpriteResources.Length];
        Sprite[] sprites = new Sprite[LetterSpriteResources.Length];
        float totalWidth = 0f;

        for (int i = 0; i < LetterSpriteResources.Length; i++)
        {
            sprites[i] = Resources.Load<Sprite>(LetterSpriteResources[i]);

            if (sprites[i] == null)
            {
                Debug.LogWarning("StartIntroAnimator could not load Resources/" + LetterSpriteResources[i] + ". Add the START letter image to Assets/Resources.");
                continue;
            }

            totalWidth += GetSpriteWidthForTargetHeight(sprites[i]);

            if (i < LetterSpriteResources.Length - 1)
            {
                totalWidth += letterSpacing;
            }
        }

        parent.sizeDelta = new Vector2(totalWidth, letterHeight);
        float cursorX = -totalWidth * 0.5f;

        for (int i = 0; i < LetterSpriteResources.Length; i++)
        {
            Sprite sprite = sprites[i];
            float width = sprite != null ? GetSpriteWidthForTargetHeight(sprite) : letterHeight * 0.7f;
            Vector2 center = new Vector2(cursorX + width * 0.5f, 0f);
            cursorX += width + letterSpacing;

            GameObject letterObject = new GameObject(LetterSpriteResources[i]);
            letterObject.transform.SetParent(parent, false);

            RectTransform rect = letterObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = new Vector2(width, letterHeight);
            rect.localScale = Vector3.zero;

            Image image = letterObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            SetImageAlpha(image, 0f);

            letters[i] = new LetterView
            {
                Rect = rect,
                Image = image,
                Center = center
            };
        }
    }

    private Image CreateSpark(Transform parent, Vector2 anchoredPosition, float size)
    {
        GameObject sparkObject = new GameObject("StartSpark");
        sparkObject.transform.SetParent(parent, false);

        RectTransform rect = sparkObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(size, size);
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image image = sparkObject.AddComponent<Image>();
        image.color = new Color(0.7f, 1f, 0.18f, 0f);
        image.raycastTarget = false;
        return image;
    }

    private IEnumerator AnimateLettersIn()
    {
        float elapsed = 0f;
        float totalDuration = letterEnterDuration + letterEnterStagger * (letters.Length - 1);

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < letters.Length; i++)
            {
                float localTime = elapsed - letterEnterStagger * i;

                if (localTime < 0f)
                {
                    continue;
                }

                float t = Mathf.Clamp01(localTime / letterEnterDuration);
                float eased = EaseOutBack(t);
                LetterView letter = letters[i];
                float yOffset = Mathf.Lerp(-70f, 0f, EaseOutQuad(t));

                letter.Rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.05f, 1f, eased);
                letter.Rect.anchoredPosition = letter.Center + Vector2.up * yOffset;
                SetImageAlpha(letter.Image, t);
            }

            yield return null;
        }

        for (int i = 0; i < letters.Length; i++)
        {
            letters[i].Rect.localScale = Vector3.one;
            letters[i].Rect.anchoredPosition = letters[i].Center;
            SetImageAlpha(letters[i].Image, 1f);
        }
    }

    private IEnumerator AnimateWordScale(float from, float to, float duration, System.Func<float, float> easing)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            wordRect.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, easing(t));
            yield return null;
        }

        wordRect.localScale = Vector3.one * to;
    }

    private IEnumerator PlaySparkFlash(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Sin(t * Mathf.PI);
            SetSparkAlpha(alpha);
            yield return null;
        }

        SetSparkAlpha(0f);
    }

    private IEnumerator AnimateExit()
    {
        float elapsed = 0f;
        Vector2 startPosition = wordRect.anchoredPosition;
        Vector2 endPosition = startPosition + Vector2.up * 130f;

        while (elapsed < exitDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / exitDuration);
            float eased = EaseInQuad(t);

            canvasGroup.alpha = 1f - eased;
            wordRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.68f, eased);
            wordRect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, eased);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    private void SetPlayerControl(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.canMove = enabled;
        }
    }

    private void SetLettersVisible(bool visible)
    {
        if (letters == null)
        {
            return;
        }

        for (int i = 0; i < letters.Length; i++)
        {
            letters[i].Rect.localScale = visible ? Vector3.one : Vector3.zero;
            SetImageAlpha(letters[i].Image, visible ? 1f : 0f);
        }
    }

    private void SetSparkAlpha(float alpha)
    {
        if (sparkImages == null)
        {
            return;
        }

        for (int i = 0; i < sparkImages.Length; i++)
        {
            Color color = sparkImages[i].color;
            color.a = alpha;
            sparkImages[i].color = color;
            sparkImages[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.35f, alpha);
        }
    }

    private float GetSpriteWidthForTargetHeight(Sprite sprite)
    {
        return letterHeight * sprite.rect.width / sprite.rect.height;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseInQuad(float t)
    {
        return t * t;
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        t -= 1f;
        return 1f + t * t * ((overshoot + 1f) * t + overshoot);
    }
}
