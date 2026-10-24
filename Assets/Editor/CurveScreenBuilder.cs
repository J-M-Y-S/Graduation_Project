using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using XCharts.Runtime;
using InstrumentAnimation;

/// <summary>
/// 在 SampleScene 中一键构建溶出仪旁边的实体曲线屏幕。
/// 使用方式：菜单 Tools / Build Curve Screen。
/// 结构：一块屏幕形状的薄 Cube 作为机身，World Space Canvas 贴在机身正面显示 XCharts 曲线。
/// </summary>
public class CurveScreenBuilder
{
    [MenuItem("Tools/Build Curve Screen")]
    public static void Build()
    {
        EnsureThemes();
        var existing = GameObject.Find("CurveScreen");
        if (existing != null) Object.DestroyImmediate(existing);

        // 屏幕根节点：溶出仪右侧、面向主相机
        var screen = new GameObject("CurveScreen");
        screen.transform.position = new Vector3(12f, 2f, 24.5f);
        screen.transform.rotation = Quaternion.identity;

        // 屏幕机身：薄 Cube
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "ScreenBody";
        var collider = body.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        body.transform.SetParent(screen.transform, false);
        body.transform.localScale = new Vector3(4.8f, 2.6f, 0.08f);
        var renderer = body.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = GetScreenMaterial();

        // World Space Canvas：贴在机身正面（朝相机方向略微外移）
        var canvasGO = new GameObject("CurveCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(screen.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0f, -0.06f);
        canvasGO.transform.localRotation = Quaternion.identity;
        // XCharts 图表以 580x300 为原生尺寸，把整块 Canvas 缩小 0.008 倍映射到世界空间
        canvasGO.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(580f, 300f);

        // 显示区背景
        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(canvasGO.transform, false);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.96f, 0.98f, 1f, 1f);

        // 图表对象
        var chartGO = new GameObject("LineChart", typeof(RectTransform));
        chartGO.transform.SetParent(canvasGO.transform, false);
        var chartRT = chartGO.GetComponent<RectTransform>();
        chartRT.anchorMin = new Vector2(0.5f, 0.5f);
        chartRT.anchorMax = new Vector2(0.5f, 0.5f);
        chartRT.pivot = new Vector2(0.5f, 0.5f);
        chartRT.anchoredPosition = Vector2.zero;
        chartRT.sizeDelta = new Vector2(580f, 300f);
        chartGO.AddComponent<LineChart>();

        var dc = chartGO.AddComponent<DissolutionChart>();
        dc.Configure();
        dc.ShowDemoCurve();

        var ctrl = screen.AddComponent<CurveScreenController>();
        ctrl.chart = dc;

        EditorUtility.SetDirty(screen);
        EditorUtility.SetDirty(chartGO);
        Debug.Log("Curve screen built.");
    }

    /// <summary>修复内存中被污染的主题实例（此前 UpdateTheme(Dark) 误改了共享的 Default 主题）。</summary>
    static void EnsureThemes()
    {
        var def = Resources.Load<Theme>("XCTheme-Default");
        if (def != null && def.themeName != "Default")
            Theme.ResetToDefaultTheme(def);
        var dark = Resources.Load<Theme>("XCTheme-Dark");
        if (dark != null && dark.themeName != "Dark")
            Theme.ResetToDarkTheme(dark);
        XCThemeMgr.ReloadThemeList();
    }

    static Material GetScreenMaterial()
    {
        const string path = "Assets/Materials/Mat_Screen_Body.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        mat = new Material(shader);
        var dark = new Color(0.03f, 0.05f, 0.08f, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", dark);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", dark);

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
