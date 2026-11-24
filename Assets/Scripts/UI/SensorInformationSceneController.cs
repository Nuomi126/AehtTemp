using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Tables;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the responsive shell for the sensor-information scene at runtime.
/// The legacy heat-map stays intact and is placed inside a responsive viewport;
/// the data tab reuses the same readings through phoneTableScript.
/// </summary>
public sealed class SensorInformationSceneController : MonoBehaviour
{
    private const string TargetSceneName = "wendurelitu";
    private static readonly Dictionary<int, CanvasGroup> LoadingCurtains = new();

    private enum SensorPage
    {
        TemperatureData,
        HeatMap
    }

    private static readonly Color Green = new Color32(0, 151, 88, 255);
    private static readonly Color DarkText = new Color32(35, 39, 43, 255);
    private static readonly Color MutedText = new Color32(118, 118, 118, 255);
    private static readonly Color PageBackground = new Color32(242, 243, 244, 255);
    private static readonly Color TableHeader = new Color32(218, 239, 232, 255);
    private static readonly Color TableAlternate = new Color32(235, 247, 243, 255);
    private static readonly Color HeatMapBackground = new Color32(8, 10, 12, 255);

    private Canvas sceneCanvas;
    private CanvasGroup loadingCurtain;
    private RectTransform root;
    private RectTransform header;
    private RectTransform tabs;
    private RectTransform controls;
    private RectTransform sideRail;
    private RectTransform heatMapPage;
    private RectTransform heatMapViewport;
    private RectTransform heatMapContent;
    private RectTransform heatMapLegend;
    private RectTransform heatMapScale;
    private RectTransform heatMapSummary;
    private RectTransform temperaturePage;
    private RectTransform categoryBar;
    private RectTransform tableViewport;
    private RectTransform tableContent;
    private RectTransform legacyHeatMap;
    private ScrollRect heatMapScroll;
    private ScrollRect temperatureDataScroll;

    private Text headerTitle;
    private Text workAreaText;
    private Text slotText;
    private Text landscapePageTitle;
    private Text dataTabText;
    private Text heatMapTabText;
    private Text currentCategoryText;
    private Text heatMapLegendTitle;
    private Text heatMapSummaryText;
    private Text pointToggleText;

    private Button rotateButton;
    private Button dataTabButton;
    private Button heatMapTabButton;
    private Button currentCategoryButton;
    private Button sideDataButton;
    private Button sideHeatMapButton;
    private Button pointToggleButton;

    private Image dataUnderline;
    private Image heatMapUnderline;
    private TableLayout temperatureTable;
    private Font uiFont;

    private phoneTableScript heatMapDataSource;
    private WorkAreaSelector workAreaSelector;
    private SlotNumberSelector slotNumberSelector;

    private readonly List<phoneTableScript.TemperatureSensorReading> latestReadings = new();
    private readonly Dictionary<phoneTableScript.TemperatureSensorCategory, Button> categoryButtons = new();
    private readonly Dictionary<phoneTableScript.TemperatureSensorCategory, Text> categoryTexts = new();
    private readonly List<Text> heatMapLegendLabels = new();
    private readonly List<RectTransform> heatMapScaleSegments = new();

