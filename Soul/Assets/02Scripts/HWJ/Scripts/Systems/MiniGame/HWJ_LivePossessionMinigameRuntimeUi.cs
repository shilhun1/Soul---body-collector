using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a small runtime-only minigame HUD when a scene has no authored UI references.
/// Authored prefab UI always takes priority, so this does not replace team UI work.
/// </summary>
public sealed partial class HWJ_LivePossessionMinigameController
{
    private const int RuntimeUiSortingOrder = 30000;
    private static readonly Vector2 RuntimeUiReferenceResolution = new Vector2(1920f, 1080f);

    private void EnsureRuntimeUi()
    {
        if (HasConfiguredUi || !createFallbackUiWhenMissing)
        {
            return;
        }

        Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject canvasObject = new GameObject(
            "HWJ_RuntimePossessionMinigameCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = RuntimeUiSortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = RuntimeUiReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = CreateRuntimeUiObject("PossessionMinigamePanel", canvasObject.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 170f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.035f, 0.04f, 0.055f, 0.96f);

        instructionText = CreateRuntimeText(
            "Instruction",
            panel.transform,
            runtimeFont,
            "LIVE POSSESSION - TAP SPACE",
            22,
            TextAnchor.MiddleCenter,
            new Vector2(18f, 108f),
            new Vector2(-18f, -12f));

        timerText = CreateRuntimeText(
            "Timer",
            panel.transform,
            runtimeFont,
            "5.0s",
            18,
            TextAnchor.MiddleRight,
            new Vector2(18f, 72f),
            new Vector2(-18f, -48f));

        mentalGaugeSlider = CreateRuntimeSlider(panel.transform);

        resultText = CreateRuntimeText(
            "GaugeValue",
            panel.transform,
            runtimeFont,
            "Gauge: 50%",
            17,
            TextAnchor.MiddleCenter,
            new Vector2(18f, 14f),
            new Vector2(-18f, -106f));

        minigameRoot = panel;
        Debug.Log(
            "[HWJ][PossessionMinigame][UI] Missing scene UI was replaced with the runtime fallback HUD.",
            this);
    }

    private static Slider CreateRuntimeSlider(Transform parent)
    {
        GameObject sliderObject = CreateRuntimeUiObject("MentalGauge", parent);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.offsetMin = new Vector2(28f, 58f);
        sliderRect.offsetMax = new Vector2(-28f, 82f);

        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(0.16f, 0.17f, 0.21f, 1f);

        GameObject fillArea = CreateRuntimeUiObject("FillArea", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        Stretch(fillAreaRect, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        GameObject fill = CreateRuntimeUiObject("Fill", fillArea.transform);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect, Vector2.zero, Vector2.zero);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.82f, 0.92f, 1f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    private static Text CreateRuntimeText(
        string objectName,
        Transform parent,
        Font font,
        string value,
        int fontSize,
        TextAnchor alignment,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject textObject = CreateRuntimeUiObject(objectName, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        Stretch(rect, offsetMin, offsetMax);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateRuntimeUiObject(string objectName, Transform parent)
    {
        GameObject created = new GameObject(objectName, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return created;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
