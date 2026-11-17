using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ScienceEducation;

public static class ScienceModuleBuilder
{
    static readonly Color Ink = new Color32(27, 65, 61, 255);
    static readonly Color Muted = new Color32(101, 120, 113, 255);
    static readonly Color Paper = new Color32(246, 245, 238, 255);
    static readonly Color White = new Color32(255, 254, 249, 255);
    static Font font;

    [MenuItem("Tools/Science/Add Practice Guide")]
    public static string AddPracticeGuide()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Scene1.unity"||Application.isPlaying)return "Open Scene1 in Edit mode first.";
        var ui=Object.FindObjectOfType<ScienceModuleUI>();
        if(ui==null||ui.practiceGuide!=null)return "Missing interface or guide already exists.";
        font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei"},32);
        var frame=ui.transform.Find("DesignFrame");
        var panel=Box("PracticeGuide",frame.Find("DemonstrationArea"),0,0,812,416,new Color32(229,235,225,255));
        Undo.RegisterCreatedObjectUndo(panel.gameObject,"Add practice guide");
        var guide=panel.gameObject.AddComponent<SciencePracticeGuide>();guide.module=ui;
        guide.stepButtons=new Button[6];
        Label("GuideHeading",panel,"实验流程导览",18,15,760,40,24,Ink,true);
        for(int i=0;i<6;i++)
        {
            guide.stepButtons[i]=Button("GuideStep"+i,panel,(i+1).ToString("00")+" "+SciencePracticeGuide.StepNames[i],14,67+i*45,210,39,White,Ink);
            guide.stepButtons[i].GetComponentInChildren<Text>().fontSize=18;
        }
        Box("GuideDivider",panel,244,67,1,270,new Color32(193,208,192,255));
        guide.detailHeading=Label("GuideDetailHeading",panel,"",266,70,520,64,26,Ink,true);
        guide.detailBody=Label("GuideDetailBody",panel,"",266,143,520,200,20,Ink);
        guide.statusLabel=Label("GuideStatus",panel,"",18,340,775,26,15,Muted);
        guide.summaryButton=Button("Summary",panel,"学习总结",14,374,146,31,White,Ink);
        guide.reviewButton=Button("Review",panel,"返回复习",174,374,146,31,White,Ink);
        guide.practiceButton=Button("EnterPractice",panel,"进入实操 →",592,374,204,31,Ink,Color.white);
        ui.practiceGuide=guide;
        ui.progressText=Label("LearningProgress",frame,"已学 0 / 5 章 · 本机保存",755,840,275,32,16,Muted);
        ui.markLearnedButton=Button("MarkLearned",frame,"标记本章已学",490,830,240,48,White,Ink);
        ui.completionLabels=new Text[5];ui.directoryLabels=new Text[5];
        for(int i=0;i<5;i++)
        {
            var chapterButton=ui.chapterBackgrounds[i].transform;
            ui.chapterLabels[i].fontSize=18;ui.chapterLabels[i].rectTransform.sizeDelta=new Vector2(174,66);
            ui.completionLabels[i]=Label("CompletionStatus",chapterButton,"",198,0,36,66,13,new Color32(202,218,181,255));
            ui.completionLabels[i].alignment=TextAnchor.MiddleCenter;
            ui.directoryLabels[i]=ui.directory.transform.Find("DirectoryChapter"+i+"/Label").GetComponent<Text>();
        }
        var buildScenes=new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach(var path in new[]{"Assets/Scenes/Scene1.unity",guide.practiceScenePath})
        {
            var existing=buildScenes.Find(s=>s.path==path);
            if(existing==null)buildScenes.Add(new EditorBuildSettingsScene(path,true));else existing.enabled=true;
        }
        EditorBuildSettings.scenes=buildScenes.ToArray();
        guide.SelectStep(0);ui.ApplyChineseFont();ui.SelectChapter(0);
        EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Practice guide, local progress and scene routing saved.";
    }

    [MenuItem("Tools/Science/Add Conditions Chapter")]
    public static string AddConditionsChapter()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Scene1.unity"||Application.isPlaying)return "Open Scene1 in Edit mode first.";
        var ui=Object.FindObjectOfType<ScienceModuleUI>();
        if(ui==null||ui.conditionsChapter!=null)return "Missing interface or chapter already exists.";
        font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei"},32);
        var panel=Box("ConditionsChapter",ui.transform.Find("DesignFrame/DemonstrationArea"),0,0,812,416,new Color32(229,235,225,255));
        Undo.RegisterCreatedObjectUndo(panel.gameObject,"Add conditions chapter");
        var c=panel.gameObject.AddComponent<ScienceConditionsChapter>();c.module=ui;
        c.topicButtons=new Button[4];c.presetButtons=new Button[2];
        for(int i=0;i<4;i++){c.topicButtons[i]=Button("Topic"+i,panel,ScienceConditionsChapter.Topics[i],14+i*198,12,190,42,White,Ink);c.topicButtons[i].GetComponentInChildren<Text>().fontSize=18;}
        for(int i=0;i<2;i++){c.presetButtons[i]=Button("Preset"+i,panel,i==0?"50 / 75 rpm":"50 / 100 rpm",14+i*154,65,146,34,White,Ink);c.presetButtons[i].GetComponentInChildren<Text>().fontSize=16;}
        Label("FixedConditions",panel,"同一教学片剂与介质 · 温度固定37℃",336,69,460,30,16,Muted);
        var view=Box("ComparisonView",panel,14,112,280,166,Color.white);Object.DestroyImmediate(view.GetComponent<Image>());
        c.comparisonView=view.gameObject.AddComponent<RawImage>();
        c.readoutA=Label("ReadoutA",panel,"",24,282,138,50,16,ScienceComparisonPlot.ColorA,true);
        c.readoutB=Label("ReadoutB",panel,"",166,282,145,50,16,ScienceComparisonPlot.ColorB,true);
        Label("YTitle",panel,"示意溶出百分比 / %",342,103,430,27,15,Muted);
        var plotBox=Box("ComparisonPlot",panel,356,136,420,146,White);Object.DestroyImmediate(plotBox.GetComponent<Image>());
        c.plot=plotBox.gameObject.AddComponent<ScienceComparisonPlot>();c.plot.chapter=c;c.plot.raycastTarget=true;
        for(int i=0;i<=4;i++)
        {
            Label("YTick"+i,panel,(100-i*25).ToString(),316,128+i*36.5f,35,24,13,Muted);
            Label("XTick"+i,panel,(i*15).ToString(),350+i*105,287,45,23,13,Muted);
        }
        Label("XTitle",panel,"教学时间 / min",628,310,165,26,14,Muted);
        c.legend=Label("CurveLegend",panel,"",342,339,450,24,14,Muted);
        Label("ModelNote",panel,"搅拌慢放 · 尺寸变化无定量含义",14,338,320,27,14,Muted);
        var sliderRoot=Box("TimeSlider",panel,18,363,776,16,Color.clear);
        var track=Box("Track",sliderRoot,0,6,776,4,new Color32(176,195,178,255));
        var thumb=Box("Thumb",sliderRoot,0,0,14,16,Ink);thumb.pivot=new Vector2(.5f,.5f);
        c.timeSlider=sliderRoot.gameObject.AddComponent<Slider>();c.timeSlider.minValue=0;c.timeSlider.maxValue=60;
        c.timeSlider.handleRect=thumb;c.timeSlider.targetGraphic=thumb.GetComponent<Image>();
        c.playButton=Button("PlayPause",panel,"播放",14,382,100,30,Ink,Color.white);
        c.replayButton=Button("Replay",panel,"重播",124,382,100,30,White,Ink);
        c.timeLabel=Label("TimeLabel",panel,"",260,386,530,28,16,Muted);
        ui.conditionsChapter=c;c.ResetComparison();ui.ApplyChineseFont();ui.SelectChapter(0);
        EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Conditions chapter added and Scene1 saved.";
    }

    [MenuItem("Tools/Science/Add Process Chapter")]
    public static string AddProcessChapter()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Scene1.unity" || Application.isPlaying) return "Open Scene1 in Edit mode first.";
        var ui=Object.FindObjectOfType<ScienceModuleUI>();
        if(ui==null || ui.processChapter!=null) return "Missing interface or chapter already exists.";
        font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei"},32);
        var panel=Box("ProcessChapter",ui.transform.Find("DesignFrame/DemonstrationArea"),0,0,812,416,new Color32(229,235,225,255));
        Undo.RegisterCreatedObjectUndo(panel.gameObject,"Add process chapter");
        var chapter=panel.gameObject.AddComponent<ScienceProcessChapter>();chapter.module=ui;
        chapter.stageButtons=new Button[4];
        for(int i=0;i<4;i++)
        {
            var b=Button("Stage"+i,panel,(i+1)+" "+ScienceProcessChapter.StageNames[i],14+i*198,12,190,42,White,Ink);
            b.GetComponentInChildren<Text>().fontSize=18;chapter.stageButtons[i]=b;
        }
        Label("MacroCaption",panel,"杯内宏观示意",35,65,330,28,17,Ink,true);
        Label("DetailCaption",panel,"局部概念示意 · 非同比例",433,65,360,28,17,Ink,true);
        var image=Box("ProcessView",panel,14,98,784,226,Color.white);
        Object.DestroyImmediate(image.GetComponent<Image>());
        chapter.processView=image.gameObject.AddComponent<RawImage>();
        Box("ProcessDivider",panel,406,98,1,222,new Color32(193,208,192,255));
        Label("Legend",panel,"棕色：片剂或碎片   蓝点：成分示意（并非气泡，不代表真实数量）",22,325,770,26,15,Muted);
        var track=Box("PlaybackTrack",panel,14,354,784,5,new Color32(193,208,192,255));
        var fill=Box("PlaybackFill",track,0,0,0,0,Ink);
        fill.anchorMin=Vector2.zero;fill.anchorMax=new Vector2(0,1);fill.offsetMin=fill.offsetMax=Vector2.zero;
        chapter.progressFill=fill.GetComponent<Image>();
        chapter.playButton=Button("PlayPause",panel,"播放",14,371,100,34,Ink,Color.white);
        chapter.replayButton=Button("Replay",panel,"重播",124,371,100,34,White,Ink);
        chapter.nextStageButton=Button("NextStage",panel,"下一节点",234,371,144,34,White,Ink);
        chapter.statusLabel=Label("PlaybackStatus",panel,"节点 1 / 4 · 等待播放",402,377,390,28,16,Muted);
        ui.processChapter=chapter;chapter.SelectStage(0);
        ui.ApplyChineseFont();ui.SelectChapter(0);
        EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Process chapter added and Scene1 saved.";
    }

    [MenuItem("Tools/Science/Add Instrument Chapter")]
    public static string AddInstrumentChapter()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path != "Assets/Scenes/Scene1.unity" || Application.isPlaying) return "Open Scene1 in Edit mode first.";
        var ui = Object.FindObjectOfType<ScienceModuleUI>();
        if(ui == null || ui.instrumentChapter != null) return "Missing interface or chapter already exists.";
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei" },32);
        var panel = Box("InstrumentChapter",ui.transform.Find("DesignFrame/DemonstrationArea"),0,0,812,416,new Color32(229,235,225,255));
        Undo.RegisterCreatedObjectUndo(panel.gameObject,"Add instrument chapter");
        var chapter = panel.gameObject.AddComponent<ScienceInstrumentChapter>();
        chapter.module=ui;
        chapter.instrumentPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dissolution_Apparatus.prefab");
        chapter.partButtons=new Button[5]; chapter.hotspots=new Button[5]; chapter.hotspotLeaders=new RectTransform[5];
        var image=Box("InstrumentView",panel,14,64,784,294,Color.white);
        Object.DestroyImmediate(image.GetComponent<Image>());
        chapter.instrumentView=image.gameObject.AddComponent<RawImage>();
        for(int i=0;i<5;i++)
        {
            chapter.partButtons[i]=Button("Part"+i,panel,(i+1)+" "+ScienceInstrumentChapter.Names[i],14+i*158,12,152,42,White,Ink);
            chapter.partButtons[i].GetComponentInChildren<Text>().fontSize=18;
            var line=Box("HotspotLeader"+i,image,0,0,1,2,Ink);
            line.anchorMin=line.anchorMax=Vector2.zero;line.pivot=new Vector2(0,.5f);
            line.GetComponent<Image>().raycastTarget=false;chapter.hotspotLeaders[i]=line;
            var b=Button("Hotspot"+i,image,(i+1).ToString(),0,0,36,36,Ink,Color.white);
            var badge=(RectTransform)b.transform; badge.anchorMin=badge.anchorMax=Vector2.zero;badge.pivot=new Vector2(.5f,.5f);
            var label=b.GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=Vector2.zero;label.rectTransform.sizeDelta=new Vector2(36,36);label.alignment=TextAnchor.MiddleCenter;
            chapter.hotspots[i]=b;
        }
        chapter.overviewButton=Button("Overview",panel,"仪器全景",14,369,144,36,White,Ink);
        chapter.demonstrationButton=Button("Demonstration",panel,"播放示意",170,369,144,36,White,Ink);
        chapter.statusLabel=Label("InstrumentStatus",panel,"仪器全景 · 点击编号观察部件",334,374,465,30,16,Muted);
        ui.instrumentChapter=chapter;
        ui.ApplyChineseFont();ui.SelectChapter(0);
        EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Instrument chapter added and Scene1 saved.";
    }

    [MenuItem("Tools/Science/Add Basics Chapter")]
    public static string AddBasicsChapter()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Scene1.unity" || Application.isPlaying) return "Open Scene1 in Edit mode first.";
        var ui = Object.FindObjectOfType<ScienceModuleUI>();
        if (ui == null) return "Build the interface first.";
        if (ui.basicsChapter != null) return "Basics chapter already exists.";
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei" },32);
        var area = ui.transform.Find("DesignFrame/DemonstrationArea");
        var panel = Box("BasicsChapter",area,0,0,812,416,new Color32(229,235,225,255));
        Undo.RegisterCreatedObjectUndo(panel.gameObject,"Add basics chapter");
        var chapter = panel.gameObject.AddComponent<ScienceBasicsChapter>();
        ui.basicsChapter = chapter;
        chapter.tabs = new Button[6];
        for(int i=0;i<6;i++)
        {
            var b = Button("KnowledgeCard"+i,panel,ScienceBasicsChapter.TabNames[i],12+i*132,12,128,42,White,Ink);
            b.GetComponentInChildren<Text>().fontSize=17;
            chapter.tabs[i]=b;
        }
        var rawBox = Box("TabletView",panel,18,78,290,218,Color.white);
        Object.DestroyImmediate(rawBox.GetComponent<Image>());
        chapter.tabletView = rawBox.gameObject.AddComponent<RawImage>();
        chapter.tabletView.color=Color.white;
        Label("TabletCaption",panel,"通用片剂 · 外形示意",24,300,290,30,17,Muted);
        chapter.rotateLeft = Button("RotateLeft",panel,"左转",20,345,90,40,White,Ink);
        chapter.rotateRight = Button("RotateRight",panel,"右转",116,345,90,40,White,Ink);
        chapter.resetView = Button("ResetView",panel,"复位",212,345,90,40,White,Ink);
        Box("CardDivider",panel,324,78,1,310,new Color32(193,208,192,255));
        chapter.cardHeading = Label("CardHeading",panel,"",344,78,444,60,25,Ink,true);
        chapter.cardBody = Label("CardBody",panel,"",344,147,444,208,19,Ink);
        chapter.cardSource = Label("CardSource",panel,"",344,361,444,44,16,Muted);
        chapter.cardCounter = Label("CardCounter",panel,"",686,393,100,22,16,Muted);
        chapter.SelectCard(0);
        ui.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        ui.ApplyChineseFont();
        ui.SelectChapter(0);
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Basics chapter added and Scene1 saved.";
    }

    static RectTransform Box(string name, Transform parent, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = color;
        return r;
    }
    static Text Label(string name, Transform p, string value, float x, float y, float w, float h, int size, Color color, bool bold = false)
    {
        var r = Box(name, p, x, y, w, h, Color.clear);
        Object.DestroyImmediate(r.GetComponent<Image>());
        var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = value;
        t.fontSize = size; t.color = color; t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.raycastTarget = false; t.verticalOverflow = VerticalWrapMode.Truncate;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.lineSpacing = 1.22f;
        return t;
    }
    static Button Button(string name, Transform p, string text, float x, float y, float w, float h, Color color, Color textColor)
    {
        var r = Box(name, p, x, y, w, h, color);
        var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = r.GetComponent<Image>();
        var colors = b.colors; colors.highlightedColor = new Color(.85f,.93f,.89f); colors.pressedColor = new Color(.68f,.83f,.77f); b.colors = colors;
        var t = Label("Label", r, text, 16, 0, w - 32, h, 20, textColor);
        t.alignment = TextAnchor.MiddleLeft;
        return b;
    }

    [MenuItem("Tools/Science/Build Scene1 Interface")]
    public static string Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Scene1.unity" || Application.isPlaying) return "Build aborted: open Scene1 in Edit mode.";
        if (GameObject.Find("ScienceModuleCanvas") != null) return "Interface already exists.";
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei" },32);
        var root = new GameObject("ScienceModuleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ScienceModuleUI));
        Undo.RegisterCreatedObjectUndo(root, "Create science interface");
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = .5f;
        var frame = Box("DesignFrame", root.transform, 0,0,1600,900,Paper);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f,.5f); frame.anchoredPosition = Vector2.zero;
        var ui = root.GetComponent<ScienceModuleUI>();
        var sidebar = Box("Chapters",frame,0,0,270,900,new Color32(23,59,56,255));
        Label("Brand",sidebar,"本草 · 溶出",28,34,220,44,30,Color.white,true);
        Label("Module",sidebar,"三维仿真系统  /  科普",28,88,220,30,17,new Color32(176,204,192,255));
        Box("Divider",sidebar,28,143,214,1,new Color32(65,99,90,255));
        Label("NavigationHeading",sidebar,"学习章节",28,174,200,30,17,new Color32(176,204,192,255));
        ui.chapterBackgrounds = new Image[5]; ui.chapterLabels = new Text[5];
        for(int i=0;i<5;i++)
        {
            var b=Button("Chapter"+i,sidebar,(i+1).ToString("00")+"   "+ScienceModuleUI.Titles[i],16,226+i*83,238,66,Ink,Color.white);
            ui.chapterBackgrounds[i]=b.GetComponent<Image>(); ui.chapterLabels[i]=b.GetComponentInChildren<Text>();
            UnityEventTools.AddIntPersistentListener(b.onClick,ui.SelectChapter,i);
        }
        Label("LearningRoute",sidebar,"认识概念 → 观察过程\n理解条件 → 准备实操",28,726,220,78,18,new Color32(176,204,192,255));
        Label("ModuleFooter",sidebar,"中药片剂溶出过程",28,843,220,30,16,new Color32(176,204,192,255));
        Label("SystemTitle",frame,"中药片剂溶出过程 · 科普课堂",310,30,890,44,27,Ink,true);
        Label("SystemSubtitle",frame,"观察仪器与片剂，理解实验背后的知识",310,80,870,30,17,Muted);
        var back=Button("DirectoryButton",frame,"科普目录",1390,34,170,48,White,Ink);
        UnityEventTools.AddPersistentListener(back.onClick,ui.ShowDirectory);
        Box("HeaderLine",frame,310,126,1250,1,new Color32(216,224,211,255));
        ui.chapterNumber=Label("ChapterNumber",frame,"",310,153,900,30,17,Muted);
        ui.chapterTitle=Label("ChapterTitle",frame,"",310,195,1000,66,44,Ink,true);
        ui.chapterSubtitle=Label("ChapterSubtitle",frame,"",310,276,1000,38,21,Muted);
        var view=Box("DemonstrationArea",frame,310,340,812,416,new Color32(229,235,225,255));
        ui.viewportTitle=Label("ViewportTitle",view,"",26,22,740,35,19,Ink,true);
        Box("ViewportRule",view,26,72,760,1,new Color32(193,208,192,255));
        Label("ViewportMark",view,"三 维 观 察",45,127,730,58,37,Ink,true).alignment=TextAnchor.MiddleCenter;
        Label("ViewportPlaceholder",view,"模型与过程演示将在后续步骤接入",45,209,730,45,21,Muted).alignment=TextAnchor.MiddleCenter;
        Label("ViewportInstruction",view,"选择左侧章节，预览各单元的学习方向",45,274,730,40,17,Muted).alignment=TextAnchor.MiddleCenter;
        Label("ViewportCaption",view,"展示区域  /  仪器 · 片剂 · 曲线",26,366,750,30,16,Muted);
        var knowledge=Box("KnowledgePanel",frame,1146,340,414,416,White);
        Box("Accent",knowledge,0,0,5,416,new Color32(167,139,76,255));
        Label("ObjectiveHeading",knowledge,"本节学习目标",26,22,360,40,24,Ink,true);
        ui.objective=Label("Objective",knowledge,"",26,84,358,120,21,Ink);
        Box("KnowledgeDivider",knowledge,26,223,362,1,new Color32(224,228,216,255));
        Label("PreviewHeading",knowledge,"内容预览",26,245,360,34,18,Muted,true);
        ui.description=Label("Description",knowledge,"",26,295,358,105,18,Muted);
        Label("Hint",frame,"学习提示：按 Esc 可打开或收起科普目录",310,777,870,32,16,Muted);
        ui.previousButton=Button("Previous",frame,"← 上一节",310,830,166,48,White,Ink);
        UnityEventTools.AddPersistentListener(ui.previousButton.onClick,ui.Previous);
        ui.pageCounter=Label("PageCounter",frame,"",1035,836,150,40,20,Muted);
        ui.nextButton=Button("Next",frame,"下一节 →",1380,830,180,48,Ink,Color.white);
        UnityEventTools.AddPersistentListener(ui.nextButton.onClick,ui.Next);
        var directory=Box("Directory",frame,286,140,1298,660,Paper); ui.directory=directory.gameObject;
        Label("DirectoryHeading",directory,"从这里开始探索",40,30,1100,75,42,Ink,true);
        Label("DirectoryDescription",directory,"按顺序学习，或选择你想了解的章节。",40,112,1100,42,22,Muted);
        for(int i=0;i<5;i++)
        {
            var b=Button("DirectoryChapter"+i,directory,(i+1).ToString("00")+"     "+ScienceModuleUI.Titles[i],40,188+i*83,1210,62,White,Ink);
            UnityEventTools.AddIntPersistentListener(b.onClick,ui.SelectChapter,i);
        }
        if(Object.FindObjectOfType<EventSystem>()==null)
        {
            var events=new GameObject("ScienceEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(events,"Create science event system");
        }
        ui.SelectChapter(0);
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=root;
        return "Scene1 interface created and saved.";
    }
}
