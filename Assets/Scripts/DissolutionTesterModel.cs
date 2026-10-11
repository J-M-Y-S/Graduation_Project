// DissolutionTesterModel.cs
// ---------------------------------------------------------------------------
// 依据三视工程图程序化生成的实验室溶出仪模型。
// 外形尺寸: 宽 700 x 深 420 x 高 520 (mm)，1 Unity 单位 = 1 米。
// 不使用任何贴图，仅用基础材质的颜色 / 金属度 / 光滑度来表现外观。
//
// 用法:
//   1. 把本文件拖进 Unity 工程的 Assets 目录。
//   2. 菜单 GameObject > 溶出仪 > 创建溶出仪模型，即可在场景中生成。
//      (该菜单会把 9 个材质同时保存为 Assets/DissolutionTester/*.mat)
//   3. 或者在任意空物体上添加 DissolutionTesterModel 组件，运行时自动生成。
// 兼容内置渲染管线(Standard)与 URP(Lit)。
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LabDissolution
{
    [AddComponentMenu("Lab/Dissolution Tester Model")]
    public class DissolutionTesterModel : MonoBehaviour
    {
        [Header("生成选项")]
        [Tooltip("运行时自动生成模型")]
        public bool buildOnStart = true;
        [Tooltip("为每个部件生成碰撞体（一般不需要）")]
        public bool createColliders = false;
        [Tooltip("生成前清空已有子物体")]
        public bool clearBeforeBuild = true;

        [Header("外形尺寸 (米)")]
        public float width  = 0.70f;   // 700 mm
        public float depth  = 0.42f;   // 420 mm
        public float height = 0.52f;   // 520 mm

        // 4 个溶出杯的中心 X 坐标
        static readonly float[] VesselX = { -0.255f, -0.085f, 0.085f, 0.255f };

        Material _shell, _frame, _tray, _dark, _screen, _metal, _rubber, _glass, _water;
        readonly List<Material> _materials = new List<Material>();

        void Start()
        {
            if (buildOnStart) Build();
        }

        // ------------------------------------------------------------------
        [ContextMenu("生成模型 / Build Model")]
        public void Build()
        {
            if (clearBeforeBuild) ClearChildren();
            CreateMaterials();

            Transform root = transform;

            BuildFeet(root);
            BuildLowerHousing(root);
            BuildVessels(root);
            BuildHead(root);
            BuildScreen(root);
            BuildKnobsAndProbe(root);
        }

        [ContextMenu("清除模型 / Clear Model")]
        public void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroySafe(transform.GetChild(i).gameObject);
        }

        // ------------------------------------------------------------------
        // 底座脚垫
        // ------------------------------------------------------------------
        void BuildFeet(Transform root)
        {
            Transform g = Group(root, "Feet");
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Cyl(g, "Foot", new Vector3(sx * 0.29f, 0.0125f, sz * 0.155f),
                        0.020f, 0.025f, _rubber);
        }

        // ------------------------------------------------------------------
        // 下机身：水浴槽框架 + 顶板
        // ------------------------------------------------------------------
        void BuildLowerHousing(Transform root)
        {
            Transform g = Group(root, "LowerHousing");

            Box(g, "BasePlate", new Vector3(0f, 0.040f, 0f),
                new Vector3(0.68f, 0.030f, 0.40f), _frame);
            Box(g, "BathTray", new Vector3(0f, 0.062f, 0f),
                new Vector3(0.60f, 0.016f, 0.34f), _tray);

            foreach (float sx in new[] { -0.315f, 0.315f })
                foreach (float sz in new[] { -0.185f, 0.185f })
                    Box(g, "Post", new Vector3(sx, 0.115f, sz),
                        new Vector3(0.030f, 0.150f, 0.030f), _shell);

            Box(g, "TopPlate", new Vector3(0f, 0.190f, 0f),
                new Vector3(width, 0.022f, depth), _shell);

            Box(g, "CentreColumn", new Vector3(0f, 0.270f, -0.150f),
                new Vector3(0.055f, 0.150f, 0.055f), _shell);
        }

        // ------------------------------------------------------------------
        // 4 组：溶出杯 + 液体 + 搅拌轴 + 桨叶
        // ------------------------------------------------------------------
        void BuildVessels(Transform root)
        {
            Transform g = Group(root, "Vessels");

            for (int i = 0; i < VesselX.Length; i++)
            {
                float x = VesselX[i];
                Transform v = Group(g, "Cup" + (i + 1));

                Sph(v, "Bottom", new Vector3(x, 0.125f, 0f), 0.068f, _glass);
                Cyl(v, "Body", new Vector3(x, 0.166f, 0f), 0.068f, 0.086f, _glass);
                Cyl(v, "NeckFlange", new Vector3(x, 0.205f, 0f), 0.080f, 0.016f, _shell);

                Sph(v, "Liquid", new Vector3(x, 0.125f, 0f), 0.062f, _water);
                Cyl(v, "LiquidBody", new Vector3(x, 0.157f, 0f), 0.062f, 0.064f, _water);

                Cyl(v, "Shaft", new Vector3(x, 0.265f, 0f), 0.006f, 0.310f, _metal);
                Cyl(v, "HeadCoupling", new Vector3(x, 0.330f, 0f), 0.024f, 0.030f, _metal);
                Box(v, "Paddle", new Vector3(x, 0.105f, 0f),
                    new Vector3(0.045f, 0.030f, 0.006f), _metal);
            }
        }

        // ------------------------------------------------------------------
        // 上机身（机头）
        // ------------------------------------------------------------------
        void BuildHead(Transform root)
        {
            Transform g = Group(root, "Head");
            Box(g, "HeadBody", new Vector3(0f, 0.430f, 0f),
                new Vector3(width, 0.180f, depth), _shell);
            Box(g, "HeadBand", new Vector3(0f, 0.340f, 0f),
                new Vector3(0.715f, 0.012f, 0.430f), _frame);
        }

        // ------------------------------------------------------------------
        // 触摸屏模组：竖立并向后倾约 34 度
        // ------------------------------------------------------------------
        void BuildScreen(Transform root)
        {
            Transform g = Group(root, "ScreenModule");
            Quaternion rot = Quaternion.Euler(56f, 0f, 0f);
            Vector3 c = new Vector3(0f, 0.560f, -0.032f);

            Box(g, "Bezel", c, new Vector3(0.215f, 0.016f, 0.140f), _dark, rot);

            Vector3 normal = rot * Vector3.up;
            Box(g, "Glass", c + normal * 0.0125f,
                new Vector3(0.180f, 0.006f, 0.104f), _screen, rot);
        }

        // ------------------------------------------------------------------
        // 顶部旋钮 + 右侧探头
        // ------------------------------------------------------------------
        void BuildKnobsAndProbe(Transform root)
        {
            Transform g = Group(root, "Fittings");

            foreach (float sx in new[] { -1f, 1f })
            {
                float x = sx * 0.215f;
                Cyl(g, "KnobBase", new Vector3(x, 0.526f, -0.040f), 0.020f, 0.014f, _metal);
                Cyl(g, "KnobStem", new Vector3(x, 0.543f, -0.040f), 0.008f, 0.022f, _metal);
                Cyl(g, "KnobCap", new Vector3(x, 0.558f, -0.040f), 0.014f, 0.012f, _metal);
            }

            Cyl(g, "Probe", new Vector3(0.235f, 0.250f, 0.055f), 0.0035f, 0.100f, _dark);
            Sph(g, "ProbeTip", new Vector3(0.235f, 0.197f, 0.055f), 0.006f, _dark);
        }

        // ==================================================================
        // 图元工具
        // ==================================================================
        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        GameObject Primitive(PrimitiveType type, Transform parent, string name, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (!createColliders)
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null) DestroySafe(col);
            }
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return go;
        }

        GameObject Box(Transform p, string n, Vector3 c, Vector3 s, Material m)
        {
            return Box(p, n, c, s, m, Quaternion.identity);
        }

        GameObject Box(Transform p, string n, Vector3 c, Vector3 s, Material m, Quaternion rot)
        {
            GameObject go = Primitive(PrimitiveType.Cube, p, n, m);
            go.transform.localPosition = c;
            go.transform.localRotation = rot;
            go.transform.localScale = s;
            return go;
        }

        GameObject Cyl(Transform p, string n, Vector3 c, float radius, float h, Material m)
        {
            GameObject go = Primitive(PrimitiveType.Cylinder, p, n, m);
            go.transform.localPosition = c;
            go.transform.localScale = new Vector3(radius * 2f, h * 0.5f, radius * 2f);
            return go;
        }

        GameObject Sph(Transform p, string n, Vector3 c, float radius, Material m)
        {
            GameObject go = Primitive(PrimitiveType.Sphere, p, n, m);
            go.transform.localPosition = c;
            go.transform.localScale = Vector3.one * (radius * 2f);
            return go;
        }

        void DestroySafe(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        // ==================================================================
        // 材质
        // ==================================================================
        void CreateMaterials()
        {
            _materials.Clear();
            _shell  = Make("Tester_Shell",   new Color(0.93f, 0.93f, 0.94f), 0.00f, 0.35f, false);
            _frame  = Make("Tester_Frame",   new Color(0.85f, 0.85f, 0.87f), 0.00f, 0.30f, false);
            _tray   = Make("Tester_Tray",    new Color(0.80f, 0.83f, 0.85f), 0.00f, 0.40f, false);
            _dark   = Make("Tester_Dark",    new Color(0.12f, 0.12f, 0.13f), 0.00f, 0.45f, false);
            _screen = Make("Tester_Screen",  new Color(0.72f, 0.76f, 0.80f), 0.00f, 0.85f, false);
            _metal  = Make("Tester_Metal",   new Color(0.78f, 0.79f, 0.82f), 1.00f, 0.80f, false);
            _rubber = Make("Tester_Rubber",  new Color(0.14f, 0.14f, 0.15f), 0.00f, 0.25f, false);
            _glass  = Make("Tester_Glass",   new Color(0.82f, 0.92f, 0.98f, 0.26f), 0.00f, 0.95f, true);
            _water  = Make("Tester_Water",   new Color(0.62f, 0.82f, 0.93f, 0.55f), 0.00f, 0.90f, true);

            if (_screen.HasProperty("_EmissionColor"))
            {
                _screen.EnableKeyword("_EMISSION");
                _screen.SetColor("_EmissionColor", new Color(0.72f, 0.76f, 0.80f) * 0.25f);
            }
        }

        Material Make(string name, Color color, float metallic, float smoothness, bool transparent)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            bool urp = shader != null;
            if (!urp)
            {
                shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
                if (shader == null) shader = Shader.Find("Diffuse");
            }

            Material m = new Material(shader);
            m.name = name;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);

            if (transparent)
            {
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                if (urp)
                {
                    m.SetFloat("_Surface", 1f);
                    m.SetFloat("_Blend", 0f);
                    m.SetFloat("_AlphaClip", 0f);
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.SetShaderPassEnabled("ShadowCaster", false);
                }
                else
                {
                    m.SetFloat("_Mode", 3f);
                    m.EnableKeyword("_ALPHABLEND_ON");
                    m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                }
            }

            _materials.Add(m);
            return m;
        }

#if UNITY_EDITOR
        /// <summary>把运行时材质保存成工程资源，方便直接编辑。</summary>
        public void SaveMaterialsAsAssets(string folder = "Assets/DissolutionTester")
        {
            if (Application.isPlaying) return;
            if (_materials.Count == 0) CreateMaterials();
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "DissolutionTester");
            foreach (Material m in _materials)
            {
                string path = folder + "/" + m.name + ".mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                    AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(m, path);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }

#if UNITY_EDITOR
    static class DissolutionTesterMenu
    {
        [MenuItem("GameObject/溶出仪/创建溶出仪模型", false, 10)]
        static void CreateModel()
        {
            GameObject go = new GameObject("DissolutionTester");
            DissolutionTesterModel comp = go.AddComponent<DissolutionTesterModel>();
            comp.buildOnStart = false;
            comp.Build();
            comp.SaveMaterialsAsAssets();
            // 整棵层级建好后再登记 Undo，撤销时一并移除
            Undo.RegisterCreatedObjectUndo(go, "Create Dissolution Tester");

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
#endif
}
