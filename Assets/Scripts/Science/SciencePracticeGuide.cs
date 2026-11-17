using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ScienceEducation
{
    public class SciencePracticeGuide : MonoBehaviour
    {
        public ScienceModuleUI module;
        public Button[] stepButtons;
        public Button summaryButton, reviewButton, practiceButton;
        public Text detailHeading, detailBody, statusLabel;
        public string practiceScenePath="Assets/Scenes/MainScene.unity";
        public int CurrentStep { get; private set; }
        public bool IsLoading { get; private set; }
        public static readonly string[] StepNames={"准备与检查","介质与温度","参数与取样","投片与定位","运行与观察","结果与整理"};
        static readonly string[] Instructions={
            "进入实验室后，先找到溶出仪、药片存放处与曲线屏。\n\n了解当前实验任务，确认杯位与仪器状态。需要观察杯内或投放片剂时，先确认机头位置适合操作。",
            "向目标溶杯加入液体，并查看当前温度与目标温度。\n\n现有系统提供加水、倒水和加热交互；实际实验的介质种类、体积与温度，应依据所选方法确定。",
            "打开仪器参数面板，查看AB／CD杯转速，并设置各杯取样时间点。\n\n输入后复核单位和先后顺序。取样时间、取样体积和取样空间位置是不同的概念。",
            "从存放处选择药片，按系统提示投入目标溶杯，再确认机头处于运行所需位置。\n\n留意手持药片提示与杯位，避免重复投放。具体投片、定位与启动顺序，应服从选定的教学流程。",
            "启动运行，观察搅拌、计时、取样提示及曲线变化。\n\n现有系统会按设置的时间点给出提示；这些提示与模拟曲线不代表已完成真实的取样和化学检测。",
            "按系统允许的流程停止运行，查看曲线与参数打印信息，并整理杯内状态。\n\n现有实操在取样点全部结束前限制停止。记录本次设置与观察结果，区分教学数据和真实实验数据。"
        };
        static readonly string[] Purposes={"明确任务与操作对象，减少误操作。","确认介质和温度条件，为后续观察建立一致的起点。","明确本次运行条件与观察时点。","确认药片、杯位与运行位置对应正确。","联系操作状态、时间提示与模拟结果。","保留条件与结果记录，为复盘提供依据。"};
        static readonly string[] Observations={"观察目标：仪器位置、溶杯状态、药片来源。","观察目标：杯内是否有液体，当前温度是否达到教学要求。","观察目标：转速单位为rpm，时间单位为min；检查各杯配置。","观察目标：手中拿的是什么、投到哪一杯、机头是否定位。","观察目标：曲线对应哪一杯、当前时间、下一个取样提示。","观察目标：参数与曲线是否对应；打印信息中的数值属于模拟结果。"};

        void Awake()
        {
            for(int i=0;i<stepButtons.Length;i++){int step=i;stepButtons[i].onClick.AddListener(()=>SelectStep(step));}
            summaryButton.onClick.AddListener(ShowSummary);reviewButton.onClick.AddListener(module.ShowDirectory);
            practiceButton.onClick.AddListener(EnterPractice);
        }
        void OnEnable(){if(Application.isPlaying)SelectStep(0);}
        public void SelectStep(int step)
        {
            CurrentStep=Mathf.Clamp(step,0,5);
            detailHeading.text=(CurrentStep+1).ToString("00")+"  "+StepNames[CurrentStep];
            detailBody.text=Instructions[CurrentStep];
            module.objective.text=Purposes[CurrentStep];module.objective.fontSize=21;
            module.description.text=Observations[CurrentStep];module.description.fontSize=18;
            module.description.transform.parent.Find("PreviewHeading").GetComponent<Text>().text="实操观察目标";
            statusLabel.text="系统操作导览 · 实际实验按适用方法执行";
            StyleSelection();
        }
        public void ShowSummary()
        {
            CurrentStep=6;
            detailHeading.text="带着三个认识进入实操";
            detailBody.text="01  外观变化是观察线索，溶出量需要检测依据。\n\n02  比较结果前，先确认片剂、介质与实验条件。\n\n03  教学动画与曲线帮助理解，不代替真实测定。";
            module.objective.text="回顾科普内容，明确接下来要观察和操作的对象。";
            module.objective.fontSize=21;
            module.description.text="按需要返回科普目录复习，或进入实操。下方“标记本章已学”由你自行确认，进度不是考核成绩。";
            module.description.fontSize=18;
            module.description.transform.parent.Find("PreviewHeading").GetComponent<Text>().text="学习与衔接";
            statusLabel.text="已学 "+module.CompletedCount+" / 5 章 · 可随时复习或进入实操";
            StyleSelection();
        }
        void StyleSelection()
        {
            for(int i=0;i<stepButtons.Length;i++)
            {
                bool selected=i==CurrentStep;
                stepButtons[i].GetComponent<Image>().color=selected?new Color32(39,102,94,255):new Color32(255,254,249,255);
                stepButtons[i].GetComponentInChildren<Text>().color=selected?Color.white:new Color32(27,65,61,255);
            }
            summaryButton.GetComponent<Image>().color=CurrentStep==6?new Color32(39,102,94,255):new Color32(255,254,249,255);
            summaryButton.GetComponentInChildren<Text>().color=CurrentStep==6?Color.white:new Color32(27,65,61,255);
        }
        public void EnterPractice()
        {
            if(IsLoading)return;
            if(!Application.CanStreamedLevelBeLoaded(practiceScenePath)) { statusLabel.text="实操场景暂不可用，请检查场景配置。";return; }
            module.SaveProgress();
            IsLoading=true;practiceButton.interactable=false;statusLabel.text="正在进入实操…";
            try
            {
                var operation=SceneManager.LoadSceneAsync(practiceScenePath,LoadSceneMode.Single);
                if(operation==null)throw new System.InvalidOperationException("Scene load did not start.");
            }
            catch(System.Exception)
            {
                IsLoading=false;practiceButton.interactable=true;statusLabel.text="进入实操失败，请稍后重试。";
            }
        }
    }
}
