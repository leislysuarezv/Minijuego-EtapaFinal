using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LetterSequenceAnimator : MonoBehaviour
{
    private class LetterView
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Center;
    }

    public static IEnumerator Play(string[] resourceNames, float letterHeight, float letterSpacing, float visibleDuration, float exitDuration)
    {
        GameObject animatorObject = new GameObject(nameof(LetterSequenceAnimator));
        LetterSequenceAnimator animator = animatorObject.AddComponent<LetterSequenceAnimator>();
        yield return animator.PlaySequence(resourceNames, letterHeight, letterSpacing, visibleDuration, exitDuration);
    }

    private CanvasGroup canvasGroup;
    private RectTransform wordRect;
    private LetterView[] letters;
    private Image[] sparkImages;

    private IEnumerator PlaySequence(string[] resourceNames, float letterHeight, float letterSpacing, float visibleDuration, float exitDuration)
    {
        BuildCanvas(resourceNames, letterHeight, letterSpacing);

        canvasGroup.alpha = 1f;
        wordRect.anchoredPosition = Vector2.zero;
        SetSparkAlpha(0f);
        SetLettersVisible(false);

        yield return AnimateLettersIn(0.32f, 0.055f);
        yield return AnimateWordScale(1f, 1.1f, 0.09f, EaseOutQuad);
        yield return AnimateWordScale(1.1f, 1f, 0.11f, EaseOutQuad);
        yield return PlaySparkFlash(0.22f);
        yield return new WaitForSeconds(visibleDuration);
        yield return AnimateExit(exitDuration);

        Destroy(gameObject);
    }

    private void BuildCanvas(string[] resourceNames, float letterHeight, float letterSpacing)
    {
        GameObject canvasObject = new GameObject("LetterSequenceCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        UIResponsiveUtility.ApplyCanvasScaler(scaler);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;

        GameObject wordObject = new GameObject("Animated Letters");
        wordObject.transform.SetParent(canvasObject.transform, false);

        wordRect = wordObject.AddComponent<RectTransform>();
        wordRect.anchorMin = new Vector2(0.5f, 0.5f);
        wordRect.anchorMax = new Vector2(0.5f, 0.5f);
        wordRect.pivot = new Vector2(0.5f, 0.5f);
        wordRect.anchoredPosition = Vector2.zero;

        BuildLetterImages(resourceNames, letterHeight, letterSpacing);

        sparkImages = new[]
        {
            CreateSpark(canvasObject.transform, new Vector2(488f, 118f), 62f),
            CreateSpark(canvasObject.transform, new Vector2(-508f, 104f), 42f),
            CreateSpark(canvasObject.transform, new Vector2(175f, -120f), 36f)
        };
    }

    private void BuildLetterImages(string[] resourceNames, float letterHeight, float letterSpacing)
    {
        letters = new LetterView[resourceNames.Length];
        Sprite[] sprites = new Sprite[resourceNames.Length];
        float totalWidth = 0f;

        for (int i = 0; i < resourceNames.Length; i++)
        {
            sprites[i] = LoadSpriteWithFallback(resourceNames[i]);

            if (sprites[i] == null)
            {
                Debug.LogWarning("LetterSequenceAnimator could not load Resources/" + resourceNames[i] + ".");
                continue;
            }

            totalWidth += GetSpriteWidthForTargetHeight(sprites[i], letterHeight);

            if (i < resourceNames.Length - 1)
            {
                totalWidth += letterSpacing;
            }
        }

        wordRect.sizeDelta = new Vector2(totalWidth, letterHeight);
        float cursorX = -totalWidth * 0.5f;

        for (int i = 0; i < resourceNames.Length; i++)
        {
            Sprite sprite = sprites[i];
            float width = sprite != null ? GetSpriteWidthForTargetHeight(sprite, letterHeight) : letterHeight * 0.7f;
            Vector2 center = new Vector2(cursorX + width * 0.5f, 0f);
            cursorX += width + letterSpacing;

            GameObject letterObject = new GameObject(resourceNames[i]);
            letterObject.transform.SetParent(wordRect, false);

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

    private Sprite LoadSpriteWithFallback(string resourceName)
    {
        Sprite sprite = Resources.Load<Sprite>(resourceName);

        if (sprite == null && resourceName == "HF")
        {
            sprite = Resources.Load<Sprite>("H");
        }

        if (sprite == null && resourceName == "S")
        {
            sprite = Resources.Load<Sprite>("FS");
        }

        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourceName);

            if (texture == null && resourceName == "HF")
            {
                texture = Resources.Load<Texture2D>("H");
            }

            if (texture == null && resourceName == "S")
            {
                texture = Resources.Load<Texture2D>("FS");
            }

            if (texture != null)
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        return sprite;
    }

    private Image CreateSpark(Transform parent, Vector2 anchoredPosition, float size)
    {
        GameObject sparkObject = new GameObject("LetterSpark");
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

    private IEnumerator AnimateLettersIn(float duration, float stagger)
    {
        float elapsed = 0f;
        float totalDuration = duration + stagger * (letters.Length - 1);

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < letters.Length; i++)
            {
                float localTime = elapsed - stagger * i;

                if (localTime < 0f)
                {
                    continue;
                }

                float t = Mathf.Clamp01(localTime / duration);
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
            SetSparkAlpha(Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        SetSparkAlpha(0f);
    }

    private IEnumerator AnimateExit(float duration)
    {
        float elapsed = 0f;
        Vector2 startPosition = wordRect.anchoredPosition;
        Vector2 endPosition = startPosition + Vector2.up * 130f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseInQuad(t);

            canvasGroup.alpha = 1f - eased;
            wordRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.68f, eased);
            wordRect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, eased);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    private void SetLettersVisible(bool visible)
    {
        for (int i = 0; i < letters.Length; i++)
        {
            letters[i].Rect.localScale = visible ? Vector3.one : Vector3.zero;
            SetImageAlpha(letters[i].Image, visible ? 1f : 0f);
        }
    }

    private void SetSparkAlpha(float alpha)
    {
        for (int i = 0; i < sparkImages.Length; i++)
        {
            Color color = sparkImages[i].color;
            color.a = alpha;
            sparkImages[i].color = color;
            sparkImages[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.35f, alpha);
        }
    }

    private static float GetSpriteWidthForTargetHeight(Sprite sprite, float targetHeight)
    {
        return targetHeight * sprite.rect.width / sprite.rect.height;
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
