using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScienceEducation
{
    public class ScienceConditionsChapter : MonoBehaviour
    {
        public ScienceModuleUI module;
        public ScienceComparisonPlot plot;
        public RawImage comparisonView;
        public Button[] topicButtons, presetButtons;
        public Button playButton, replayButton;
        public Slider timeSlider;
        public Text readoutA, readoutB, timeLabel, legend;
        public float TimeMinutes { get; private set; }
        public bool IsPlaying { get; private set; }
        public int CurrentPreset { get; private set; }
        public int CurrentTopic { get; private set; }
        public int RpmA => 50;
        public int RpmB => CurrentPreset==0?75:100;
        public static readonly string[] Topics={"搅拌条件对比","曲线读图","温度控制","介质与取样"};
        static readonly string[] Objectives={
            "只改变搅拌转速，比较同一教学时间的两组结果。片剂、介质与温度设定相同，观察两条示意曲线。",
            "横轴是教学时间，纵轴是示意溶出百分比。竖线标出当前观察时点；两条曲线上的标记给出同一时刻的读数。",
            "温度是需要控制的实验条件。本例固定为37℃，仅作教学设定。实际温度及允许偏差应按适用方法确认。",
            "介质的组成、体积及取样条件需要明确。比较结果前，应先确认这些条件与检测对象是否一致。"
        };
        static readonly string[] Notes={
            "本例设Q=100×(1−e^(−kt))，k=0.035×rpm/50。系数与数值为教学设定，不能据此认定转速越高越好。",
            "拖动曲线或下方时间轴可暂停并定位。全程曲线为预设教学数据；杯内尺寸变化无定量含义，不代表实测结果。",
            "温度可能影响溶出相关过程，应保持并记录规定条件。本章不进行温度定量推算，也不把37℃视为普遍的最优温度。",
            "取样时间、取样体积与取样位置是不同概念。检测值须按规定方法计算，必要时考虑取样、补液等因素。参考：FDA溶出试验指南。"
        };
        public const string ReferenceUrl="https://www.fda.gov/media/70936/download";
        GameObject displayRoot;
        readonly List<Material> materials=new List<Material>();
        readonly Transform[] tablets=new Transform[2];
        readonly Transform[] paddles=new Transform[2];
        Camera viewCamera;
        RenderTexture texture;

        void Awake()
        {
            for(int i=0;i<topicButtons.Length;i++){int n=i;topicButtons[i].onClick.AddListener(()=>SelectTopic(n));}
            for(int i=0;i<presetButtons.Length;i++){int n=i;presetButtons[i].onClick.AddListener(()=>SelectPreset(n));}
            playButton.onClick.AddListener(TogglePlayback);replayButton.onClick.AddListener(Replay);
            timeSlider.onValueChanged.AddListener(SeekTime);
        }
        void OnEnable()
        {
            if(!Application.isPlaying)return;
            if(displayRoot==null)CreateDisplay();
            displayRoot.SetActive(true);ResetComparison();
        }
        public void ResetComparison(){CurrentTopic=0;CurrentPreset=0;TimeMinutes=0;IsPlaying=false;Refresh();}
        public void SelectTopic(int topic){CurrentTopic=Mathf.Clamp(topic,0,3);PausePlayback();}
        public void SelectPreset(int preset){CurrentPreset=Mathf.Clamp(preset,0,1);TimeMinutes=0;IsPlaying=false;Refresh();}
        public void SeekTime(float minutes){TimeMinutes=Mathf.Clamp(minutes,0,60);IsPlaying=false;Refresh();}
        public void TogglePlayback(){if(TimeMinutes>=60)TimeMinutes=0;IsPlaying=!IsPlaying;Refresh();}
        public void Replay(){TimeMinutes=0;IsPlaying=true;Refresh();}
        public void PausePlayback(){IsPlaying=false;Refresh();}
        public float PercentAt(float minutes,int series)
        {
            // The coefficients define a teaching example, not a fitted product model.
            float k=.035f*(series==0?RpmA:RpmB)/50;
            return InstrumentAnimation.DissolutionModel.DissolutionPercent(Mathf.Clamp(minutes,0,60),k);
        }
        public void Advance(float deltaSeconds)
        {
            if(!IsPlaying||deltaSeconds<=0)return;
            TimeMinutes=Mathf.Min(60,TimeMinutes+deltaSeconds*3);
            if(TimeMinutes>=60)IsPlaying=false;
            Refresh();
        }
        void Update(){Advance(Time.unscaledDeltaTime);}
        void Refresh()
        {
            module.objective.text=Objectives[CurrentTopic];module.objective.fontSize=19;
            module.description.text=Notes[CurrentTopic];module.description.fontSize=17;
            module.description.transform.parent.Find("PreviewHeading").GetComponent<Text>().text="理解与边界";
            readoutA.text="A  "+RpmA+" rpm\n示意值 "+PercentAt(TimeMinutes,0).ToString("F1")+"%";
            readoutB.text="B  "+RpmB+" rpm\n示意值 "+PercentAt(TimeMinutes,1).ToString("F1")+"%";
            legend.text="绿线 A："+RpmA+" rpm     棕线 B："+RpmB+" rpm";
            timeLabel.text="教学时间 "+TimeMinutes.ToString("F1")+" min"+(IsPlaying?" · 播放中":" · 已暂停");
            playButton.GetComponentInChildren<Text>().text=IsPlaying?"暂停":"播放";
            timeSlider.SetValueWithoutNotify(TimeMinutes);
            for(int i=0;i<topicButtons.Length;i++)Style(topicButtons[i],i==CurrentTopic);
            for(int i=0;i<presetButtons.Length;i++)Style(presetButtons[i],i==CurrentPreset);
            plot.SetVerticesDirty();UpdateVisuals();
        }
        void Style(Button b,bool selected)
        {
            b.GetComponent<Image>().color=selected?new Color32(39,102,94,255):new Color32(255,254,249,255);
            b.GetComponentInChildren<Text>().color=selected?Color.white:new Color32(27,65,61,255);
        }
        Material Material(Color color)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.SetColor("_BaseColor",color);materials.Add(m);return m;
        }
        Transform Primitive(string name,PrimitiveType type,Material material,Vector3 scale,Vector3 position)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.layer=27;go.transform.SetParent(displayRoot.transform,false);
            go.transform.localScale=scale;go.transform.localPosition=position;
            Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        void CreateDisplay()
        {
            displayRoot=new GameObject("ScienceConditionsDisplay");displayRoot.transform.position=new Vector3(8000,0,0);
            var brown=Material(new Color(.63f,.38f,.17f));var glass=Material(new Color(.46f,.64f,.67f));
            var steel=Material(new Color(.33f,.44f,.43f));
            for(int cup=0;cup<2;cup++)
            {
                float x=cup==0?-1.2f:1.2f;
                for(int ring=0;ring<3;ring++)
                {
                    var points=new Vector3[64];float y=ring==0?-.8f:ring==1?.8f:.4f;
                    for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;points[i]=new Vector3(x+Mathf.Cos(a)*.8f,y,Mathf.Sin(a)*.8f);}
                    var go=new GameObject("CupRing"+cup+ring,typeof(LineRenderer));go.layer=27;go.transform.SetParent(displayRoot.transform,false);
                    var line=go.GetComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=64;line.SetPositions(points);
                    line.widthMultiplier=.025f;line.sharedMaterial=glass;
                }
                for(int side=0;side<2;side++)Primitive("CupWall"+cup+side,PrimitiveType.Cube,glass,new Vector3(.025f,1.6f,.025f),new Vector3(x+(side==0?-.8f:.8f),0,0));
                tablets[cup]=Primitive("Tablet"+cup,PrimitiveType.Cylinder,brown,new Vector3(.55f,.1f,.55f),new Vector3(x,-.65f,0));
                Primitive("Shaft"+cup,PrimitiveType.Cylinder,steel,new Vector3(.035f,.6f,.035f),new Vector3(x,.2f,0));
                paddles[cup]=Primitive("Paddle"+cup,PrimitiveType.Cube,steel,new Vector3(.65f,.16f,.08f),new Vector3(x,-.35f,0));
            }
            var cameraObject=new GameObject("ConditionsCamera",typeof(Camera));cameraObject.transform.SetParent(displayRoot.transform,false);
            cameraObject.transform.localPosition=new Vector3(0,3,-8);cameraObject.transform.LookAt(displayRoot.transform.position);
            viewCamera=cameraObject.GetComponent<Camera>();viewCamera.cullingMask=1<<27;viewCamera.clearFlags=CameraClearFlags.SolidColor;
            viewCamera.backgroundColor=new Color32(229,235,225,255);viewCamera.orthographic=true;viewCamera.orthographicSize=1.4f;
            viewCamera.nearClipPlane=.1f;viewCamera.farClipPlane=25;
            texture=new RenderTexture(640,380,24);texture.name="ScienceConditionsPreview";texture.Create();viewCamera.targetTexture=texture;comparisonView.texture=texture;
        }
        void UpdateVisuals()
        {
            if(displayRoot==null)return;
            for(int i=0;i<2;i++)
            {
                tablets[i].localScale=new Vector3(.55f,.1f,.55f)*Mathf.Lerp(1,.45f,PercentAt(TimeMinutes,i)/100);
                paddles[i].localRotation=Quaternion.Euler(0,TimeMinutes*(i==0?RpmA:RpmB)*.6f,0);
            }
        }
        void OnDisable(){IsPlaying=false;if(displayRoot!=null)displayRoot.SetActive(false);}
        void OnDestroy()
        {
            if(viewCamera!=null)viewCamera.targetTexture=null;
            if(texture!=null){texture.Release();Destroy(texture);}
            foreach(var m in materials)if(m!=null)Destroy(m);
            if(displayRoot!=null)Destroy(displayRoot);
        }
    }
}
