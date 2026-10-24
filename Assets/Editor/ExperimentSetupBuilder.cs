using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using InstrumentAnimation;
using System.Collections.Generic;

/// <summary>
/// 在 SampleScene 中一键搭建实验交互（药片实体 + 溶杯点击 + 加水/倒水 UI）。
/// 使用方式：菜单 Tools / Build Experiment Setup。
/// </summary>
public class ExperimentSetupBuilder
{
    // 玻璃溶杯 A B C D
    static readonly string[] CupNames = { "Tube016", "Tube018", "Tube020", "Tube022" };
    // 水体模型 A B C D
    static readonly string[] WaterNames = { "Tube030", "Tube031", "Tube032", "Tube029" };

    static readonly Color[] TabletColors =
    {
        new Color(0.9f, 0.25f, 0.25f), // 红 复方丹参片
        new Color(0.3f, 0.5f, 0.95f),  // 蓝 板蓝根片
        new Color(0.35f, 0.8f, 0.4f),  // 绿 穿心莲片
        new Color(0.95f, 0.8f, 0.25f), // 黄 牛黄解毒片
        new Color(0.7f, 0.4f, 0.9f)    // 紫 银翘解毒片
    };

    static readonly string[] TabletMaterialPaths =
    {
        "Assets/Materials/Mat_Tablet_Red.mat",
        "Assets/Materials/Mat_Tablet_Blue.mat",
        "Assets/Materials/Mat_Tablet_Green.mat",
        "Assets/Materials/Mat_Tablet_Yellow.mat",
        "Assets/Materials/Mat_Tablet_Purple.mat"
    };

    [MenuItem("Tools/Build Experiment Setup")]
    public static void Build()
    {
        DestroyByName("TabletRack");
        DestroyByName("DissolutionExperiment");
        DestroyByName("TabletInventory");
        DestroyByName("WaterInteractionCanvas");

        TabletInventory.EnsureExists();

        var expGO = new GameObject("DissolutionExperiment");
        var exp = expGO.AddComponent<DissolutionExperiment>();

        // 1) 药片实体：5 个彩色 Cube，放在仪器左侧、相机视野内
        var rack = new GameObject("TabletRack");
        rack.transform.position = new Vector3(-10.5f, 0f, 24f);
        var defs = TabletCatalog.Defaults;
        float[] ys = { -4.5f, -3f, -1.5f, 0f, 1.5f };
        for (int i = 0; i < defs.Count; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Tablet_" + defs[i].tabletName;
            cube.transform.SetParent(rack.transform, false);
            cube.transform.localPosition = new Vector3(0f, ys[i], 0f);
            cube.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            cube.GetComponent<Renderer>().sharedMaterial = GetTabletMaterial(i);
            var t = cube.AddComponent<Tablet>();
            t.definition = defs[i];
        }

        // 2) 溶杯点击 + 水体控制
        var app = GameObject.Find("Dissolution_Apparatus");
        for (int i = 0; i < 4; i++)
        {
            var cup = app != null ? app.transform.Find(CupNames[i]) : null;
            if (cup != null)
            {
                if (cup.GetComponent<Collider>() == null) cup.gameObject.AddComponent<BoxCollider>();
                var cc = cup.gameObject.GetComponent<CupClickable>();
                if (cc == null) cc = cup.gameObject.AddComponent<CupClickable>();
                cc.cupIndex = i;
            }

            var water = app != null ? app.transform.Find(WaterNames[i]) : null;
            if (water != null)
            {
                RestoreWater(water);
                var wl = water.gameObject.GetComponent<WaterLevelController>();
                if (wl == null) wl = water.gameObject.AddComponent<WaterLevelController>();
                wl.CaptureFull();
                wl.SetLevel(0f); // 初始无水
                exp.waterLevels[i] = wl;
            }
        }

        // 3) 加水/倒水 UI
        var ui = BuildWaterUI();
        exp.waterUI = ui;

        // 4) 加热面板实时显示
        SetupHeatingUI();

        // 4.5) 转速输入框同步（L/AB、R/CD）
        SetupSpeedSync();

        // 5) 搅拌桨（Cylinder021/024/027/030）
        SetupPaddles(app);

        // 6) 杯内药片消融材质与杯体引用
        SetupDissolve(exp, app);

        // 7) 打印纸条 UI
        exp.printPaper = BuildPrintUI();

        // 8) 语言切换
        SetupLanguage();

        EditorUtility.SetDirty(expGO);
        EditorUtility.SetDirty(rack);
        Debug.Log("Experiment setup built.");
    }

    static void SetupLanguage()
    {
        LocalizationManager.EnsureExists();
        var canvas = GameObject.Find("ParameterPanelCanvas");
        var root = canvas != null ? canvas.transform.Find("ParameterPanelRoot") : null;
        var lang = root != null ? root.Find("LanguagePanel") : null;
        if (lang == null) { Debug.LogWarning("LanguagePanel not found"); return; }
        WireLanguageButton(lang.Find("简体中文Button"), UILanguage.Chinese);
        WireLanguageButton(lang.Find("EnglishButton"), UILanguage.English);
        Debug.Log("Language options set up.");
    }

