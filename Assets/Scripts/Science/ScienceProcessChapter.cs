using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScienceEducation
{
    public class ScienceProcessChapter : MonoBehaviour
    {
        public ScienceModuleUI module;
        public RawImage processView;
        public Button[] stageButtons;
        public Button playButton, replayButton, nextStageButton;
        public Image progressFill;
        public Text statusLabel;
        public int CurrentStage { get; private set; }
        public float NormalizedProgress { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsComplete => NormalizedProgress >= 1;
        public static readonly string[] StageNames = { "接触介质", "结构变化", "成分进入介质", "回顾与检测" };
        static readonly string[] Explanations = {
            "观察片剂与介质接触。介质可接触片剂表面，并可能进入孔隙；具体变化取决于制剂与实验条件。",
            "观察片剂及碎片的外观变化。结构散开与成分溶出可能同时发生，不能把两者理解为必然先后完成。",
            "观察蓝点所表达的概念：所关注成分由固体进入介质。蓝点只是帮助理解，不表示真实分子的大小、数量或轨迹。",
            "外观变化只是观察线索。判断某一成分的溶出量，需要按适用方法取样、检测和计算；片剂消失不等于全部成分完全溶出。"
        };
        static readonly string[] Notes = {
            "点击播放观看本段，段末自动暂停。可直接点击上方节点定位；这里的进度仅表示演示播放位置。",
            "棕色代表片剂或碎片，蓝点代表成分示意。图形与运动经过简化，演示时长不代表真实实验时间。",
            "右侧为局部概念示意，与左侧不按同一比例绘制。蓝点并非气泡，介质中的成分通常不能直接用肉眼这样观察。",
            "参考：WHO《国际药典》崩解试验与溶出试验章节。本示意不用于推算具体中药片剂的溶出量或评价疗效。"
        };
        public const string DisintegrationReference = "https://digicollections.net/phint/pdf/b/7.5.4.5.3-Disintegration-test-for-tablets-and-capsules.pdf";
        public const string DissolutionReference = "https://digicollections.net/phint/pdf/b/10.3.1.Dissolution-testing-of-tablets-and-capsules.pdf";
        const float StageDurationSeconds = 5;
        GameObject displayRoot;
        Transform tablet, localFragment;
        readonly List<Transform> fragments = new List<Transform>();
        readonly List<Transform> macroDots = new List<Transform>();
        readonly List<Transform> detailDots = new List<Transform>();
        readonly List<Material> materials = new List<Material>();
        Camera viewCamera;
        RenderTexture texture;

        void Awake()
        {
            for(int i=0;i<stageButtons.Length;i++)
            {
                int stage=i;
                stageButtons[i].onClick.AddListener(()=>SelectStage(stage));
            }
            playButton.onClick.AddListener(TogglePlayback);
            replayButton.onClick.AddListener(Replay);
            nextStageButton.onClick.AddListener(NextStage);
        }
        void OnEnable()
        {
            if(!Application.isPlaying) return;
            if(displayRoot==null) CreateDisplay();
            displayRoot.SetActive(true);
            SelectStage(0);
        }
        public void SelectStage(int index)
        {
            CurrentStage=Mathf.Clamp(index,0,StageNames.Length-1);
            NormalizedProgress=CurrentStage/4f; IsPlaying=false;
            Refresh(); UpdateVisuals();
        }
        public void TogglePlayback()
        {
            if(IsComplete) return;
            if(!IsPlaying && NormalizedProgress>=(CurrentStage+1)/4f && CurrentStage<3) CurrentStage++;
            IsPlaying=!IsPlaying;
            Refresh();
        }
        public void Replay() { SelectStage(0); IsPlaying=true; Refresh(); }
        public void PausePlayback() { IsPlaying=false; Refresh(); }
        public void NextStage() { if(CurrentStage<3) SelectStage(CurrentStage+1); }
        public void Advance(float deltaSeconds)
        {
            if(!IsPlaying || deltaSeconds<=0) return;
            float end=(CurrentStage+1)/4f;
            NormalizedProgress=Mathf.Min(end,NormalizedProgress+deltaSeconds/(StageDurationSeconds*4));
            if(NormalizedProgress>=end) IsPlaying=false;
            Refresh(); UpdateVisuals();
        }
        void Update() { Advance(Time.unscaledDeltaTime); }
        void Refresh()
        {
            module.objective.fontSize=19;
            module.objective.text=Explanations[CurrentStage];
            module.description.fontSize=17;
            module.description.text=Notes[CurrentStage];
            module.description.transform.parent.Find("PreviewHeading").GetComponent<Text>().text="观察提示";
            bool atEnd=NormalizedProgress>=(CurrentStage+1)/4f;
            playButton.interactable=!IsComplete;
            playButton.GetComponentInChildren<Text>().text=IsPlaying ? "暂停" : IsComplete ? "已结束" : atEnd ? "继续" : "播放";
            nextStageButton.interactable=CurrentStage<3;
            progressFill.rectTransform.anchorMax=new Vector2(NormalizedProgress,1);
            statusLabel.text="节点 "+(CurrentStage+1)+" / 4 · "+(IsPlaying ? "播放中" : IsComplete ? "演示结束" : atEnd ? "段末暂停" : NormalizedProgress>CurrentStage/4f ? "已暂停" : "等待播放");
            for(int i=0;i<stageButtons.Length;i++)
            {
                bool selected=i==CurrentStage;
                stageButtons[i].GetComponent<Image>().color=selected ? new Color32(39,102,94,255) : new Color32(255,254,249,255);
                stageButtons[i].GetComponentInChildren<Text>().color=selected ? Color.white : new Color32(27,65,61,255);
            }
        }
        Material MakeMaterial(Color color, bool unlit=false)
        {
            var m=new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor",color);
            if(!unlit) m.SetFloat("_Smoothness",.15f);
            materials.Add(m); return m;
        }
        Transform Primitive(string name,PrimitiveType type,Material material,Vector3 scale)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.layer=28;
            g.transform.SetParent(displayRoot.transform,false);
            g.transform.localScale=scale;
            Destroy(g.GetComponent<Collider>());
            g.GetComponent<Renderer>().sharedMaterial=material;
            return g.transform;
        }
        void Line(string name,Vector3[] points,Material material,bool loop)
        {
            var g=new GameObject(name,typeof(LineRenderer));g.layer=28;g.transform.SetParent(displayRoot.transform,false);
            var l=g.GetComponent<LineRenderer>();l.useWorldSpace=false;l.loop=loop;l.positionCount=points.Length;l.SetPositions(points);
            l.widthMultiplier=.018f;l.sharedMaterial=material;
        }
        void CreateDisplay()
        {
            displayRoot=new GameObject("ScienceProcessDisplay");displayRoot.transform.position=new Vector3(7000,0,0);
            var brown=MakeMaterial(new Color(.63f,.38f,.17f));
            var blue=MakeMaterial(new Color(.12f,.55f,.72f),true);
            var glass=MakeMaterial(new Color(.46f,.64f,.67f),true);
            var water=MakeMaterial(new Color(.29f,.67f,.77f),true);
            for(int level=0;level<3;level++)
            {
                var ring=new Vector3[64];float y=level==0 ? -.85f : level==1 ? .85f : .42f;
                for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;ring[i]=new Vector3(-2.1f+Mathf.Cos(a)*.86f,y,Mathf.Sin(a)*.86f);}
                Line("CupRing"+level,ring,level==2 ? water : glass,true);
            }
            for(int side=0;side<2;side++)
            {
                float x=-2.1f+(side==0 ? -.86f : .86f);
                Line("CupWall"+side,new[]{new Vector3(x,-.85f,0),new Vector3(x,.85f,0)},glass,false);
            }
            tablet=Primitive("Tablet",PrimitiveType.Cylinder,brown,new Vector3(.72f,.12f,.72f));
            localFragment=Primitive("DetailFragment",PrimitiveType.Cube,brown,new Vector3(.85f,.38f,.65f));
            localFragment.localRotation=Quaternion.Euler(10,20,5);
            for(int i=0;i<8;i++) fragments.Add(Primitive("Fragment"+i,PrimitiveType.Sphere,brown,Vector3.one*.15f));
            for(int i=0;i<24;i++)
            {
                macroDots.Add(Primitive("CupComponent"+i,PrimitiveType.Sphere,blue,Vector3.one*.045f));
                detailDots.Add(Primitive("DetailComponent"+i,PrimitiveType.Sphere,blue,Vector3.one*.085f));
            }
            var cameraObject=new GameObject("ProcessCamera",typeof(Camera));cameraObject.transform.SetParent(displayRoot.transform,false);
            cameraObject.transform.localPosition=new Vector3(0,3,-8);cameraObject.transform.LookAt(displayRoot.transform.position);
            viewCamera=cameraObject.GetComponent<Camera>();viewCamera.cullingMask=1<<28;
            viewCamera.clearFlags=CameraClearFlags.SolidColor;viewCamera.backgroundColor=new Color32(229,235,225,255);
            viewCamera.orthographic=true;viewCamera.orthographicSize=1.25f;viewCamera.nearClipPlane=.1f;viewCamera.farClipPlane=25;
            texture=new RenderTexture(1280,370,24);texture.name="ScienceProcessPreview";texture.Create();viewCamera.targetTexture=texture;processView.texture=texture;
            UpdateVisuals();
        }
        void UpdateVisuals()
        {
            if(displayRoot==null) return;
            float p=NormalizedProgress;
            float contact=Mathf.SmoothStep(0,1,Mathf.Clamp01(p/.25f));
            float scatter=Mathf.Clamp01((p-.16f)/.55f);
            tablet.localPosition=new Vector3(-2.1f,Mathf.Lerp(.2f,-.65f,contact),0);
            tablet.localScale=new Vector3(.72f,.12f,.72f)*Mathf.Lerp(1,.4f,scatter);
            localFragment.localPosition=new Vector3(2.1f,-.25f,0);
            localFragment.localScale=new Vector3(.85f,.38f,.65f)*Mathf.Lerp(1,.65f,p);
            for(int i=0;i<fragments.Count;i++)
            {
                float a=i*Mathf.PI*2/fragments.Count;
                fragments[i].localPosition=tablet.localPosition+new Vector3(Mathf.Cos(a)*scatter*.5f,.06f,Mathf.Sin(a)*scatter*.5f);
                fragments[i].localScale=Vector3.one*Mathf.Lerp(.02f,.15f,scatter);
                fragments[i].gameObject.SetActive(scatter>0);
            }
            for(int i=0;i<detailDots.Count;i++)
            {
                float birth=.08f+i*.021f;
                float travel=Mathf.Clamp01((p-birth)/.43f);
                bool visible=p>birth;
                macroDots[i].gameObject.SetActive(visible);detailDots[i].gameObject.SetActive(visible);
                float a=i*2.39996f;
                var spread=new Vector3(Mathf.Cos(a),Mathf.Sin(a*1.7f)*.55f,Mathf.Sin(a));
                macroDots[i].localPosition=Vector3.Lerp(tablet.localPosition,new Vector3(-2.1f,0,0)+spread*.67f,travel);
                detailDots[i].localPosition=new Vector3(2.1f,-.25f,0)+spread*Mathf.Lerp(.46f,1.12f,travel);
            }
        }
        void OnDisable() { IsPlaying=false;if(displayRoot!=null) displayRoot.SetActive(false); }
        void OnDestroy()
        {
            if(viewCamera!=null) viewCamera.targetTexture=null;
            if(texture!=null){texture.Release();Destroy(texture);}
            foreach(var material in materials) if(material!=null) Destroy(material);
            if(displayRoot!=null) Destroy(displayRoot);
        }
    }
}