    private SensorPage currentPage = SensorPage.HeatMap;
    private phoneTableScript.TemperatureSensorCategory currentCategory = phoneTableScript.TemperatureSensorCategory.Cathode;
    private bool lastPortrait;
    private Vector2 lastCanvasSize;
    private bool isBuilt;
    private bool showPointLabels = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != TargetSceneName)
            return;

        SensorInformationSceneController existing = FindObjectsOfType<SensorInformationSceneController>(true)
            .FirstOrDefault(controller => controller.gameObject.scene == scene);
        if (existing != null)
            return;

        HideSceneCanvasUntilReady(scene);

        GameObject host = new GameObject("__SensorInformationSceneController");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<SensorInformationSceneController>();
    }

    private static void HideSceneCanvasUntilReady(Scene scene)
    {
        Canvas canvas = scene.GetRootGameObjects()
            .SelectMany(rootObject => rootObject.GetComponentsInChildren<Canvas>(true))
            .FirstOrDefault(candidate => candidate.isRootCanvas);
        if (canvas == null)
            return;

        CanvasGroup curtain = canvas.gameObject.AddComponent<CanvasGroup>();
        curtain.alpha = 0;
        curtain.interactable = false;
        curtain.blocksRaycasts = false;
        LoadingCurtains[scene.handle] = curtain;
    }

    private IEnumerator Start()
    {
        LoadingCurtains.TryGetValue(gameObject.scene.handle, out loadingCurtain);

        // Let the legacy scene initialise selectors and API bindings first.
        yield return null;

        sceneCanvas = FindSceneComponent<Canvas>();
        heatMapDataSource = FindSceneComponent<phoneTableScript>();
        workAreaSelector = FindSceneComponent<WorkAreaSelector>();
        slotNumberSelector = FindSceneComponent<SlotNumberSelector>();

        if (sceneCanvas == null || heatMapDataSource == null)
        {
            Debug.LogError("传感监视界面初始化失败：未找到 Canvas 或 phoneTableScript。");
            RevealSceneCanvas();
            yield break;
        }

        uiFont = Resources.Load<Font>("Fonts/Alibaba-PuHuiTi-Regular")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        BuildInterface();
        BindLegacyComponents();
        isBuilt = true;

        yield return null;
        ApplyResponsiveLayout(true);
        ShowPage(SensorPage.HeatMap);
        ApplyTemperatureSnapshot(heatMapDataSource.GetLatestSensorReadings());
        Canvas.ForceUpdateCanvases();
        RevealSceneCanvas();
    }

    private void RevealSceneCanvas()
    {
        if (loadingCurtain != null)
        {
            loadingCurtain.alpha = 1;
            loadingCurtain.interactable = true;
            loadingCurtain.blocksRaycasts = true;
            Destroy(loadingCurtain);
        }

        LoadingCurtains.Remove(gameObject.scene.handle);
    }

    private void Update()
    {
        if (!isBuilt || root == null)
            return;

        bool portrait = Screen.height >= Screen.width;
        Vector2 canvasSize = root.rect.size;
        if (portrait != lastPortrait || Vector2.SqrMagnitude(canvasSize - lastCanvasSize) > 1f)
        {
            ApplyResponsiveLayout(true);
        }
    }

    private void OnDestroy()
    {
        if (heatMapDataSource != null)
            heatMapDataSource.TemperatureReadingsUpdated -= ApplyTemperatureSnapshot;

        if (workAreaSelector != null && workAreaSelector.OnWorkAreaConfirmedEvent != null)
            workAreaSelector.OnWorkAreaConfirmedEvent.RemoveListener(OnWorkAreaConfirmed);

        if (slotNumberSelector != null && slotNumberSelector.OnSlotConfirmed != null)
            slotNumberSelector.OnSlotConfirmed.RemoveListener(OnSlotConfirmed);
    }

    private T FindSceneComponent<T>() where T : Component
    {
        return FindSceneComponents<T>().FirstOrDefault();
    }

    private IEnumerable<T> FindSceneComponents<T>() where T : Component
    {
        Scene scene = gameObject.scene;
        return FindObjectsOfType<T>(true).Where(component => component.gameObject.scene == scene);
    }

    private void BuildInterface()
    {
        Transform canvasTransform = sceneCanvas.transform;
        HideLegacyChrome(canvasTransform);

        root = CreatePanel("SensorInformationRoot", canvasTransform, PageBackground).rectTransform;
        Stretch(root);

        BuildHeader();
        BuildTabs();
        BuildControls();
        BuildSideRail();
        BuildHeatMapPage();
        BuildTemperaturePage();

        root.SetAsLastSibling();
    }

    private void HideLegacyChrome(Transform canvasTransform)
    {
        string[] hiddenRootNames =
        {
            "headTitleBackImage",
            "choose",
            "totalBackRawImage",
            "Text (TMP)"
        };

        foreach (string objectName in hiddenRootNames)
        {
            Transform child = canvasTransform.Find(objectName);
            if (child != null)
                child.gameObject.SetActive(false);
        }

        Transform workPanel = canvasTransform.Find("gongqSelectionPanel");
        if (workPanel != null)
            workPanel.gameObject.SetActive(false);

        Transform slotPanel = canvasTransform.Find("caohSelectionPanel");
        if (slotPanel != null)
            slotPanel.gameObject.SetActive(false);
    }

    private void BuildHeader()
    {
        header = CreatePanel("PortraitHeader", root, Green).rectTransform;
        Button backButton = CreateButton("Back", header, "‹", Color.clear, Color.white, ReturnToMainScene);
        SetAnchors((RectTransform)backButton.transform, new Vector2(0.015f, 0.08f), new Vector2(0.13f, 0.72f));

        headerTitle = CreateText("Title", header, "传感监视", Color.white, 40, FontStyles.Bold, TextAlignmentOptions.Center);
        SetAnchors(headerTitle.rectTransform, new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.72f));
    }

    private void BuildTabs()
    {
        tabs = CreatePanel("PortraitTabs", root, Color.white).rectTransform;

        dataTabButton = CreateTab("TemperatureDataTab", tabs, "温度数据", 0f, 0.5f,
            () => ShowPage(SensorPage.TemperatureData), out dataTabText, out dataUnderline);
        heatMapTabButton = CreateTab("HeatMapTab", tabs, "温度热力图", 0.5f, 1f,
            () => ShowPage(SensorPage.HeatMap), out heatMapTabText, out heatMapUnderline);
    }

    private Button CreateTab(string name, Transform parent, string label, float left, float right, UnityAction action,
        out Text tabText, out Image underline)
    {
        Button button = CreateButton(name, parent, string.Empty, Color.clear, DarkText, action);
        SetAnchors((RectTransform)button.transform, new Vector2(left, 0), new Vector2(right, 1));

        tabText = CreateText("Label", button.transform, label, MutedText, 32, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(tabText.rectTransform);

        underline = CreatePanel("Underline", button.transform, Green);
        SetAnchors(underline.rectTransform, new Vector2(0.36f, 0.04f), new Vector2(0.64f, 0.09f));
        return button;
    }

    private void BuildControls()
    {
        controls = CreatePanel("SelectorBar", root, PageBackground).rectTransform;

        Button workButton = CreateButton("WorkArea", controls, string.Empty, Color.white, DarkText, OpenWorkAreaSelector);
        workAreaText = CreateText("Value", workButton.transform, "1车间2工区", DarkText, 29, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        SetAnchors(workAreaText.rectTransform, new Vector2(0.06f, 0), new Vector2(0.84f, 1));
        Text chevron = CreateText("Chevron", workButton.transform, "▼", DarkText, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        SetAnchors(chevron.rectTransform, new Vector2(0.84f, 0), new Vector2(0.98f, 1));

        Image slotPanel = CreatePanel("SlotSelector", controls, Color.white);
        Button previous = CreateButton("Previous", slotPanel.transform, "‹", Color.clear, DarkText, () => ChangeSlot(-1));
        SetAnchors((RectTransform)previous.transform, new Vector2(0, 0), new Vector2(0.20f, 1));
        Button slotOpen = CreateButton("SlotValue", slotPanel.transform, string.Empty, Color.clear, DarkText, OpenSlotSelector);
        SetAnchors((RectTransform)slotOpen.transform, new Vector2(0.20f, 0), new Vector2(0.80f, 1));
        slotText = CreateText("Value", slotOpen.transform, "2031", DarkText, 29, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(slotText.rectTransform);
        Button next = CreateButton("Next", slotPanel.transform, "›", Color.clear, DarkText, () => ChangeSlot(1));
        SetAnchors((RectTransform)next.transform, new Vector2(0.80f, 0), new Vector2(1, 1));

        rotateButton = CreateButton("Rotate", controls, "↗", Color.clear, Green, ToggleOrientation);
        landscapePageTitle = CreateText("LandscapeTitle", controls, "温度热力图", Green, 30, FontStyles.Bold, TextAlignmentOptions.Center);
        pointToggleButton = CreateButton("PointLabels", controls, string.Empty, Color.clear, DarkText, TogglePointLabels);
        pointToggleText = CreateText("Label", pointToggleButton.transform, "☑ 显示测点", DarkText, 22,
            FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(pointToggleText.rectTransform);
    }

    private void BuildSideRail()
    {
        sideRail = CreatePanel("LandscapeSideRail", root, new Color32(28, 31, 33, 255)).rectTransform;
        sideDataButton = CreateButton("Data", sideRail, "数", Color.clear, Color.white, () => ShowPage(SensorPage.TemperatureData));
        sideHeatMapButton = CreateButton("HeatMap", sideRail, "热", Color.clear, Color.white, () => ShowPage(SensorPage.HeatMap));
        Button backButton = CreateButton("Back", sideRail, "返", Color.clear, Color.white, ReturnToMainScene);

        SetAnchors((RectTransform)sideDataButton.transform, new Vector2(0.10f, 0.78f), new Vector2(0.90f, 0.95f));
        SetAnchors((RectTransform)sideHeatMapButton.transform, new Vector2(0.10f, 0.58f), new Vector2(0.90f, 0.75f));
        SetAnchors((RectTransform)backButton.transform, new Vector2(0.10f, 0.38f), new Vector2(0.90f, 0.55f));
    }

    private void BuildHeatMapPage()
    {
        heatMapPage = CreatePanel("HeatMapPage", root, HeatMapBackground).rectTransform;
        heatMapScroll = heatMapPage.gameObject.AddComponent<ScrollRect>();
        heatMapScroll.horizontal = true;
        heatMapScroll.vertical = true;
        heatMapScroll.movementType = ScrollRect.MovementType.Clamped;
        heatMapScroll.inertia = true;
        heatMapScroll.decelerationRate = 0.08f;

        heatMapViewport = CreatePanel("Viewport", heatMapPage, HeatMapBackground).rectTransform;
        Stretch(heatMapViewport);
        heatMapViewport.GetComponent<Image>().raycastTarget = true;
        heatMapViewport.gameObject.AddComponent<RectMask2D>();

        heatMapContent = CreateRect("Content", heatMapViewport);
        heatMapContent.anchorMin = new Vector2(0, 1);
        heatMapContent.anchorMax = new Vector2(0, 1);
        heatMapContent.pivot = new Vector2(0, 1);

        Image whiteCanvas = CreatePanel("ChamberCanvas", heatMapContent, Color.white);
        Stretch(whiteCanvas.rectTransform);
        SensorPotGraphic pot = new GameObject("ChamberOutline", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(SensorPotGraphic)).GetComponent<SensorPotGraphic>();
        pot.transform.SetParent(heatMapContent, false);
        pot.raycastTarget = false;
        Stretch((RectTransform)pot.transform);

        heatMapScroll.viewport = heatMapViewport;
        heatMapScroll.content = heatMapContent;

        Transform oldHeatMap = sceneCanvas.transform.Find("TableBackImage");
        if (oldHeatMap != null)
        {
            legacyHeatMap = oldHeatMap as RectTransform;
            legacyHeatMap.SetParent(heatMapContent, false);
            legacyHeatMap.anchorMin = new Vector2(0, 1);
            legacyHeatMap.anchorMax = new Vector2(0, 1);
            legacyHeatMap.pivot = new Vector2(0, 1);
            legacyHeatMap.anchoredPosition = Vector2.zero;
            legacyHeatMap.localRotation = Quaternion.identity;
        }

        BuildHeatMapLegend();
        heatMapSummary = CreatePanel("HeatMapSummary", heatMapPage, Color.black).rectTransform;
        heatMapSummaryText = CreateText("Summary", heatMapSummary, "测点数据加载中…",
            new Color32(107, 232, 139, 255), 24, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(heatMapSummaryText.rectTransform);
    }

    private void BuildHeatMapLegend()
    {
        heatMapLegend = CreatePanel("TemperatureLegend", heatMapPage, Color.white).rectTransform;
        heatMapLegendTitle = CreateText("Title", heatMapLegend, "温度 ℃", DarkText, 19,
            FontStyles.Bold, TextAlignmentOptions.Center);

        heatMapScale = CreateRect("ColorScale", heatMapLegend);
        for (int i = 0; i < 35; i++)
        {
            float low = i / 35f;
            float high = (i + 1) / 35f;
            Color color = Color.Lerp(new Color32(20, 58, 204, 255), new Color32(27, 182, 38, 255), Mathf.Clamp01(low * 2f));
            if (low >= 0.5f) color = Color.Lerp(new Color32(27, 182, 38, 255), new Color32(251, 233, 15, 255), (low - 0.5f) * 2f);
            if (low >= 0.72f) color = Color.Lerp(new Color32(251, 233, 15, 255), new Color32(237, 37, 23, 255), (low - 0.72f) / 0.28f);
            Image segment = CreatePanel($"Segment{i}", heatMapScale, color);
            SetAnchors(segment.rectTransform, new Vector2(0, low), new Vector2(1, high));
            heatMapScaleSegments.Add(segment.rectTransform);
        }

        string[] ticks = { "50", "100", "150", "200", "250", "300", "350", "400" };
        for (int i = 0; i < ticks.Length; i++)
        {
            Text label = CreateText($"Tick{i}", heatMapLegend, ticks[i], DarkText, 18,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            heatMapLegendLabels.Add(label);
        }

        heatMapLegend.SetAsLastSibling();
    }

    private void BuildTemperaturePage()
    {
        temperaturePage = CreatePanel("TemperatureDataPage", root, PageBackground).rectTransform;
        categoryBar = CreatePanel("CategoryBar", temperaturePage, PageBackground).rectTransform;

        CreateCategoryButton(phoneTableScript.TemperatureSensorCategory.Cathode, "阴极", 0f, 0.22f);
        CreateCategoryButton(phoneTableScript.TemperatureSensorCategory.SteelWindow, "钢窗", 0.23f, 0.47f);
        CreateCategoryButton(phoneTableScript.TemperatureSensorCategory.EndAndBottom, "端底部", 0.48f, 0.78f);

        currentCategoryText = CreateText("CurrentCategory", categoryBar, string.Empty, Green, 44, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
        SetAnchors(currentCategoryText.rectTransform, new Vector2(0.78f, 0), new Vector2(0.98f, 1));
        currentCategoryText.gameObject.SetActive(false);

        Image viewportImage = CreatePanel("TableViewport", temperaturePage, Color.white);
        viewportImage.raycastTarget = true;
        tableViewport = viewportImage.rectTransform;
        tableViewport.gameObject.AddComponent<RectMask2D>();

        temperatureDataScroll = tableViewport.gameObject.AddComponent<ScrollRect>();
        temperatureDataScroll.horizontal = false;
        temperatureDataScroll.vertical = true;
        temperatureDataScroll.movementType = ScrollRect.MovementType.Clamped;
        temperatureDataScroll.inertia = true;

        tableContent = CreateRect("TableContent", tableViewport);
        tableContent.anchorMin = new Vector2(0, 1);
        tableContent.anchorMax = new Vector2(1, 1);
        tableContent.pivot = new Vector2(0.5f, 1);
        tableContent.anchoredPosition = Vector2.zero;

        temperatureTable = tableContent.gameObject.AddComponent<TableLayout>();
        temperatureTable.padding = new RectOffset(0, 0, 0, 0);
        temperatureTable.CellSpacing = 1;
        temperatureTable.CellPadding = new RectOffset(18, 18, 6, 6);
        temperatureTable.UseGlobalCellPadding = true;
        temperatureTable.AutoCalculateHeight = true;

        temperatureDataScroll.viewport = tableViewport;
        temperatureDataScroll.content = tableContent;
    }

    private void CreateCategoryButton(phoneTableScript.TemperatureSensorCategory category, string label, float left, float right)
    {
        Button button = CreateButton(category.ToString(), categoryBar, string.Empty, Color.clear, DarkText, () => SelectCategory(category));
        SetAnchors((RectTransform)button.transform, new Vector2(left, 0.08f), new Vector2(right, 0.92f));

        Text radio = CreateText("Radio", button.transform, "○", MutedText, 27, FontStyles.Bold, TextAlignmentOptions.Center);
        SetAnchors(radio.rectTransform, new Vector2(0, 0), new Vector2(0.24f, 1));
        Text text = CreateText("Label", button.transform, label, DarkText, 25, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        SetAnchors(text.rectTransform, new Vector2(0.24f, 0), new Vector2(1, 1));

        categoryButtons[category] = button;
        categoryTexts[category] = radio;
    }

    private void BindLegacyComponents()
    {
        heatMapDataSource.TemperatureReadingsUpdated += ApplyTemperatureSnapshot;

        if (workAreaSelector != null)
        {
            string selectedWorkArea = workAreaSelector.GetSelectedWorkArea();
            if (!string.IsNullOrWhiteSpace(selectedWorkArea))
                workAreaText.text = selectedWorkArea;

            workAreaSelector.OnWorkAreaConfirmedEvent ??= new UnityEvent<string>();
            workAreaSelector.OnWorkAreaConfirmedEvent.AddListener(OnWorkAreaConfirmed);
        }

        if (slotNumberSelector != null)
        {
            slotNumberSelector.OnSlotConfirmed ??= new UnityEvent<string>();
            slotNumberSelector.OnSlotConfirmed.AddListener(OnSlotConfirmed);
            string selectedSlot = slotNumberSelector.PrepareForWorkArea(workAreaText.text, slotText.text);
            if (IsUsableSlotNumber(selectedSlot))
            {
                slotText.text = selectedSlot;
                heatMapDataSource.SetSelectedSlotNumber(selectedSlot);
            }
        }

        UpdateTabVisuals();
        UpdateCategoryVisuals();
    }

    private void OpenWorkAreaSelector()
    {
        if (workAreaSelector == null)
            return;

        string preparedWorkArea = workAreaSelector.PrepareSelection(workAreaText.text);
        if (!string.IsNullOrWhiteSpace(preparedWorkArea))
            workAreaText.text = preparedWorkArea;

        WireSelectorButtons(workAreaSelector.cancelButton, workAreaSelector.CancelSelection,
            workAreaSelector.confirmButton, workAreaSelector.ConfirmSelection);
        PrepareSelectorPanel(workAreaSelector.selectorPanel, workAreaSelector.cancelButton,
            workAreaSelector.confirmButton, workAreaSelector.itemScrollRect);
        BeginSelectorInteraction(workAreaSelector.selectorPanel);
        workAreaSelector.OpenSelectorPanel();
        BringPanelToFront(workAreaSelector.selectorPanel);
    }

    private void OpenSlotSelector()
    {
        if (slotNumberSelector == null)
            return;

        string preparedSlot = slotNumberSelector.PrepareForWorkArea(workAreaText.text, slotText.text);
        if (IsUsableSlotNumber(preparedSlot))
            slotText.text = preparedSlot;

        WireSelectorButtons(slotNumberSelector.cancelButton, slotNumberSelector.CancelSelection,
            slotNumberSelector.confirmButton, slotNumberSelector.ConfirmSelection);
        PrepareSelectorPanel(slotNumberSelector.selectorPanel, slotNumberSelector.cancelButton,
            slotNumberSelector.confirmButton, slotNumberSelector.itemScrollRect);
        BeginSelectorInteraction(slotNumberSelector.selectorPanel);
        slotNumberSelector.OpenSelectorPanel();
        BringPanelToFront(slotNumberSelector.selectorPanel);
    }

    private void PrepareSelectorPanel(GameObject panel, Button cancelButton, Button confirmButton, ScrollRect scrollRect)
    {
        if (panel == null)
            return;

        bool portrait = Screen.height >= Screen.width;
        RectTransform panelRect = panel.transform as RectTransform;
        if (panelRect != null && panelRect.parent != root)
            panelRect.SetParent(root, false);

        if (portrait)
            SetAnchors(panelRect, Vector2.zero, new Vector2(1, 0.41f));
        else
            SetAnchors(panelRect, new Vector2(0.055f, 0), new Vector2(1, 0.58f));

        Image background = panel.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color32(226, 226, 226, 255);
            background.raycastTarget = true;
        }

        RectTransform headerRect = cancelButton != null ? cancelButton.transform.parent as RectTransform : null;
        if (headerRect != null)
            SetAnchors(headerRect, new Vector2(0, 0.84f), Vector2.one);

        if (cancelButton != null)
            SetAnchors(cancelButton.transform as RectTransform, Vector2.zero, new Vector2(0.25f, 1));
        if (confirmButton != null)
            SetAnchors(confirmButton.transform as RectTransform, new Vector2(0.75f, 0), Vector2.one);

        if (scrollRect != null)
        {
            SetAnchors(scrollRect.transform as RectTransform, Vector2.zero, new Vector2(1, 0.84f));
            Image scrollBackground = scrollRect.GetComponent<Image>();
            if (scrollBackground != null)
                scrollBackground.color = new Color32(232, 232, 232, 255);
        }

        EnsureSelectorButtonLabel(cancelButton, "取消");
        EnsureSelectorButtonLabel(confirmButton, "确定");
    }

    private void WireSelectorButtons(Button cancelButton, UnityAction cancelAction,
        Button confirmButton, UnityAction confirmAction)
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(cancelAction);
            cancelButton.onClick.AddListener(EndSelectorInteraction);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(confirmAction);
            confirmButton.onClick.AddListener(EndSelectorInteraction);
        }
    }

    private void BeginSelectorInteraction(GameObject activePanel)
    {
        GameObject workPanel = workAreaSelector != null ? workAreaSelector.selectorPanel : null;
        GameObject slotPanel = slotNumberSelector != null ? slotNumberSelector.selectorPanel : null;
        if (workPanel != null && workPanel != activePanel)
            workPanel.SetActive(false);
        if (slotPanel != null && slotPanel != activePanel)
            slotPanel.SetActive(false);

        SetUnderlyingScrollingEnabled(false);
    }

    private void EndSelectorInteraction()
    {
        SetUnderlyingScrollingEnabled(true);
    }

    private void SetUnderlyingScrollingEnabled(bool enabled)
    {
        if (heatMapScroll != null)
        {
            heatMapScroll.StopMovement();
            heatMapScroll.enabled = enabled;
        }

        if (temperatureDataScroll != null)
        {
            temperatureDataScroll.StopMovement();
            temperatureDataScroll.enabled = enabled;
        }
    }

    private void EnsureSelectorButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        button.interactable = true;
        if (button.targetGraphic != null)
        {
            button.targetGraphic.enabled = true;
            button.targetGraphic.raycastTarget = true;
            button.targetGraphic.color = new Color(1, 1, 1, 0.01f);
        }

        foreach (TextMeshProUGUI legacyLabel in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            legacyLabel.gameObject.SetActive(false);

        Transform existing = button.transform.Find("RuntimeLabel");
        Text text = existing != null ? existing.GetComponent<Text>() : null;
        if (text == null)
        {
            text = CreateText("RuntimeLabel", button.transform, label, Green, 28,
                FontStyles.Normal, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
        }
        else
        {
            text.text = label;
            text.color = Green;
        }
    }

    private static void BringPanelToFront(GameObject panel)
    {
        if (panel != null)
            panel.transform.SetAsLastSibling();
    }

    private void OnWorkAreaConfirmed(string selectedWorkArea)
    {
        workAreaText.text = selectedWorkArea;
        if (slotNumberSelector == null)
            return;

        string selectedSlot = slotNumberSelector.PrepareForWorkArea(selectedWorkArea, null);
        if (IsUsableSlotNumber(selectedSlot))
        {
            slotText.text = selectedSlot;
            heatMapDataSource.SetSelectedSlotNumber(selectedSlot);
        }
    }

    private void OnSlotConfirmed(string selectedSlot)
    {
        if (!IsUsableSlotNumber(selectedSlot))
            return;

        slotText.text = selectedSlot;
        heatMapDataSource.SetSelectedSlotNumber(selectedSlot);
    }

    private static bool IsUsableSlotNumber(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out _);
    }

    private void ChangeSlot(int offset)
    {
        if (slotNumberSelector != null && slotNumberSelector.SelectRelativeSlot(offset))
            slotText.text = slotNumberSelector.GetSelectedSlotNumber();
    }

    private void TogglePointLabels()
    {
        showPointLabels = !showPointLabels;
        pointToggleText.text = showPointLabels ? "☑ 显示测点" : "□ 显示测点";
        if (legacyHeatMap == null) return;
        foreach (Transform child in legacyHeatMap.GetComponentsInChildren<Transform>(true))
            if (child.name.EndsWith("UITable-pos", StringComparison.OrdinalIgnoreCase))
                child.gameObject.SetActive(showPointLabels);
    }

    private void ReturnToMainScene()
    {
        SceneLoader.currentSelectedScene = "MainScene";
        SceneManager.LoadScene("MainScene");
    }

    private void ToggleOrientation()
    {
        if (Screen.height >= Screen.width)
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        else
            Screen.orientation = ScreenOrientation.Portrait;
    }

    private void ShowPage(SensorPage page)
    {
        currentPage = page;
        heatMapPage.gameObject.SetActive(page == SensorPage.HeatMap);
        temperaturePage.gameObject.SetActive(page == SensorPage.TemperatureData);
        landscapePageTitle.text = page == SensorPage.HeatMap ? "温度热力图" : "温度数据";
        rotateButton.gameObject.SetActive(page == SensorPage.HeatMap);
        UpdateTabVisuals();
        ApplyResponsiveLayout(false);
    }

    private void UpdateTabVisuals()
    {
        bool dataSelected = currentPage == SensorPage.TemperatureData;
        if (dataTabText != null) dataTabText.color = dataSelected ? Green : MutedText;
        if (heatMapTabText != null) heatMapTabText.color = dataSelected ? MutedText : Green;
        if (dataUnderline != null) dataUnderline.gameObject.SetActive(dataSelected);
        if (heatMapUnderline != null) heatMapUnderline.gameObject.SetActive(!dataSelected);

        SetButtonLabelColor(sideDataButton, dataSelected ? new Color32(190, 255, 224, 255) : Color.white);
        SetButtonLabelColor(sideHeatMapButton, dataSelected ? Color.white : new Color32(190, 255, 224, 255));
    }

    private void SelectCategory(phoneTableScript.TemperatureSensorCategory category)
    {
        currentCategory = category;
        UpdateCategoryVisuals();
        RebuildTemperatureTable();
    }

    private void UpdateCategoryVisuals()
    {
        foreach (var pair in categoryTexts)
        {
            bool selected = pair.Key == currentCategory;
            pair.Value.text = selected ? "●" : "○";
            pair.Value.color = selected ? Green : MutedText;
        }

        if (currentCategoryText != null)
        {
            currentCategoryText.text = currentCategory switch
            {
                phoneTableScript.TemperatureSensorCategory.Cathode => "阴极温度",
                phoneTableScript.TemperatureSensorCategory.SteelWindow => "钢窗温度",
                _ => "端底部温度"
            };
        }
    }

    private void ApplyTemperatureSnapshot(IReadOnlyList<phoneTableScript.TemperatureSensorReading> readings)
    {
        latestReadings.Clear();
        if (readings != null)
            latestReadings.AddRange(readings);
        if (heatMapSummaryText != null)
        {
            var values = latestReadings.Select(r => r.temperature)
                .Where(value => !float.IsNaN(value) && !float.IsInfinity(value)).ToList();
            heatMapSummaryText.text = values.Count == 0 ? "暂无测点温度数据"
                : $"测点 {values.Count}    平均温度 {values.Average():0} ℃    最高温度 {values.Max():0} ℃";
        }
        RebuildTemperatureTable();
    }

    private void RebuildTemperatureTable()
    {
        if (temperatureTable == null)
            return;

        foreach (TableRow oldRow in temperatureTable.Rows)
        {
            oldRow.gameObject.SetActive(false);
            Destroy(oldRow.gameObject);
        }

        bool portrait = Screen.height >= Screen.width;
        float rowHeight = portrait ? 92f : 48f;
        float fontSize = portrait ? 30f : 20f;

        AddTableRow(new[] { "测温点", "温度", "测温点", "温度" }, true, 0, rowHeight, fontSize);

        List<ReadingPair> pairs = BuildReadingPairs();
        for (int i = 0; i < pairs.Count; i++)
        {
            ReadingPair pair = pairs[i];
            AddTableRow(new[]
            {
                pair.leftName,
                pair.leftValue,
                pair.rightName,
                pair.rightValue
            }, false, i, rowHeight, fontSize);
        }

        float width = Mathf.Max(100f, tableViewport != null ? tableViewport.rect.width : root.rect.width * 0.92f);
        temperatureTable.ColumnWidths = new List<float> { width * 0.28f, width * 0.22f, width * 0.28f, width * 0.22f };
        tableContent.sizeDelta = new Vector2(0, rowHeight * (pairs.Count + 1));
        temperatureTable.UpdateLayout();
    }

    private List<ReadingPair> BuildReadingPairs()
    {
        List<phoneTableScript.TemperatureSensorReading> filtered = latestReadings
            .Where(reading => reading.category == currentCategory)
            .ToList();

        if (filtered.Count == 0)
            return BuildPlaceholderPairs();

        List<phoneTableScript.TemperatureSensorReading> left = filtered
            .Where(IsLeftReading)
            .OrderBy(reading => reading.positionIndex)
            .ThenBy(reading => reading.directionIndex)
            .ThenBy(reading => reading.nodeIndex)
            .ToList();
        List<phoneTableScript.TemperatureSensorReading> right = filtered
            .Where(reading => !IsLeftReading(reading))
            .OrderBy(reading => reading.positionIndex)
            .ThenBy(reading => reading.directionIndex)
            .ThenBy(reading => reading.nodeIndex)
            .ToList();

        int rowCount = Mathf.Max(left.Count, right.Count);
        List<ReadingPair> pairs = new(rowCount);
        for (int i = 0; i < rowCount; i++)
        {
            phoneTableScript.TemperatureSensorReading leftReading = i < left.Count ? left[i] : null;
            phoneTableScript.TemperatureSensorReading rightReading = i < right.Count ? right[i] : null;
            pairs.Add(new ReadingPair
            {
                leftName = leftReading?.displayName ?? string.Empty,
                leftValue = FormatTemperature(leftReading),
                rightName = rightReading?.displayName ?? string.Empty,
                rightValue = FormatTemperature(rightReading)
            });
        }
        return pairs;
    }

    private bool IsLeftReading(phoneTableScript.TemperatureSensorReading reading)
    {
        if (reading.directionType == 1) return true;
        if (reading.directionType == 2) return false;
        return reading.positionIndex == 3;
    }

    private List<ReadingPair> BuildPlaceholderPairs()
    {
        List<ReadingPair> pairs = new();
        if (currentCategory == phoneTableScript.TemperatureSensorCategory.Cathode)
        {
            for (int i = 1; i <= 28; i++)
                pairs.Add(new ReadingPair { leftName = $"A{i}-1", leftValue = "—", rightName = $"B{i}-1", rightValue = "—" });
        }
        else if (currentCategory == phoneTableScript.TemperatureSensorCategory.SteelWindow)
        {
            for (int i = 1; i <= 28; i++)
                pairs.Add(new ReadingPair { leftName = $"WA{i}", leftValue = "—", rightName = $"WB{i}", rightValue = "—" });
        }
        else
        {
            for (int i = 1; i <= 9; i++)
                pairs.Add(new ReadingPair { leftName = $"G{i}", leftValue = "—", rightName = $"T{i}", rightValue = "—" });
            for (int i = 1; i <= 14; i++)
                pairs.Add(new ReadingPair { leftName = $"BA{i}", leftValue = "—", rightName = $"BB{i}", rightValue = "—" });
        }
        return pairs;
    }

    private static string FormatTemperature(phoneTableScript.TemperatureSensorReading reading)
    {
        if (reading == null) return string.Empty;
        return Mathf.Approximately(reading.temperature, Mathf.Round(reading.temperature))
            ? Mathf.RoundToInt(reading.temperature).ToString()
            : reading.temperature.ToString("0.0");
    }

    private void AddTableRow(string[] values, bool headerRow, int rowIndex, float rowHeight, float fontSize)
    {
        TableRow row = temperatureTable.AddRow(values.Length);
        row.preferredHeight = rowHeight;
        row.dontUseTableRowBackground = true;
        row.image.color = headerRow ? TableHeader : (rowIndex % 2 == 0 ? Color.white : TableAlternate);

        List<TableCell> cells = row.Cells;
        for (int i = 0; i < cells.Count; i++)
        {
            TableCell cell = cells[i];
            cell.image.color = new Color(1, 1, 1, 0);
            Text text = CreateText("Value", cell.transform, values[i], DarkText, fontSize,
                headerRow ? FontStyles.Bold : FontStyles.Normal,
                i % 2 == 0 ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center);
            if (i % 2 == 0)
                SetAnchors(text.rectTransform, new Vector2(0.06f, 0), new Vector2(0.94f, 1));
            else
                Stretch(text.rectTransform);
        }
    }

    private void ApplyResponsiveLayout(bool rebuildTable)
    {
        if (root == null)
            return;

        bool portrait = Screen.height >= Screen.width;
        bool orientationChanged = portrait != lastPortrait;
        lastPortrait = portrait;
        lastCanvasSize = root.rect.size;

        header.gameObject.SetActive(portrait);
        tabs.gameObject.SetActive(portrait);
        sideRail.gameObject.SetActive(!portrait);

        if (portrait)
        {
            SetAnchors(header, new Vector2(0, 0.89f), new Vector2(1, 1));
            SetAnchors(tabs, new Vector2(0, 0.82f), new Vector2(1, 0.89f));
            SetAnchors(controls, new Vector2(0, 0.755f), new Vector2(1, 0.82f));
            SetAnchors(heatMapPage, new Vector2(0, 0), new Vector2(1, 0.755f));
            SetAnchors(temperaturePage, new Vector2(0, 0), new Vector2(1, 0.755f));

            bool showRotate = currentPage == SensorPage.HeatMap;
            rotateButton.gameObject.SetActive(showRotate);
            pointToggleButton.gameObject.SetActive(false);
            landscapePageTitle.gameObject.SetActive(false);
            if (showRotate)
            {
                SetAnchors(FindRect(controls, "WorkArea"), new Vector2(0.04f, 0.16f), new Vector2(0.43f, 0.84f));
                SetAnchors(FindRect(controls, "SlotSelector"), new Vector2(0.46f, 0.16f), new Vector2(0.83f, 0.84f));
                SetAnchors((RectTransform)rotateButton.transform, new Vector2(0.86f, 0.14f), new Vector2(0.97f, 0.86f));
            }
            else
            {
                SetAnchors(FindRect(controls, "WorkArea"), new Vector2(0.04f, 0.16f), new Vector2(0.49f, 0.84f));
                SetAnchors(FindRect(controls, "SlotSelector"), new Vector2(0.53f, 0.16f), new Vector2(0.96f, 0.84f));
            }

            SetAnchors(categoryBar, new Vector2(0.03f, 0.90f), new Vector2(0.97f, 1));
            SetAnchors(tableViewport, new Vector2(0.04f, 0.015f), new Vector2(0.96f, 0.90f));
        }
        else
        {
            SetAnchors(sideRail, new Vector2(0, 0), new Vector2(0.055f, 1));
            SetAnchors(controls, new Vector2(0.055f, 0.89f), new Vector2(1, 1));
            SetAnchors(heatMapPage, new Vector2(0.055f, 0), new Vector2(1, 0.89f));
            SetAnchors(temperaturePage, new Vector2(0.055f, 0), new Vector2(1, 0.89f));

            landscapePageTitle.gameObject.SetActive(true);
            rotateButton.gameObject.SetActive(true);
            pointToggleButton.gameObject.SetActive(currentPage == SensorPage.HeatMap);
            SetAnchors(FindRect(controls, "WorkArea"), new Vector2(0.02f, 0.15f), new Vector2(0.20f, 0.85f));
            SetAnchors(FindRect(controls, "SlotSelector"), new Vector2(0.215f, 0.15f), new Vector2(0.38f, 0.85f));
            SetAnchors(landscapePageTitle.rectTransform, new Vector2(0.40f, 0), new Vector2(0.72f, 1));
            SetAnchors((RectTransform)pointToggleButton.transform, new Vector2(0.73f, 0.10f), new Vector2(0.91f, 0.90f));
            SetAnchors((RectTransform)rotateButton.transform, new Vector2(0.92f, 0.12f), new Vector2(0.985f, 0.88f));

            SetAnchors(categoryBar, new Vector2(0.02f, 0.89f), new Vector2(0.54f, 0.99f));
            SetAnchors(tableViewport, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.88f));
        }

        UpdateTypography(portrait);
        LayoutHeatMapLegend(portrait);
        Canvas.ForceUpdateCanvases();
        LayoutHeatMap(portrait, orientationChanged);

        if (rebuildTable)
            RebuildTemperatureTable();
    }

    private void UpdateTypography(bool portrait)
    {
        SetFontSize(headerTitle, portrait ? 40 : 26);
        SetFontSize(workAreaText, portrait ? 29 : 22);
        SetFontSize(slotText, portrait ? 29 : 22);
        SetFontSize(landscapePageTitle, portrait ? 30 : 26);
        SetFontSize(dataTabText, portrait ? 32 : 24);
        SetFontSize(heatMapTabText, portrait ? 32 : 24);
        SetFontSize(heatMapSummaryText, portrait ? 20 : 24);
    }

    private void LayoutHeatMapLegend(bool portrait)
    {
        if (heatMapViewport == null || heatMapLegend == null)
            return;

        if (portrait)
        {
            SetAnchors(heatMapViewport, new Vector2(0, 0.06f), new Vector2(1, 0.88f));
            SetAnchors(heatMapLegend, new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.995f));
            SetAnchors(heatMapSummary, new Vector2(0, 0), new Vector2(1, 0.06f));
            SetAnchors(heatMapLegendTitle.rectTransform, new Vector2(0, 0.62f), Vector2.one);
            SetAnchors(heatMapScale, new Vector2(0.03f, 0.34f), new Vector2(0.97f, 0.55f));
            for (int i = 0; i < heatMapScaleSegments.Count; i++)
                SetAnchors(heatMapScaleSegments[i], new Vector2(i / 35f, 0), new Vector2((i + 1) / 35f, 1));
            for (int i = 0; i < heatMapLegendLabels.Count; i++)
            {
                float left = 0.03f + i * 0.94f / 8f;
                SetAnchors(heatMapLegendLabels[i].rectTransform,
                    new Vector2(left, 0.02f), new Vector2(left + 0.94f / 8f, 0.32f));
                SetFontSize(heatMapLegendLabels[i], 14);
            }
        }
        else
        {
            SetAnchors(heatMapViewport, new Vector2(0.02f, 0.06f), new Vector2(0.91f, 0.98f));
            SetAnchors(heatMapLegend, new Vector2(0.915f, 0.06f), new Vector2(0.995f, 0.98f));
            SetAnchors(heatMapSummary, new Vector2(0, 0), new Vector2(1, 0.06f));
            SetAnchors(heatMapLegendTitle.rectTransform, new Vector2(0, 0.92f), Vector2.one);
            SetAnchors(heatMapScale, new Vector2(0.08f, 0.035f), new Vector2(0.29f, 0.91f));
            for (int i = 0; i < heatMapScaleSegments.Count; i++)
                SetAnchors(heatMapScaleSegments[i], new Vector2(0, i / 35f), new Vector2(1, (i + 1) / 35f));
            float itemHeight = 0.875f / heatMapLegendLabels.Count;
            for (int i = 0; i < heatMapLegendLabels.Count; i++)
            {
                float bottom = 0.035f + itemHeight * i;
                SetAnchors(heatMapLegendLabels[i].rectTransform, new Vector2(0.38f, bottom), new Vector2(1, bottom + itemHeight));
                SetFontSize(heatMapLegendLabels[i], 16);
            }
            SetFontSize(heatMapLegendTitle, 17);
        }
    }

    private void LayoutHeatMap(bool portrait, bool resetScrollPosition)
    {
        if (legacyHeatMap == null || heatMapViewport == null)
            return;

        float sourceWidth = Mathf.Max(1, legacyHeatMap.sizeDelta.x);
        float sourceHeight = Mathf.Max(1, legacyHeatMap.sizeDelta.y);
        if (sourceWidth < 100) sourceWidth = 2160;
        if (sourceHeight < 100) sourceHeight = 1000;

        float fitWidth = heatMapViewport.rect.width / sourceWidth;
        float fitHeight = heatMapViewport.rect.height / sourceHeight;
        float scale;
        if (portrait)
        {
            // Leave a modest horizontal crop for readability without the
            // excessive fixed portrait enlargement used previously.
            scale = Mathf.Clamp(fitWidth * 1.35f, 0.75f, 1.55f);
        }
        else
        {
            scale = Mathf.Min(fitWidth, fitHeight);
            scale = Mathf.Max(0.1f, scale);
        }

        legacyHeatMap.sizeDelta = new Vector2(sourceWidth, sourceHeight);
        legacyHeatMap.localScale = Vector3.one * scale;
        legacyHeatMap.anchoredPosition = Vector2.zero;

        heatMapContent.sizeDelta = new Vector2(sourceWidth * scale, sourceHeight * scale);
        heatMapContent.anchoredPosition = Vector2.zero;

        if (resetScrollPosition && heatMapScroll != null)
        {
            heatMapScroll.StopMovement();
            heatMapScroll.horizontalNormalizedPosition = portrait ? 0f : 0.5f;
            heatMapScroll.verticalNormalizedPosition = 1f;
        }
    }

    private static RectTransform FindRect(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child as RectTransform : null;
    }

    private Image CreatePanel(string name, Transform parent, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private Button CreateButton(string name, Transform parent, string label, Color background, Color foreground, UnityAction action)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);

        Image image = gameObject.GetComponent<Image>();
        image.color = background;
        Button button = gameObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.93f, 0.96f, 0.95f, Mathf.Max(background.a, 0.25f));
        colors.pressedColor = new Color(0.82f, 0.90f, 0.87f, Mathf.Max(background.a, 0.4f));
        button.colors = colors;
        if (action != null)
            button.onClick.AddListener(action);

        if (!string.IsNullOrEmpty(label))
        {
            Text text = CreateText("Label", button.transform, label, foreground, 30, FontStyles.Normal, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
        }
        return button;
    }

    private Text CreateText(string name, Transform parent, string value, Color color, float fontSize,
        FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        Text text = gameObject.GetComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.color = color;
        text.fontSize = Mathf.RoundToInt(fontSize);
        text.fontStyle = (style & FontStyles.Bold) != 0 ? FontStyle.Bold : FontStyle.Normal;
        text.alignment = ToTextAnchor(alignment);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.resizeTextForBestFit = false;
        text.raycastTarget = false;
        return text;
    }

    private static TextAnchor ToTextAnchor(TextAlignmentOptions alignment)
    {
        if (alignment == TextAlignmentOptions.MidlineLeft)
            return TextAnchor.MiddleLeft;
        if (alignment == TextAlignmentOptions.MidlineRight)
            return TextAnchor.MiddleRight;
        return TextAnchor.MiddleCenter;
    }

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, Vector2.zero, Vector2.one);
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        if (rect == null) return;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetFontSize(Text text, float size)
    {
        if (text == null) return;
        text.fontSize = Mathf.RoundToInt(size);
    }

    private static void SetButtonLabelColor(Button button, Color color)
    {
        if (button == null) return;
        Text label = button.GetComponentInChildren<Text>();
        if (label != null) label.color = color;
    }

    private sealed class ReadingPair
    {
        public string leftName;
        public string leftValue;
        public string rightName;
        public string rightValue;
    }
}
