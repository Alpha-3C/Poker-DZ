using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class GameConsoleOverlay : MonoBehaviour
{
    [SerializeField, Range(20, 200)] private int maxMessages = 80;
    [SerializeField, Min(200)] private int panelWidth = 680;
    [SerializeField, Min(120)] private int panelHeight = 300;

    private readonly ConcurrentQueue<LogLine> pending = new ConcurrentQueue<LogLine>();
    private readonly Queue<LogLine> history = new Queue<LogLine>();

    private GameObject overlayRoot;
    private Font overlayFont;
    private Text logText;
    private RectTransform viewportRect;
    private RectTransform contentRect;
    private ScrollRect scrollRect;

    private struct LogLine
    {
        public readonly DateTime Time;
        public readonly string Message;
        public readonly LogType Type;

        public LogLine(string message, LogType type)
        {
            Time = DateTime.Now;
            Message = message;
            Type = type;
        }
    }

    private void Awake()
    {
        CreateOverlay();
    }

    private void OnEnable()
    {
        Application.logMessageReceivedThreaded += ReceiveLog;
        if (overlayRoot != null)
            overlayRoot.SetActive(true);
    }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= ReceiveLog;
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (overlayRoot != null)
            Destroy(overlayRoot);
        if (overlayFont != null)
            Destroy(overlayFont);
    }

    private void ReceiveLog(string message, string stackTrace, LogType type)
    {
        // This callback can run on a worker thread. UI changes happen in LateUpdate.
        pending.Enqueue(new LogLine(message, type));
    }

    private void LateUpdate()
    {
        bool changed = false;
        int processed = 0;
        while (processed < 200 && pending.TryDequeue(out LogLine line))
        {
            history.Enqueue(line);
            while (history.Count > maxMessages)
                history.Dequeue();
            changed = true;
            processed++;
        }

        if (!changed)
            return;

        var text = new StringBuilder();
        foreach (LogLine line in history)
        {
            if (text.Length > 0)
                text.AppendLine();

            text.Append('[').Append(line.Time.ToString("HH:mm:ss")).Append("] ");
            if (line.Type == LogType.Warning)
                text.Append("[警告] ");
            else if (line.Type == LogType.Error || line.Type == LogType.Exception || line.Type == LogType.Assert)
                text.Append("[错误] ");

            text.Append(line.Message);
        }

        logText.text = text.ToString();
        Canvas.ForceUpdateCanvases();
        float contentHeight = Mathf.Max(viewportRect.rect.height, logText.preferredHeight + 4f);
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void CreateOverlay()
    {
        overlayRoot = new GameObject("Game Console Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = overlayRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        DontDestroyOnLoad(overlayRoot);

        overlayFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 20);

        var panel = new GameObject("Console Panel", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        panel.transform.SetParent(overlayRoot.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        var background = panel.GetComponent<Image>();
        background.color = new Color(0.04f, 0.06f, 0.09f, 0.82f);
        background.raycastTarget = false;

        var header = new GameObject("Title", typeof(RectTransform), typeof(Text));
        header.transform.SetParent(panel.transform, false);
        var headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(12f, -36f);
        headerRect.offsetMax = new Vector2(-12f, -6f);
        var headerText = header.GetComponent<Text>();
        headerText.font = overlayFont;
        headerText.fontSize = 20;
        headerText.fontStyle = FontStyle.Bold;
        headerText.color = Color.white;
        headerText.alignment = TextAnchor.MiddleLeft;
        headerText.raycastTarget = false;
        headerText.text = "运行日志";

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(panel.transform, false);
        viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(12f, 12f);
        viewportRect.offsetMax = new Vector2(-12f, -42f);

        var content = new GameObject("Log Text", typeof(RectTransform), typeof(Text));
        content.transform.SetParent(viewport.transform, false);
        contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        logText = content.GetComponent<Text>();
        logText.font = overlayFont;
        logText.fontSize = 18;
        logText.lineSpacing = 1.15f;
        logText.color = Color.white;
        logText.alignment = TextAnchor.UpperLeft;
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Overflow;
        logText.raycastTarget = false;
        logText.text = "等待控制台输出…";

        scrollRect = panel.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = false;
    }
}
