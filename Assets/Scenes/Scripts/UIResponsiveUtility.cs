using UnityEngine;
using UnityEngine.UI;

public static class UIResponsiveUtility
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    public static void ApplyCanvasScaler(CanvasScaler scaler)
    {
        if (scaler == null)
        {
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    public static void ConfigureText(Text text, int maxFontSize)
    {
        if (text == null)
        {
            return;
        }

        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(maxFontSize * 0.62f));
        text.resizeTextMaxSize = maxFontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    public static Vector2 GetReferenceSafePanelSize(float preferredWidth, float preferredHeight, float horizontalMargin, float verticalMargin)
    {
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
        float referenceAspect = ReferenceResolution.x / ReferenceResolution.y;
        float availableWidth = ReferenceResolution.x - horizontalMargin * 2f;
        float availableHeight = ReferenceResolution.y - verticalMargin * 2f;

        if (aspect < referenceAspect)
        {
            availableWidth *= Mathf.Clamp(aspect / referenceAspect, 0.82f, 1f);
        }
        else if (aspect > 2.05f)
        {
            availableWidth = Mathf.Min(ReferenceResolution.x - 180f, preferredWidth + 120f);
        }

        float panelWidth = aspect > 2.05f ? availableWidth : Mathf.Min(preferredWidth, availableWidth);
        return new Vector2(panelWidth, Mathf.Min(preferredHeight, availableHeight));
    }
}
