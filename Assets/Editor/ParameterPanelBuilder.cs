using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using System.Collections.Generic;
using InstrumentAnimation;

/// <summary>
/// 在 SampleScene 中一键构建溶出仪参数面板 UI。
/// 使用方式：菜单 Tools / Build Parameter Panel。
/// 面板为 Screen Space Overlay 的半透明悬浮窗，默认隐藏，点击仪器 MainScreen 后弹出。
/// </summary>
public class ParameterPanelBuilder
{
    [MenuItem("Tools/Build Parameter Panel")]
    public static void Build()
    {
        // 清理旧面板，避免重复叠加（含失活对象，GameObject.Find 无法找到失活对象）
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (var c in canvases)
        {
            if (c.name == "ParameterPanelCanvas" && c.gameObject.scene.IsValid())
                Object.DestroyImmediate(c.gameObject);
        }

        ExperimentConfig.EnsureExists();
        CreateEventSystem();

        GameObject mainScreen = SetupMainScreen();
        GameObject canvas = CreateCanvas();
        GameObject root = CreateRootWindow(canvas);
        var panel = root.GetComponent<ParameterPanel>();

        if (mainScreen != null)
        {
            var clicker = mainScreen.GetComponent<MainScreenClicker>();
            if (clicker == null) clicker = mainScreen.AddComponent<MainScreenClicker>();
            clicker.parameterPanel = root;
        }

        // 主菜单
        GameObject mainMenu = CreateMainMenu(root);
        panel.mainMenu = mainMenu;

        // 子面板
        GameObject runPanel = CreateRunPanel(root);
        GameObject samplingTimePanel = CreateSamplingTimePanel(root);
        GameObject cupA = CreateCupTimePanel(root, "A", 0);
        GameObject cupB = CreateCupTimePanel(root, "B", 1);
        GameObject cupC = CreateCupTimePanel(root, "C", 2);
        GameObject cupD = CreateCupTimePanel(root, "D", 3);
        GameObject samplingPosPanel = CreateSamplingPositionPanel(root);
        GameObject tempPanel = CreateTemperaturePanel(root);
        GameObject heatPanel = CreateHeatingPanel(root);
        GameObject preheatPanel = CreatePreheatPanel(root);
        GameObject speedPanel = CreateSpeedPanel(root);
        GameObject languagePanel = CreateLanguagePanel(root);
        GameObject helpPanel = CreateHelpPanel(root);
        GameObject helpRun = CreateHelpDetail(root, "运行模式说明",
            "运行模式下四个溶杯同时进行溶出实验。点击“开始”后搅拌桨按设定转速旋转，仪器按取样点时间依次取样；全部取样点完成后才允许停止。");
        GameObject helpSample = CreateHelpDetail(root, "取样点说明",
            "每个溶杯最多可填写 10 个取样时间点，时间必须按从小到大顺序填写，不能出现后一个时间点小于前一个时间点。");
        GameObject helpSpeed = CreateHelpDetail(root, "转速设置说明",
            "AB 杯与 CD 杯分别设置初始转速与末端转速。转速在相邻取样点之间线性过渡：第一个取样点之前保持初始转速，最后一个取样点之后保持末端转速。");
        GameObject helpTemp = CreateHelpDetail(root, "温度设置说明",
            "目标温度 37℃，允许误差 ±0.5℃。加热或降温完成后若超出 36.5℃～37.5℃ 范围，会给出提示。");
        GameObject helpPrint = CreateHelpDetail(root, "打印说明",
            "实验结束后点击“打印”可生成本次实验的纸条，记录温度、转速、取样点时间与各杯最终溶出度。");
        GameObject helpInfo = CreateHelpDetail(root, "设备信息",
            "溶出度测试仪（桨法）。4 个溶杯，常用介质体积 900 mL，支持 200 / 500 / 900 mL 取样位置。");

        panel.subPanels.Clear();
        panel.subPanels.AddRange(new[]
        {
            runPanel, samplingTimePanel, cupA, cupB, cupC, cupD,
            samplingPosPanel, tempPanel, heatPanel, preheatPanel,
            speedPanel, languagePanel, helpPanel,
            helpRun, helpSample, helpSpeed, helpTemp, helpPrint, helpInfo
        });

        // 主菜单顶部导航
        WireOpenButton(mainMenu, "运行Button", panel, runPanel);
        WireOpenButton(mainMenu, "取样点Button", panel, samplingTimePanel);
        WireOpenButton(mainMenu, "语言Button", panel, languagePanel);
        WireOpenButton(mainMenu, "说明Button", panel, helpPanel);

        // 左右转速卡片点击进入转速设置
        WireOpenButton(mainMenu, "LCard", panel, speedPanel);
        WireOpenButton(mainMenu, "RCard", panel, speedPanel);

        // 底部功能按钮
        var ctrl = GetController();
        WireOpenButton(mainMenu, "取样位置Button", panel, samplingPosPanel);
        WireActionButton(mainMenu, "机头上升Button", ctrl.HeadUp);
        WireActionButton(mainMenu, "机头下降Button", ctrl.HeadDown);
        WireOpenButton(mainMenu, "温度Button", panel, tempPanel);
        WireOpenButton(mainMenu, "加热Button", panel, heatPanel);
        WireOpenButton(mainMenu, "预加热Button", panel, preheatPanel);

        // 取样点时间 -> 各杯时间面板
        WireOpenButton(samplingTimePanel, "CupA", panel, cupA);
        WireOpenButton(samplingTimePanel, "CupB", panel, cupB);
        WireOpenButton(samplingTimePanel, "CupC", panel, cupC);
        WireOpenButton(samplingTimePanel, "CupD", panel, cupD);

        // 说明 -> 各说明详情
        WireOpenButton(helpPanel, "运行模式说明Button", panel, helpRun);
        WireOpenButton(helpPanel, "取样点说明Button", panel, helpSample);
        WireOpenButton(helpPanel, "转速设置说明Button", panel, helpSpeed);
        WireOpenButton(helpPanel, "温度设置说明Button", panel, helpTemp);
        WireOpenButton(helpPanel, "打印说明Button", panel, helpPrint);
        WireOpenButton(helpPanel, "设备信息Button", panel, helpInfo);

        SetupHeadGroup();

        EditorUtility.SetDirty(root);
        Debug.Log("Parameter panel built.");
    }

