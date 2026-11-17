using UnityEngine;
using UnityEngine.UI;

namespace ScienceEducation
{
    public class ScienceBasicsChapter : MonoBehaviour
    {
        public Text cardHeading, cardBody, cardSource, cardCounter;
        public Button[] tabs;
        public Button rotateLeft, rotateRight, resetView;
        public RawImage tabletView;
        public int CurrentCard { get; private set; }
        GameObject displayRoot;
        Transform tablet;
        RenderTexture texture;
        Material tabletMaterial;
        Camera displayCamera;

        public static readonly string[] TabNames = { "认识片剂", "什么是崩解", "什么是溶出", "实验意义", "中药特点", "观察边界" };
        public static readonly string[] ReferenceUrls = {
            "https://digicollections.net/phint/en/d/Jb/6.2.1.8.html",
            "https://database.ich.org/sites/default/files/Q6A%20Guideline.pdf",
            "https://digicollections.net/phint/pdf/b/10.3.1.Dissolution-testing-of-tablets-and-capsules.pdf",
            "https://www.fda.gov/media/70936/download",
            "",
            "https://www.fda.gov/media/70936/download"
        };
        static readonly string[] Headings = { "一片药，不只有外观", "崩解：片剂结构发生变化", "溶出：关注成分进入介质", "为什么需要溶出实验？", "中药片剂要看什么？", "看见变化，还需检测依据" };
        static readonly string[] Bodies = {
            "片剂是将药物与适宜辅料制成的固体制剂。辅料可帮助成型，并影响片剂的性能。\n\n左侧的三维外形是通用示意，不能据此判断具体品种的处方、含量或质量。",
            "崩解关注片剂在规定条件下结构散开、形成较小颗粒等变化。\n\n颗粒变小，并不表示所关注成分已经全部溶解。崩解与溶出描述的是不同方面，两者可能同时发生。",
            "溶出关注药物成分从制剂进入溶出介质、形成溶液的过程。实验常在规定条件下，测定指定时间的溶出量。\n\n药片变小或肉眼看不见，不能单独作为完全溶出的判断依据。",
            "在规定条件下测定溶出情况，有助于了解制剂的释放特征，并用于质量评价及处方、工艺研究。\n\n比较结果时，应同时关注介质、温度、搅拌条件与检测方法。",
            "中药片剂可能包含多种成分，具体测定对象应依据该品种适用的质量标准或经过验证的方法确定。\n\n解释“溶出百分比”前，要先说明测的是什么、以什么作为计算依据。",
            "本章药片只用于观察形态，颜色、尺寸与比例均为示意，不代表具体药品。\n\n体外溶出结果需要结合适用方法解释，不能直接据此判断体内吸收或临床疗效。"
        };
        static readonly string[] Sources = {
            "参考：WHO《国际药典》· Tablets（片剂通则）",
            "参考：ICH Q6A · 3.3.2.1（崩解与溶出）",
            "参考：WHO《国际药典》· 溶出试验章节",
            "参考：FDA · 速释固体口服制剂溶出试验指南",
            "方法提示：具体品种应依据适用标准核实检测对象",
            "参考：FDA · 溶出试验指南（体内外关系）"
        };

        void OnEnable()
        {
            if (Application.isPlaying && displayRoot == null) CreateTablet();
            if (displayRoot != null) displayRoot.SetActive(true);
        }
        void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int card = i;
                tabs[i].onClick.AddListener(() => SelectCard(card));
            }
            rotateLeft.onClick.AddListener(() => RotateTablet(-30));
            rotateRight.onClick.AddListener(() => RotateTablet(30));
            resetView.onClick.AddListener(ResetTablet);
            SelectCard(0);
        }
        public void SelectCard(int index)
        {
            CurrentCard = Mathf.Clamp(index, 0, TabNames.Length - 1);
            cardHeading.text = Headings[CurrentCard];
            cardBody.text = Bodies[CurrentCard];
            cardSource.text = Sources[CurrentCard];
            cardCounter.text = (CurrentCard + 1).ToString("00") + " / 06";
            for (int i = 0; i < tabs.Length; i++)
            {
                bool selected = i == CurrentCard;
                tabs[i].GetComponent<Image>().color = selected ? new Color32(39,102,94,255) : new Color32(248,248,240,255);
                tabs[i].GetComponentInChildren<Text>().color = selected ? Color.white : new Color32(27,65,61,255);
            }
        }
        public void RotateTablet(float angle) { if (tablet != null) tablet.Rotate(Vector3.up,angle,Space.World); }
        public void ResetTablet() { if (tablet != null) tablet.rotation = Quaternion.Euler(24, 0, -15); }
        void CreateTablet()
        {
            displayRoot = new GameObject("ScienceTabletDisplay");
            displayRoot.transform.position = new Vector3(5000,0,0);
            var model = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            model.name = "IllustrativeTablet"; model.layer = 30;
            model.transform.SetParent(displayRoot.transform,false);
            model.transform.localScale = new Vector3(1.65f,.23f,1.65f);
            Destroy(model.GetComponent<Collider>());
            tablet = model.transform; ResetTablet();
            tabletMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            tabletMaterial.SetColor("_BaseColor",new Color(.68f,.43f,.22f));
            tabletMaterial.SetFloat("_Smoothness",.24f);
            model.GetComponent<Renderer>().sharedMaterial = tabletMaterial;
            var cameraObject = new GameObject("TabletCamera",typeof(Camera));
            cameraObject.transform.SetParent(displayRoot.transform,false);
            cameraObject.transform.localPosition = new Vector3(0,2.5f,-4);
            cameraObject.transform.LookAt(tablet.position);
            displayCamera = cameraObject.GetComponent<Camera>();
            displayCamera.cullingMask = 1 << 30; displayCamera.clearFlags = CameraClearFlags.SolidColor;
            displayCamera.backgroundColor = new Color32(229,235,225,255);
            displayCamera.orthographic = true; displayCamera.orthographicSize = 1.35f;
            displayCamera.nearClipPlane = .1f; displayCamera.farClipPlane = 15;
            texture = new RenderTexture(640,480,24); texture.name = "ScienceTabletPreview";
            texture.Create(); displayCamera.targetTexture = texture; tabletView.texture = texture;
            var lightObject = new GameObject("TabletLight",typeof(Light));
            lightObject.transform.SetParent(displayRoot.transform,false);
            lightObject.transform.rotation = Quaternion.Euler(40,-30,0);
            var light = lightObject.GetComponent<Light>();light.type = LightType.Directional;
            light.intensity = 1.4f; light.cullingMask = 1 << 30; light.shadows = LightShadows.None;
        }
        void OnDisable() { if (displayRoot != null) displayRoot.SetActive(false); }
        void OnDestroy()
        {
            if (displayCamera != null) displayCamera.targetTexture = null;
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (tabletMaterial != null) Destroy(tabletMaterial);
            if (displayRoot != null) Destroy(displayRoot);
        }
    }
}
