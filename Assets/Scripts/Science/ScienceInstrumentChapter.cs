using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScienceEducation
{
    public class ScienceInstrumentChapter : MonoBehaviour
    {
        public GameObject instrumentPrefab;
        public ScienceModuleUI module;
        public RawImage instrumentView;
        public Button[] partButtons, hotspots;
        public RectTransform[] hotspotLeaders;
        public Button overviewButton, demonstrationButton;
        public Text statusLabel;
        public int CurrentPart { get; private set; } = -1;
        public bool IsDemonstrating { get; private set; }
        public Camera ViewCamera { get; private set; }
        public static readonly string[] Names = { "溶杯", "搅拌桨", "机头", "控制屏", "升降支柱" };
        static readonly string[] TargetNames = { "Tube016", "Cylinder021", "Box04", "MainScreen", "TelescopicLeg" };
        static readonly string[] Descriptions = {
            "容纳溶出介质与受试片剂，是观察溶出过程的实验容器。不同杯位可用于平行观察。介质种类与体积应按所选实验方法确定。",
            "转动时搅动介质，为实验提供规定的搅拌条件。实验需按方法控制转速与桨的位置。本页旋转仅用于观察部件，不代表设定的实验转速。",
            "上部组件承载搅拌轴等部件，并与升降结构配合。装样或观察时可升起，运行前需确认位置。本页升降为结构动作示意。",
            "用于查看和设置仪器运行相关信息。在实操模块中，可由此打开参数面板。科普页只展示其位置与作用。",
            "支承并引导上部组件升降，帮助部件保持相对位置。点击“播放示意”可观察与机头配合的动作；实际结构与行程以仪器为准。"
        };
        GameObject displayRoot;
        Transform model;
        readonly List<Renderer> renderers = new List<Renderer>();
        readonly List<Transform> movingParts = new List<Transform>();
        readonly Dictionary<Transform, Vector3> initialPositions = new Dictionary<Transform, Vector3>();
        readonly Dictionary<Transform, Quaternion> initialRotations = new Dictionary<Transform, Quaternion>();
        readonly List<Renderer>[] targets = { new List<Renderer>(), new List<Renderer>(), new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };
        RenderTexture texture;
        readonly List<Material> displayMaterials = new List<Material>();
        Bounds overviewBounds;
        Vector3 desiredCenter;
        float desiredSize, animationTime;
        readonly Vector3 viewingDirection = new Vector3(1, .38f, -.55f).normalized;

        void Awake()
        {
            for (int i = 0; i < Names.Length; i++)
            {
                int part = i;
                partButtons[i].onClick.AddListener(() => SelectPart(part));
                hotspots[i].onClick.AddListener(() => SelectPart(part));
            }
            overviewButton.onClick.AddListener(ShowOverview);
            demonstrationButton.onClick.AddListener(ToggleDemonstration);
        }
        void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (displayRoot == null) CreateDisplay();
            displayRoot.SetActive(true);
            ShowOverview();
        }
        void CreateDisplay()
        {
            displayRoot = new GameObject("ScienceInstrumentDisplay");
            displayRoot.transform.position = new Vector3(6000, 0, 0);
            var instance = Instantiate(instrumentPrefab, displayRoot.transform, false);
            instance.name = "InstrumentModel";
            model = instance.transform; model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = 29;
                initialPositions[t] = t.localPosition; initialRotations[t] = t.localRotation;
                if (t.name == "Box02" || t.name == "Box04" || t.name == "Border" || t.name == "MainScreen" ||
                    t.name.StartsWith("Cylinder02") && t.name != "Cylinder020" ||
                    t.name == "Cylinder030" || t.name == "Cylinder031" || t.name == "Cylinder032") movingParts.Add(t);
            }
            renderers.AddRange(instance.GetComponentsInChildren<Renderer>());
            overviewBounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                var materials = r.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    var source = materials[m];
                    if (source == null || source.name != "材质.004") continue;
                    var copy = new Material(source);
                    copy.SetColor("_BaseColor",new Color(.62f,.70f,.67f));
                    copy.SetColor("_EmissionColor",Color.black);
                    copy.SetFloat("_Smoothness",.25f); copy.SetFloat("_Metallic",.15f);
                    materials[m] = copy; displayMaterials.Add(copy);
                }
                r.sharedMaterials = materials;
                overviewBounds.Encapsulate(r.bounds);
                for (int i = 0; i < TargetNames.Length; i++) if (r.name == TargetNames[i]) targets[i].Add(r);
            }
            var cameraObject = new GameObject("InstrumentCamera", typeof(Camera));
            cameraObject.transform.SetParent(displayRoot.transform, false);
            ViewCamera = cameraObject.GetComponent<Camera>();
            ViewCamera.cullingMask = 1 << 29;
            ViewCamera.clearFlags = CameraClearFlags.SolidColor;
            ViewCamera.backgroundColor = new Color32(229,235,225,255);
            ViewCamera.orthographic = true; ViewCamera.nearClipPlane = .1f; ViewCamera.farClipPlane = 250;
            texture = new RenderTexture(1280, 480, 24); texture.name = "ScienceInstrumentPreview";
            texture.Create(); ViewCamera.targetTexture = texture; instrumentView.texture = texture;
            var lamp = new GameObject("InstrumentLight", typeof(Light));
            lamp.transform.SetParent(displayRoot.transform,false); lamp.transform.rotation = Quaternion.Euler(35,-60,0);
            var light = lamp.GetComponent<Light>(); light.type = LightType.Directional;
            light.cullingMask = 1 << 29; light.intensity = .65f; light.shadows = LightShadows.None;
        }
        Bounds GetPartBounds(int index)
        {
            var b = targets[index][0].bounds;
            foreach (var r in targets[index]) b.Encapsulate(r.bounds);
            return b;
        }
        public void SelectPart(int index)
        {
            if (displayRoot == null) return;
            ResetPose();
            CurrentPart = Mathf.Clamp(index,0,Names.Length-1);
            foreach (var r in renderers) r.SetPropertyBlock(null);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor",new Color(1,.65f,.16f,.95f));
            block.SetColor("_Color",new Color(1,.65f,.16f,.95f));
            foreach (var r in targets[CurrentPart]) r.SetPropertyBlock(block);
            var b = GetPartBounds(CurrentPart);
            desiredCenter = b.center;
            desiredSize = Mathf.Max(3.5f,b.extents.magnitude * 1.15f);
            module.objective.text = Names[CurrentPart] + "\n" + Descriptions[CurrentPart].Split('。')[0] + "。";
            module.objective.fontSize = 19;
            module.description.text = Descriptions[CurrentPart];
            module.description.fontSize = 17;
            module.description.transform.parent.Find("PreviewHeading").GetComponent<Text>().text = "部件说明";
            demonstrationButton.interactable = CurrentPart == 1 || CurrentPart == 2 || CurrentPart == 4;
            statusLabel.text = "当前部件：" + Names[CurrentPart];
            UpdateSelection();
        }
        public void ShowOverview()
        {
            if (displayRoot == null) return;
            ResetPose(); CurrentPart = -1;
            foreach (var r in renderers) r.SetPropertyBlock(null);
            desiredCenter = overviewBounds.center;
            desiredSize = overviewBounds.extents.magnitude * .85f;
            ViewCamera.transform.position = desiredCenter + viewingDirection * 80;
            ViewCamera.transform.LookAt(desiredCenter); ViewCamera.orthographicSize = desiredSize;
            module.objective.fontSize = 21;
            module.objective.text = "点击模型上的编号或上方部件名称，认识五个部件的位置与作用。";
            module.description.text = "当前为仪器全景。金色表示选中部件；搅拌桨与升降结构支持动作示意。";
            module.description.fontSize = 18;
            module.description.transform.parent.Find("PreviewHeading").GetComponent<Text>().text = "观察提示";
            statusLabel.text = "仪器全景 · 点击编号观察部件";
            demonstrationButton.interactable = false;
            UpdateSelection();
        }
        void UpdateSelection()
        {
            for(int i=0;i<Names.Length;i++)
            {
                bool selected = CurrentPart == i;
                partButtons[i].GetComponent<Image>().color = selected ? new Color32(39,102,94,255) : new Color32(255,254,249,255);
                partButtons[i].GetComponentInChildren<Text>().color = selected ? Color.white : new Color32(27,65,61,255);
                hotspots[i].GetComponent<Image>().color = selected ? new Color32(174,116,30,255) : new Color32(39,102,94,255);
            }
        }
        public void ToggleDemonstration()
        {
            if (CurrentPart != 1 && CurrentPart != 2 && CurrentPart != 4) return;
            IsDemonstrating = !IsDemonstrating;
            demonstrationButton.GetComponentInChildren<Text>().text = IsDemonstrating ? "暂停示意" : "播放示意";
        }
        void ResetPose()
        {
            IsDemonstrating = false; animationTime = 0;
            foreach (var p in initialPositions) { p.Key.localPosition = p.Value; p.Key.localRotation = initialRotations[p.Key]; }
            demonstrationButton.GetComponentInChildren<Text>().text = "播放示意";
        }
        void LateUpdate()
        {
            if (ViewCamera == null) return;
            float blend = 1 - Mathf.Exp(-Time.unscaledDeltaTime * 8);
            ViewCamera.transform.position = Vector3.Lerp(ViewCamera.transform.position,desiredCenter + viewingDirection * 80,blend);
            ViewCamera.transform.rotation = Quaternion.Slerp(ViewCamera.transform.rotation,Quaternion.LookRotation(-viewingDirection),blend);
            ViewCamera.orthographicSize = Mathf.Lerp(ViewCamera.orthographicSize,desiredSize,blend);
            if (IsDemonstrating)
            {
                animationTime += Time.unscaledDeltaTime;
                if (CurrentPart == 1) foreach(var r in targets[1]) r.transform.localRotation = initialRotations[r.transform] * Quaternion.Euler(0,animationTime * 90,0);
                else foreach(var t in movingParts) t.localPosition = initialPositions[t] + Vector3.down * (1 - Mathf.Cos(animationTime)) * 2.39f;
            }
            var rect = instrumentView.rectTransform;
            var size = rect.rect.size;
            var placed = new List<Vector2>();
            for(int i=0;i<hotspots.Length;i++)
            {
                var v = ViewCamera.WorldToViewportPoint(GetPartBounds(i).center);
                bool visible = v.z > 0 && v.x > .02f && v.x < .98f && v.y > .02f && v.y < .98f;
                hotspots[i].gameObject.SetActive(visible);
                hotspotLeaders[i].gameObject.SetActive(visible);
                var badge = (RectTransform)hotspots[i].transform;
                var point = new Vector2(v.x*size.x,v.y*size.y);
                var position = point;
                if(i == 2) position += new Vector2(-38,24);
                if(i == 3) position += new Vector2(32,4);
                if(i == 4) position += new Vector2(-32,-8);
                foreach(var previous in placed) if(Vector2.Distance(position,previous)<44) position.x += 48;
                position = new Vector2(Mathf.Clamp(position.x,20,size.x-20),Mathf.Clamp(position.y,20,size.y-20));
                badge.anchoredPosition = position;
                if(visible) placed.Add(position);
                var delta = position - point;
                hotspotLeaders[i].anchoredPosition=point;
                hotspotLeaders[i].sizeDelta=new Vector2(delta.magnitude,2);
                hotspotLeaders[i].localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            }
        }
        void OnDisable() { if(displayRoot!=null) { ResetPose(); displayRoot.SetActive(false); } }
        void OnDestroy()
        {
            if(ViewCamera!=null) ViewCamera.targetTexture=null;
            if(texture!=null) { texture.Release(); Destroy(texture); }
            foreach(var material in displayMaterials) if(material!=null) Destroy(material);
            if(displayRoot!=null) Destroy(displayRoot);
        }
    }
}