    // ------------------------------------------------------------------
    // 环境准备
    // ------------------------------------------------------------------
    static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static GameObject SetupMainScreen()
    {
        GameObject ms = GameObject.Find("MainScreen");
        if (ms == null) return null;
        if (ms.GetComponent<Collider>() == null) ms.AddComponent<BoxCollider>();
        if (ms.GetComponent<MainScreenClicker>() == null) ms.AddComponent<MainScreenClicker>();
        return ms;
    }

    static DissolutionTesterController GetController()
    {
        var app = GameObject.Find("Dissolution_Apparatus");
        if (app == null) return null;
        var c = app.GetComponent<DissolutionTesterController>();
        if (c == null) c = app.AddComponent<DissolutionTesterController>();
        return c;
    }

    static void SetupHeadGroup()
    {
        var app = GameObject.Find("Dissolution_Apparatus");
        if (app == null) return;
        var controller = app.GetComponent<DissolutionTesterController>();
        if (controller == null) controller = app.AddComponent<DissolutionTesterController>();

        // 已经建好且非空就不要再动
        if (controller.headGroup != null && controller.headGroup.childCount > 0) return;

        // 若已有空的 AutoHeadGroup，先清掉
        var old = app.transform.Find("AutoHeadGroup");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        float sumY = 0f; int count = 0;
        var children = new List<Transform>();
        foreach (Transform c in app.transform)
        {
            children.Add(c);
            sumY += c.position.y;
            count++;
        }
        if (count == 0) return;
        float avgY = sumY / count;

        var headGO = new GameObject("AutoHeadGroup");
        headGO.transform.SetParent(app.transform, false);
        foreach (var c in children)
        {
            if (c.position.y > avgY && c.name != "AutoHeadGroup" && c.name != "Box03")
                c.SetParent(headGO.transform, true);
        }

        controller.headGroup = headGO.transform;
        controller.headDownY = -4.78f;
        controller.headUpY = 0.5f;
        EditorUtility.SetDirty(app);
    }