    static void WireLanguageButton(Transform t, UILanguage lang)
    {
        if (t == null) return;
        var opt = t.GetComponent<LanguageOption>();
        if (opt == null) opt = t.gameObject.AddComponent<LanguageOption>();
        opt.language = lang;
        if (t.GetComponent<LocalizationIgnore>() == null) t.gameObject.AddComponent<LocalizationIgnore>();
        EditorUtility.SetDirty(t.gameObject);
    }

    static PrintPaperUI BuildPrintUI()
    {
        var canvasGO = new GameObject("PrintCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var root = CreatePanel(canvasGO.transform, "Root", new Color(0f, 0f, 0f, 0.6f));
        var rrt = root.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
        var group = root.AddComponent<CanvasGroup>();

        var paper = CreatePanel(root.transform, "Paper", new Color(0.97f, 0.95f, 0.9f));
        var prt = paper.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(480, 800);
        prt.anchoredPosition = Vector2.zero;

        var title = CreateText(paper.transform, "溶出度测试报告", 30, new Color(0.15f, 0.15f, 0.15f), new Vector2(0, 350), new Vector2(440, 50), TextAnchor.MiddleCenter);
        var body = CreateText(paper.transform, "", 20, new Color(0.15f, 0.15f, 0.15f), new Vector2(0, 20), new Vector2(420, 620), TextAnchor.UpperLeft);
        var bodyText = body.GetComponent<Text>();
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        bodyText.lineSpacing = 1.2f;

        var close = CreateButton(paper.transform, "关闭", new Vector2(0, -350), new Vector2(160, 60));

        var ui = canvasGO.AddComponent<PrintPaperUI>();
        ui.root = root;
        ui.group = group;
        ui.titleText = title.GetComponent<Text>();
        ui.bodyText = bodyText;
        ui.closeButton = close.GetComponent<Button>();
        root.SetActive(false);
        return ui;
    }

    static void SetupDissolve(DissolutionExperiment exp, GameObject app)
    {
        if (exp == null) return;
        exp.cupTransforms = new Transform[4];
        for (int i = 0; i < 4; i++)
            exp.cupTransforms[i] = app != null ? app.transform.Find(CupNames[i]) : null;
        exp.dissolveMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Shader Graphs_DissolveShader.mat");
        EditorUtility.SetDirty(exp);
        Debug.Log("Dissolve material=" + (exp.dissolveMaterial != null));
    }

    static void SetupSpeedSync()
    {
        var canvas = GameObject.Find("ParameterPanelCanvas");
        if (canvas == null) return;
        var bindings = canvas.GetComponentsInChildren<ConfigFieldBinding>(true);
        int count = 0;
        foreach (var b in bindings)
        {
            int pair;
            bool end;
            switch (b.field)
            {
                case ConfigField.InitialSpeedAB: pair = 0; end = false; break;
                case ConfigField.EndSpeedAB: pair = 0; end = true; break;
                case ConfigField.InitialSpeedCD: pair = 1; end = false; break;
                case ConfigField.EndSpeedCD: pair = 1; end = true; break;
                default: continue;
            }
            var sync = b.GetComponent<SpeedFieldSync>();
            if (sync == null) sync = b.gameObject.AddComponent<SpeedFieldSync>();
            sync.input = b.GetComponent<InputField>();
            sync.pairIndex = pair;
            sync.isEnd = end;
            count++;
        }
        Debug.Log("Speed sync attached: " + count);
    }

    static void SetupPaddles(GameObject app)
    {
        if (app == null) return;
        var ctrl = app.GetComponent<DissolutionTesterController>();
        if (ctrl == null) return;
        var head = app.transform.Find("AutoHeadGroup");
        var names = new[] { "Cylinder021", "Cylinder024", "Cylinder027", "Cylinder030" };
        var rods = new List<Transform>();
        foreach (var n in names)
        {
            var r = head != null ? head.Find(n) : app.transform.Find(n);
            if (r != null) rods.Add(r);
        }
        ctrl.paddles = rods.ToArray();
        EditorUtility.SetDirty(ctrl);
        Debug.Log("Paddles assigned: " + rods.Count);
    }

    static void SetupHeatingUI()
    {
        var canvas = GameObject.Find("ParameterPanelCanvas");
        var root = canvas != null ? canvas.transform.Find("ParameterPanelRoot") : null;
        var heat = root != null ? root.Find("HeatingPanel") : null;
        if (heat == null) { Debug.LogWarning("HeatingPanel not found"); return; }

        var currentText = FindValueAfterLabel(heat, "当前温度");
        var targetText = FindValueAfterLabel(heat, "目标温度");
        var progressBar = heat.Find("ProgressBar");
        var fill = progressBar != null ? progressBar.Find("Fill") : null;
        var fillRect = fill != null ? fill.GetComponent<RectTransform>() : null;

        var hui = heat.GetComponent<HeatingPanelUI>();
        if (hui == null) hui = heat.gameObject.AddComponent<HeatingPanelUI>();
        hui.currentText = currentText;
        hui.targetText = targetText;
        hui.fillRect = fillRect;
        EditorUtility.SetDirty(heat.gameObject);
        Debug.Log("Heating UI: current=" + (currentText != null) + " target=" + (targetText != null) + " fill=" + (fillRect != null));
    }

    static Text FindValueAfterLabel(Transform parent, string label)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var t = parent.GetChild(i).GetComponent<Text>();
            if (t != null && t.text == label && i + 1 < parent.childCount)
                return parent.GetChild(i + 1).GetComponent<Text>();
        }
        return null;
    }

    static WaterInteractionUI BuildWaterUI()
    {
        var canvasGO = new GameObject("WaterInteractionCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var panel = CreatePanel(canvasGO.transform, "Panel", new Color(0.1f, 0.12f, 0.16f, 0.95f));
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(620, 240);
        prt.anchoredPosition = Vector2.zero;

        var title = CreateText(panel.transform, "溶杯 A", 34, Color.white, new Vector2(0, 60), new Vector2(560, 50), TextAnchor.MiddleCenter);
        var actionBtn = CreateButton(panel.transform, "加水", new Vector2(-190, -50), new Vector2(160, 70));
        var tabletBtn = CreateButton(panel.transform, "投片", new Vector2(0, -50), new Vector2(160, 70));
        tabletBtn.GetComponent<Image>().color = new Color(0.35f, 0.7f, 0.45f);
        var cancelBtn = CreateButton(panel.transform, "取消", new Vector2(190, -50), new Vector2(160, 70));
        cancelBtn.GetComponent<Image>().color = new Color(0.42f, 0.46f, 0.52f);

        var toastGO = CreatePanel(canvasGO.transform, "Toast", new Color(0f, 0f, 0f, 0.7f));
        var trt = toastGO.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f);
        trt.pivot = new Vector2(0.5f, 0f);
        trt.sizeDelta = new Vector2(720, 56);
        trt.anchoredPosition = new Vector2(0, 140);
        var toastText = CreateText(toastGO.transform, "", 30, Color.white, Vector2.zero, new Vector2(720, 56), TextAnchor.MiddleCenter);

        var toast = canvasGO.AddComponent<Toast>();
        toast.text = toastText.GetComponent<Text>();

        // 左下角手持药片显示
        var heldGO = CreatePanel(canvasGO.transform, "HeldTablet", new Color(0f, 0f, 0f, 0.7f));
        var hrt = heldGO.GetComponent<RectTransform>();
        hrt.anchorMin = hrt.anchorMax = new Vector2(0f, 0f);
        hrt.pivot = new Vector2(0f, 0f);
        hrt.sizeDelta = new Vector2(440, 56);
        hrt.anchoredPosition = new Vector2(24, 24);
        var heldText = CreateText(heldGO.transform, "当前药片：无", 28, Color.white, Vector2.zero, new Vector2(440, 56), TextAnchor.MiddleCenter);
        var held = canvasGO.AddComponent<HeldTabletDisplay>();
        held.text = heldText.GetComponent<Text>();

        var ui = canvasGO.AddComponent<WaterInteractionUI>();
        ui.panel = panel;
        ui.titleText = title.GetComponent<Text>();
        ui.actionText = actionBtn.GetComponentInChildren<Text>();
        ui.actionButton = actionBtn.GetComponent<Button>();
        ui.tabletButton = tabletBtn.GetComponent<Button>();
        ui.cancelButton = cancelBtn.GetComponent<Button>();
        panel.SetActive(false);
        return ui;
    }

    static Material GetTabletMaterial(int i)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TabletMaterialPaths[i]);
        if (mat != null) return mat;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        mat = new Material(shader);
        mat.SetColor("_BaseColor", TabletColors[i]);
        AssetDatabase.CreateAsset(mat, TabletMaterialPaths[i]);
        AssetDatabase.SaveAssets();
        return mat;
    }

    /// <summary>恢复水体到满水原始状态（本地 Y 是直径轴，本地 Z 是高度轴）。</summary>
    static void RestoreWater(Transform water)
    {
        var ls = water.localScale;
        ls.y = ls.x;
        water.localScale = ls;
        var lp = water.localPosition;
        lp.y = 5.62f;
        water.localPosition = lp;
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
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

    static GameObject CreateButton(Transform parent, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.6f, 0.9f);
        go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        CreateText(go.transform, label, 30, Color.white, Vector2.zero, size, TextAnchor.MiddleCenter);
        return go;
    }

    static void DestroyByName(string name)
    {
        var gos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in gos)
        {
            if (go.name == name && go.scene.IsValid())
                UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
