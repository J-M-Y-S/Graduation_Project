using UnityEngine;
using UnityEngine.UI;

namespace ScienceEducation
{
    public class ScienceModuleUI : MonoBehaviour
    {
        public Text chapterNumber, chapterTitle, chapterSubtitle, objective, description, pageCounter;
        public Text viewportTitle;
        public Image[] chapterBackgrounds;
        public Text[] chapterLabels;
        public Button previousButton, nextButton;
        public GameObject directory;
        public ScienceBasicsChapter basicsChapter;
        public ScienceInstrumentChapter instrumentChapter;
        public ScienceProcessChapter processChapter;
        public ScienceConditionsChapter conditionsChapter;
        public SciencePracticeGuide practiceGuide;
        public Text progressText;
        public Text[] completionLabels;
        public Text[] directoryLabels;
        public Button markLearnedButton;
        public const string CompletionKey="DissolutionScience.Completed.v1";
        public const string LastChapterKey="DissolutionScience.LastChapter.v1";
        int completedMask;
        public int CompletedCount
        {
            get { int count=0;for(int i=0;i<5;i++)if(IsChapterLearned(i))count++;return count; }
        }
        public int CurrentChapter { get; private set; }

        public static readonly string[] Titles = { "认识片剂与溶出", "认识溶出仪", "观察溶出过程", "实验条件与曲线", "实操前导览" };
        static readonly string[] Subtitles = { "从一片药开始，了解溶出实验的意义。", "从结构入手，认识仪器各部件的作用。", "把随时间发生的变化，看得更清楚。", "联系实验条件，理解曲线表达的信息。", "带着观察目标，准备进入实验操作。" };
        static readonly string[] Objectives = { "认识片剂、崩解与溶出的基本概念，建立观察实验的起点。", "识别溶杯、搅拌桨与机头等部件，了解它们在实验中的作用。", "通过分步示意，观察片剂与介质接触后随时间发生的变化。", "认识实验条件与曲线坐标，学习比较同一时刻的示意结果。", "了解实验各环节的目的，梳理实操前需要关注的信息。" };
        static readonly string[] Previews = { "本章将结合药片展示与知识卡片，介绍片剂和溶出实验。", "本章将结合仪器模型、部件热点与镜头聚焦，帮助你观察结构。", "本章将提供分步播放、暂停和重播，帮助你观察过程。", "本章将提供搅拌条件对比，以及时间与溶出百分比的读图说明。", "本章将通过流程卡片串联准备、设置、运行与结果观察。" };

        void Awake()
        {
            ApplyChineseFont();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            completedMask=PlayerPrefs.GetInt(CompletionKey,0)&31;
            if(markLearnedButton!=null)markLearnedButton.onClick.AddListener(MarkCurrentLearned);
            SelectChapter(Mathf.Clamp(PlayerPrefs.GetInt(LastChapterKey,0),0,4));
        }

        public void ApplyChineseFont()
        {
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial Unicode MS" }, 32);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var label in GetComponentsInChildren<Text>(true)) label.font = font;
        }

        public void SelectChapter(int index)
        {
            CurrentChapter = Mathf.Clamp(index, 0, Titles.Length - 1);
            int i = CurrentChapter;
            directory.SetActive(false);
            chapterNumber.text = "学习单元  /  " + (i + 1).ToString("00");
            chapterTitle.text = Titles[i];
            chapterSubtitle.text = Subtitles[i];
            objective.text = Objectives[i];
            objective.fontSize = 21;
            description.fontSize = 18;
            description.transform.parent.Find("PreviewHeading").GetComponent<Text>().text = "内容预览";
            description.text = i == 0 && basicsChapter != null ? "点击六张知识卡浏览概念；使用左转、右转与复位按钮观察片剂外形。" : Previews[i];
            viewportTitle.text = Titles[i] + " · 三维展示";
            pageCounter.text = (i + 1).ToString("00") + "  /  05";
            previousButton.interactable = i > 0;
            nextButton.interactable = i < Titles.Length - 1;
            if (basicsChapter != null) basicsChapter.gameObject.SetActive(i == 0);
            if (instrumentChapter != null)
            {
                instrumentChapter.gameObject.SetActive(i == 1);
                if (i == 1 && Application.isPlaying) instrumentChapter.ShowOverview();
            }
            if (processChapter != null)
            {
                processChapter.gameObject.SetActive(i == 2);
                if (i == 2 && Application.isPlaying) processChapter.SelectStage(0);
            }
            if (conditionsChapter != null)
            {
                conditionsChapter.gameObject.SetActive(i == 3);
                if (i == 3 && Application.isPlaying) conditionsChapter.ResetComparison();
            }
            if (practiceGuide != null)
            {
                practiceGuide.gameObject.SetActive(i == 4);
                if (i == 4 && Application.isPlaying) practiceGuide.SelectStep(0);
            }
            for (int n = 0; n < chapterBackgrounds.Length; n++)
            {
                chapterBackgrounds[n].color = n == i ? new Color32(39, 102, 94, 255) : new Color32(23, 59, 56, 255);
                chapterLabels[n].color = n == i ? Color.white : new Color32(184, 206, 199, 255);
            }
            RefreshProgress();
            if(Application.isPlaying)SaveProgress();
        }
        public bool IsChapterLearned(int index){return index>=0&&index<5&&(completedMask&(1<<index))!=0;}
        public void MarkCurrentLearned()
        {
            completedMask|=1<<CurrentChapter;
            RefreshProgress();SaveProgress();
            if(practiceGuide!=null&&practiceGuide.gameObject.activeInHierarchy&&practiceGuide.CurrentStep==6)practiceGuide.ShowSummary();
        }
        public void SaveProgress()
        {
            if(!Application.isPlaying)return;
            PlayerPrefs.SetInt(CompletionKey,completedMask);PlayerPrefs.SetInt(LastChapterKey,CurrentChapter);PlayerPrefs.Save();
        }
        void RefreshProgress()
        {
            if(progressText!=null)progressText.text="已学 "+CompletedCount+" / 5 章 · 本机保存";
            if(markLearnedButton!=null)
            {
                bool learned=IsChapterLearned(CurrentChapter);markLearnedButton.interactable=!learned;
                markLearnedButton.GetComponentInChildren<Text>().text=learned?"本章已学":"标记本章已学";
            }
            for(int i=0;i<5;i++)
            {
                if(completionLabels!=null&&i<completionLabels.Length&&completionLabels[i]!=null)completionLabels[i].text=IsChapterLearned(i)?"已学":"";
                if(directoryLabels!=null&&i<directoryLabels.Length&&directoryLabels[i]!=null)directoryLabels[i].text=(i+1).ToString("00")+"     "+Titles[i]+(IsChapterLearned(i)?"  · 已学":"");
            }
        }
        public void Previous() { SelectChapter(CurrentChapter - 1); }
        public void Next() { SelectChapter(CurrentChapter + 1); }
        public void ShowDirectory()
        {
            if (processChapter != null && processChapter.gameObject.activeInHierarchy) processChapter.PausePlayback();
            if (conditionsChapter != null && conditionsChapter.gameObject.activeInHierarchy) conditionsChapter.PausePlayback();
            directory.SetActive(true);
        }
        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (directory.activeSelf) directory.SetActive(false);
            else ShowDirectory();
        }
    }
}