    // ------------------------------------------------------------------
    // Canvas / 悬浮窗
    // ------------------------------------------------------------------
    static GameObject CreateCanvas()
    {
        var go = new GameObject("ParameterPanelCanvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static GameObject CreateRootWindow(GameObject canvas)
    {
        var go = CreatePanel(canvas.transform, "ParameterPanelRoot", new Color(0.93f, 0.96f, 0.99f, 0.96f));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1560, 920);
        rt.anchoredPosition = Vector2.zero;
        go.AddComponent<ParameterPanel>();
        go.SetActive(false); // 默认隐藏，点击 MainScreen 弹出
        return go;
    }

    // ------------------------------------------------------------------
    // 主菜单
    // ------------------------------------------------------------------
    static GameObject CreateMainMenu(GameObject root)
    {
        var go = CreatePanel(root.transform, "MainMenuPanel", new Color(1, 1, 1, 0));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        CreateText(go.transform, "溶出度测试仪", 52, new Color(0.08f, 0.32f, 0.55f), new Vector2(-650, 400), new Vector2(520, 80), TextAnchor.MiddleLeft);
        var close = CreateButton(go.transform, "关闭", new Vector2(650, 400), new Vector2(140, 60));
        var panel = root.GetComponent<ParameterPanel>();
        var bridge = close.AddComponent<PanelButtonBridge>();
        bridge.panel = panel;
        UnityEventTools.AddPersistentListener(close.GetComponent<Button>().onClick, bridge.ClosePanel);

        // 顶部导航
        string[] navs = { "运行", "取样点", "语言", "说明" };
        for (int i = 0; i < 4; i++)
            CreateButton(go.transform, navs[i], new Vector2(-540 + i * 360, 330), new Vector2(300, 80));

        // 左右转速卡片
        CreateSpeedCard(go.transform, "LCard", "L", new Vector2(-330, 60), 0, 560, 260);
        CreateSpeedCard(go.transform, "RCard", "R", new Vector2(330, 60), 1, 560, 260);

        // 底部功能按钮
        string[] bottoms = { "取样位置", "机头上升", "机头下降", "温度", "加热", "预加热" };
        for (int i = 0; i < 6; i++)
        {
            int row = i / 3;
            int col = i % 3;
            float x = -360 + col * 360;
            float y = -190 - row * 140;
            CreateButton(go.transform, bottoms[i], new Vector2(x, y), new Vector2(300, 110));
        }
        return go;
    }

    // ------------------------------------------------------------------
    // 运行面板
    // ------------------------------------------------------------------
    static GameObject CreateRunPanel(GameObject root)
    {
        var go = CreateSubPanel(root, "RunModePanel", "运行");

        var ctrl = GetController();
        string[] tops = { "开始", "停止", "打印", "返回" };
        for (int i = 0; i < 4; i++)
        {
            var b = CreateButton(go.transform, tops[i], new Vector2(-540 + i * 360, 390), new Vector2(300, 80));
            var btn = b.GetComponent<Button>();
            var panel = root.GetComponent<ParameterPanel>();
            if (tops[i] == "开始") UnityEventTools.AddPersistentListener(btn.onClick, ctrl.StartDissolution);
            if (tops[i] == "停止") UnityEventTools.AddPersistentListener(btn.onClick, ctrl.StopDissolution);
            if (tops[i] == "打印") UnityEventTools.AddPersistentListener(btn.onClick, ctrl.Print);
            if (tops[i] == "返回")
            {
                var bridge = b.AddComponent<PanelButtonBridge>();
                bridge.panel = panel;
                UnityEventTools.AddPersistentListener(btn.onClick, bridge.Back);
            }
        }

        var abText = CreateSpeedDisplay(go.transform, new Vector2(-330, 100), "AB杯", "A", "B");
        var cdText = CreateSpeedDisplay(go.transform, new Vector2(330, 100), "CD杯", "C", "D");

        CreateText(go.transform, "运行时间", 30, Color.black, new Vector2(-540, -250), new Vector2(220, 50), TextAnchor.MiddleLeft);
        var runTime = CreateText(go.transform, "0s", 36, new Color(0.08f, 0.32f, 0.55f), new Vector2(-350, -250), new Vector2(180, 50), TextAnchor.MiddleLeft);
        CreateText(go.transform, "取样时间", 30, Color.black, new Vector2(-120, -250), new Vector2(220, 50), TextAnchor.MiddleLeft);
        var sampleTime = CreateText(go.transform, "0s", 36, new Color(0.08f, 0.32f, 0.55f), new Vector2(70, -250), new Vector2(180, 50), TextAnchor.MiddleLeft);
        CreateText(go.transform, "取样倒计时", 30, Color.black, new Vector2(300, -250), new Vector2(220, 50), TextAnchor.MiddleLeft);
        var countdown = CreateText(go.transform, "0s", 36, new Color(0.08f, 0.32f, 0.55f), new Vector2(490, -250), new Vector2(180, 50), TextAnchor.MiddleLeft);

        var rc = go.AddComponent<RunPanelController>();
        rc.abSpeedText = abText;
        rc.cdSpeedText = cdText;
        rc.runTimeText = runTime.GetComponent<Text>();
        rc.sampleTimeText = sampleTime.GetComponent<Text>();
        rc.countdownText = countdown.GetComponent<Text>();
        return go;
    }

    static Text CreateSpeedDisplay(Transform parent, Vector2 pos, string title, string c1, string c2)
    {
        var go = CreatePanel(parent, title + "Display", new Color(0.95f, 0.96f, 0.98f));
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(600, 280);
        var header = CreatePanel(go.transform, "Header", new Color(0.24f, 0.52f, 0.8f));
        header.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 120);
        header.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 56);
        CreateText(header.transform, title + " 当前转速", 30, Color.white, Vector2.zero, new Vector2(600, 56), TextAnchor.MiddleCenter);
        var value = CreateText(go.transform, c1 + ": 0 rpm    " + c2 + ": 0 rpm", 34, Color.black, new Vector2(0, 20), new Vector2(560, 80), TextAnchor.MiddleCenter);
        return value.GetComponent<Text>();
    }

    // ------------------------------------------------------------------
    // 取样点时间（杯选择 + 每杯 10 个时间点）
    // ------------------------------------------------------------------
    static GameObject CreateSamplingTimePanel(GameObject root)
    {
        var go = CreateSubPanel(root, "SamplingTimePanel", "取样点时间");
        CreateText(go.transform, "点击杯子查看取样时间点", 32, Color.black, new Vector2(0, 300), new Vector2(800, 60), TextAnchor.MiddleCenter);
        string[] cups = { "A", "B", "C", "D" };
        for (int i = 0; i < 4; i++)
            CreateCupButton(go.transform, cups[i], new Vector2(-450 + i * 300, 0));
        return go;
    }

    static GameObject CreateCupTimePanel(GameObject root, string cup, int cupIndex)
    {
        var go = CreateSubPanel(root, "CupTimePanel" + cup, "杯子编号: " + cup);
        CreateText(go.transform, "10 个取样时间点（分钟）", 30, Color.black, new Vector2(0, 310), new Vector2(800, 50), TextAnchor.MiddleCenter);
        for (int i = 0; i < 10; i++)
        {
            int row = i / 5;
            int col = i % 5;
            float x = -480 + col * 240;
            float y = 170 - row * 190;
            CreateNumberSlot(go.transform, (i + 1).ToString(), cup, cupIndex, i, new Vector2(x, y));
        }
        return go;
    }

    static void CreateNumberSlot(Transform parent, string number, string cup, int cupIndex, int slot, Vector2 pos)
    {
        var cupIcon = CreatePanel(parent, "Cup" + cup + "_" + number, new Color(0.55f, 0.8f, 0.95f));
        cupIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos.x, pos.y + 55);
        cupIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 110);
        CreateText(cupIcon.transform, number, 36, Color.white, Vector2.zero, new Vector2(110, 80), TextAnchor.MiddleCenter);
        var bottom = CreatePanel(cupIcon.transform, "Bottom", new Color(0.1f, 0.35f, 0.5f));
        bottom.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -48);
        bottom.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 34);

        var input = CreateInputField(parent, new Vector2(pos.x, pos.y - 25), new Vector2(150, 54), "", 26);
        var cfg = ExperimentConfig.Instance;
        float init = cfg != null ? cfg.GetSampleTimes(cupIndex)[slot] : 0f;
        input.text = init > 0f ? init.ToString("0.#") : "";

        var binding = input.gameObject.AddComponent<ConfigFieldBinding>();
        binding.field = ConfigField.SampleTime;
        binding.cupIndex = cupIndex;
        binding.slotIndex = slot;
        UnityEventTools.AddPersistentListener<string>(input.onEndEdit, binding.SetFloatFromText);
    }

    // ------------------------------------------------------------------
    // 取样位置
    // ------------------------------------------------------------------
    static GameObject CreateSamplingPositionPanel(GameObject root)
    {
        var go = CreateSubPanel(root, "SamplingPositionPanel", "取样点位置");
        CreateText(go.transform, "根据介质容积选择对应取样点位置", 32, Color.black, new Vector2(0, 310), new Vector2(900, 60), TextAnchor.MiddleCenter);
        CreateVolumeColumn(go.transform, "AB杯", -300, 0);
        CreateVolumeColumn(go.transform, "CD杯", 300, 1);
        return go;
    }

    static void CreateVolumeColumn(Transform parent, string pairLabel, float x, int pairIndex)
    {
        var holder = new GameObject(pairLabel + "Group", typeof(RectTransform));
        holder.transform.SetParent(parent, false);
        var rt = holder.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, -20);
        rt.sizeDelta = new Vector2(260, 420);
        var group = holder.AddComponent<ToggleGroup>();
        group.allowSwitchOff = false;

        CreateText(holder.transform, pairLabel, 48, new Color(0.08f, 0.32f, 0.55f), new Vector2(0, 170), new Vector2(260, 70), TextAnchor.MiddleCenter);
        string[] vols = { "200", "500", "900" };
        for (int i = 0; i < 3; i++)
            CreateVolumeToggle(holder.transform, pairLabel, vols[i], new Vector2(0, 50 - i * 95), group, pairIndex);
    }

    static GameObject CreateVolumeToggle(Transform parent, string pairLabel, string vol, Vector2 pos, ToggleGroup group, int pairIndex)
    {
        var go = new GameObject(pairLabel + vol, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(220, 68);

        var toggle = go.AddComponent<Toggle>();
        toggle.group = group;

        var bg = CreatePanel(go.transform, "Background", new Color(0.92f, 0.94f, 0.97f));
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        toggle.targetGraphic = bg.GetComponent<Image>();

        var check = CreatePanel(go.transform, "Checkmark", new Color(0.22f, 0.56f, 0.86f, 0f));
        var chkRT = check.GetComponent<RectTransform>();
        chkRT.anchorMin = Vector2.zero; chkRT.anchorMax = Vector2.one;
        chkRT.offsetMin = new Vector2(6, 6); chkRT.offsetMax = new Vector2(-6, -6);
        toggle.graphic = check.GetComponent<Image>();

        CreateText(go.transform, vol + " mL", 28, new Color(0.15f, 0.25f, 0.35f), Vector2.zero, new Vector2(220, 68), TextAnchor.MiddleCenter);

        var binding = go.AddComponent<ConfigFieldBinding>();
        binding.field = ConfigField.SamplePosition;
        binding.cupIndex = pairIndex;
        binding.value = vol;
        UnityEventTools.AddPersistentListener<bool>(toggle.onValueChanged, binding.OnVolumeToggle);

        bool selected = ExperimentConfig.Instance != null && ExperimentConfig.Instance.GetSamplePosition(pairIndex) == vol;
        toggle.isOn = selected;
        return go;
    }

    // ------------------------------------------------------------------
    // 温度 / 加热 / 预加热 / 转速
    // ------------------------------------------------------------------
    static GameObject CreateTemperaturePanel(GameObject root)
    {
        var go = CreateSubPanel(root, "TemperaturePanel", "温度设置");
        CreateText(go.transform, "目标温度", 34, Color.black, new Vector2(0, 260), new Vector2(300, 60), TextAnchor.MiddleCenter);
        var display = CreateText(go.transform, "37.0°C", 96, new Color(0.08f, 0.32f, 0.55f), new Vector2(0, 90), new Vector2(500, 180), TextAnchor.MiddleCenter);
        var minus = CreateButton(go.transform, "-0.1", new Vector2(-140, -110), new Vector2(180, 80));
        var plus = CreateButton(go.transform, "+0.1", new Vector2(140, -110), new Vector2(180, 80));
        CreateText(go.transform, "校验范围 37.0 ± 0.5 ℃", 28, new Color(0.7f, 0.3f, 0.25f), new Vector2(0, -230), new Vector2(500, 50), TextAnchor.MiddleCenter);

        var b1 = minus.AddComponent<ConfigFieldBinding>();
        b1.field = ConfigField.TargetTemperature;
        b1.valueText = display.GetComponent<Text>();
        UnityEventTools.AddPersistentListener(minus.GetComponent<Button>().onClick, b1.TempDown);

        var b2 = plus.AddComponent<ConfigFieldBinding>();
        b2.field = ConfigField.TargetTemperature;
        b2.valueText = display.GetComponent<Text>();
        UnityEventTools.AddPersistentListener(plus.GetComponent<Button>().onClick, b2.TempUp);
        b2.RefreshValueText();
        return go;
    }

    static GameObject CreateHeatingPanel(GameObject root)
    {
        var go = CreateSubPanel(root, "HeatingPanel", "加热设置");
        CreateText(go.transform, "当前温度", 32, Color.black, new Vector2(-180, 190), new Vector2(240, 60), TextAnchor.MiddleCenter);
        CreateText(go.transform, "26.0°C", 54, new Color(0.08f, 0.32f, 0.55f), new Vector2(-180, 95), new Vector2(240, 90), TextAnchor.MiddleCenter);
        CreateText(go.transform, "目标温度", 32, Color.black, new Vector2(180, 190), new Vector2(240, 60), TextAnchor.MiddleCenter);
        CreateText(go.transform, "37.0°C", 54, new Color(0.08f, 0.32f, 0.55f), new Vector2(180, 95), new Vector2(240, 90), TextAnchor.MiddleCenter);

        var bar = CreatePanel(go.transform, "ProgressBar", new Color(0.85f, 0.88f, 0.92f));
        bar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        bar.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 40);
        var fill = CreatePanel(bar.transform, "Fill", new Color(0.24f, 0.58f, 0.88f));
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        var start = CreateButton(go.transform, "开始加热", new Vector2(-160, -190), new Vector2(240, 80));
        var stop = CreateButton(go.transform, "停止", new Vector2(160, -190), new Vector2(240, 80));
        var ctrl = GetController();
        UnityEventTools.AddPersistentListener(start.GetComponent<Button>().onClick, ctrl.StartHeating);
        UnityEventTools.AddPersistentListener(stop.GetComponent<Button>().onClick, ctrl.StopHeating);
        return go;
    }

    static GameObject CreatePreheatPanel(GameObject root)
    {
        var go = CreateSubPanel(root, "PreheatPanel", "预加热设置");
        string[] labels = { "当前日期", "当前时间", "设定日期", "设定时间" };
        string[] defaults = { "2026-10-20", "12:00:00", "2026-10-21", "08:00:00" };
        for (int i = 0; i < 4; i++)
        {
            CreateText(go.transform, labels[i], 30, Color.black, new Vector2(-260, 160 - i * 100), new Vector2(200, 50), TextAnchor.MiddleRight);
            CreateInputField(go.transform, new Vector2(120, 160 - i * 100), new Vector2(360, 54), defaults[i], 28);
        }
        CreateButton(go.transform, "应用", new Vector2(-160, -270), new Vector2(180, 70));
        CreateButton(go.transform, "取消应用", new Vector2(160, -270), new Vector2(180, 70));
        return go;
    }

    static GameObject CreateSpeedPanel(GameObject root)
    {
        var go = CreateSubPanel(root, "SpeedPanel", "转速设置");
        CreateSpeedCard(go.transform, "ABCard", "AB杯", new Vector2(0, 130), 0, 900, 180);
        CreateSpeedCard(go.transform, "CDCard", "CD杯", new Vector2(0, -130), 1, 900, 180);
        return go;
    }

    static GameObject CreateSpeedCard(Transform parent, string name, string label, Vector2 pos, int pairIndex, float width, float height)
    {
        var go = CreatePanel(parent, name, new Color(0.9f, 0.94f, 0.98f));
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, height);

        CreateText(go.transform, label, 64, Color.black, new Vector2(-width / 2f + 80, 0), new Vector2(140, height - 20), TextAnchor.MiddleCenter);

        CreateText(go.transform, "初始转速", 28, Color.black, new Vector2(-60, height / 2f - 65), new Vector2(140, 44), TextAnchor.MiddleLeft);
        var initInput = CreateInputField(go.transform, new Vector2(100, height / 2f - 65), new Vector2(140, 50), ExperimentConfig.Instance.GetInitialSpeed(pairIndex).ToString(), 28);
        CreateText(go.transform, "rpm", 28, Color.black, new Vector2(210, height / 2f - 65), new Vector2(60, 44), TextAnchor.MiddleLeft);

        CreateText(go.transform, "末端转速", 28, Color.black, new Vector2(-60, -height / 2f + 65), new Vector2(140, 44), TextAnchor.MiddleLeft);
        var endInput = CreateInputField(go.transform, new Vector2(100, -height / 2f + 65), new Vector2(140, 50), ExperimentConfig.Instance.GetEndSpeed(pairIndex).ToString(), 28);
        CreateText(go.transform, "rpm", 28, Color.black, new Vector2(210, -height / 2f + 65), new Vector2(60, 44), TextAnchor.MiddleLeft);

        var bi = initInput.gameObject.AddComponent<ConfigFieldBinding>();
        bi.field = pairIndex == 0 ? ConfigField.InitialSpeedAB : ConfigField.InitialSpeedCD;
        UnityEventTools.AddPersistentListener<string>(initInput.onEndEdit, bi.SetIntFromText);

        var be = endInput.gameObject.AddComponent<ConfigFieldBinding>();
        be.field = pairIndex == 0 ? ConfigField.EndSpeedAB : ConfigField.EndSpeedCD;
        UnityEventTools.AddPersistentListener<string>(endInput.onEndEdit, be.SetIntFromText);

        go.AddComponent<Button>();
        return go;
    }

    // ------------------------------------------------------------------
    // 语言 / 说明
    // ------------------------------------------------------------------
    static GameObject CreateLanguagePanel(GameObject root)
    {
        var go = CreateSubPanel(root, "LanguagePanel", "语言");
        CreateButton(go.transform, "简体中文", new Vector2(0, 80), new Vector2(480, 120));
        CreateButton(go.transform, "English", new Vector2(0, -120), new Vector2(480, 120));
        return go;
    }

    static GameObject CreateHelpPanel(GameObject root)
    {
        var go = CreateSubPanel(root, "HelpPanel", "说明");
        string[] labels = { "运行模式说明", "取样点说明", "转速设置说明", "温度设置说明", "打印说明", "设备信息" };
        for (int i = 0; i < 6; i++)
        {
            int row = i / 3;
            int col = i % 3;
            float x = -440 + col * 440;
            float y = 60 - row * 260;
            CreateSquareButton(go.transform, labels[i], new Vector2(x, y));
        }
        return go;
    }

    static GameObject CreateHelpDetail(GameObject root, string title, string body)
    {
        var go = CreateSubPanel(root, "HelpDetail_" + title, title);
        CreateBodyText(go.transform, body, new Vector2(0, -80), new Vector2(1100, 600));
        return go;
    }

    // ------------------------------------------------------------------
    // 基础控件工厂
    // ------------------------------------------------------------------
    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static GameObject CreateSubPanel(GameObject root, string name, string title)
    {
        var go = CreatePanel(root.transform, name, new Color(0.93f, 0.96f, 0.99f, 0.99f));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.SetActive(false);

        CreateText(go.transform, title, 52, new Color(0.08f, 0.32f, 0.55f), new Vector2(-650, 400), new Vector2(500, 80), TextAnchor.MiddleLeft);
        var back = CreateButton(go.transform, "返回", new Vector2(650, 400), new Vector2(140, 60));
        var bridge = back.AddComponent<PanelButtonBridge>();
        bridge.panel = root.GetComponent<ParameterPanel>();
        UnityEventTools.AddPersistentListener(back.GetComponent<Button>().onClick, bridge.Back);
        return go;
    }

    static GameObject CreateText(Transform parent, string text, int fontSize, Color color, Vector2 pos, Vector2 size, TextAnchor anchor)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = anchor;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    static void CreateBodyText(Transform parent, string text, Vector2 pos, Vector2 size)
    {
        var go = CreateText(parent, text, 30, Color.black, pos, size, TextAnchor.UpperLeft);
        var t = go.GetComponent<Text>();
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.lineSpacing = 1.4f;
    }

    static GameObject CreateButton(Transform parent, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.32f, 0.62f, 0.9f);
        var btn = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        CreateText(go.transform, label, 32, Color.white, Vector2.zero, size, TextAnchor.MiddleCenter);
        return go;
    }

    static GameObject CreateSquareButton(Transform parent, string label, Vector2 pos)
    {
        var go = CreateButton(parent, label, pos, new Vector2(300, 200));
        var img = go.GetComponent<Image>();
        img.color = new Color(0.88f, 0.9f, 0.93f);
        var t = go.GetComponentInChildren<Text>();
        t.color = Color.black;
        t.fontSize = 28;
        return go;
    }

    static void CreateCupButton(Transform parent, string label, Vector2 pos)
    {
        var go = new GameObject("Cup" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200, 260);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.55f, 0.8f, 0.95f);
        go.AddComponent<Button>();
        CreateText(go.transform, label, 80, Color.white, new Vector2(0, 50), new Vector2(160, 140), TextAnchor.MiddleCenter);
        var bottom = CreatePanel(go.transform, "Bottom", new Color(0.1f, 0.35f, 0.5f));
        bottom.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -95);
        bottom.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 60);
    }

    static InputField CreateInputField(Transform parent, Vector2 pos, Vector2 size, string initial, int fontSize)
    {
        var go = new GameObject("InputField", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.9f, 0.94f, 0.98f);

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<Text>();
        text.text = initial;
        text.fontSize = fontSize;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleLeft;
        text.font = font;
        text.supportRichText = false;
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12, 0); textRT.offsetMax = new Vector2(-12, 0);

        var phGO = new GameObject("Placeholder", typeof(RectTransform));
        phGO.transform.SetParent(go.transform, false);
        var ph = phGO.AddComponent<Text>();
        ph.text = "";
        ph.fontSize = fontSize;
        ph.color = new Color(0.55f, 0.6f, 0.65f);
        ph.alignment = TextAnchor.MiddleLeft;
        ph.font = font;
        ph.supportRichText = false;
        var phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(12, 0); phRT.offsetMax = new Vector2(-12, 0);

        var input = go.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = ph;
        input.targetGraphic = bg;
        input.text = initial;
        return input;
    }

    // ------------------------------------------------------------------
    // 事件绑定（持久化监听，场景保存后仍有效）
    // ------------------------------------------------------------------
    static void WireOpenButton(GameObject parent, string childName, ParameterPanel panel, GameObject target)
    {
        var t = parent.transform.Find(childName);
        if (t == null) return;
        var bridge = t.gameObject.AddComponent<PanelButtonBridge>();
        bridge.panel = panel;
        bridge.targetPanel = target;
        var btn = t.GetComponent<Button>();
        if (btn != null) UnityEventTools.AddPersistentListener(btn.onClick, bridge.OpenTarget);
    }

    static void WireActionButton(GameObject parent, string childName, UnityEngine.Events.UnityAction action)
    {
        var t = parent.transform.Find(childName);
        if (t == null) return;
        var btn = t.GetComponent<Button>();
        if (btn != null) UnityEventTools.AddPersistentListener(btn.onClick, action);
    }
}
