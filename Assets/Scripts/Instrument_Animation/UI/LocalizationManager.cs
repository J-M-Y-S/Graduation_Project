using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    public enum UILanguage { Chinese, English }

    /// <summary>中英文本切换。按原始中文文本做映射，切换时刷新场景内所有 Text。</summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }
        public UILanguage currentLanguage = UILanguage.Chinese;

        public bool IsEnglish => currentLanguage == UILanguage.English;

        static readonly Dictionary<string, string> ZhToEn = new Dictionary<string, string>
        {
            { "溶出度测试仪", "Dissolution Tester" },
            { "主菜单", "Main Menu" },
            { "运行", "Run" },
            { "取样点", "Sampling Points" },
            { "语言", "Language" },
            { "说明", "Help" },
            { "关闭", "Close" },
            { "返回", "Back" },
            { "开始", "Start" },
            { "停止", "Stop" },
            { "打印", "Print" },
            { "初始转速", "Initial Speed" },
            { "末端转速", "End Speed" },
            { "取样位置", "Sampling Position" },
            { "机头上升", "Raise Head" },
            { "机头下降", "Lower Head" },
            { "温度", "Temperature" },
            { "加热", "Heat" },
            { "预加热", "Preheat" },
            { "运行时间", "Run Time" },
            { "取样时间", "Sampling Time" },
            { "取样倒计时", "Sampling Countdown" },
            { "AB杯", "AB Cups" },
            { "CD杯", "CD Cups" },
            { "取样点时间", "Sampling Times" },
            { "点击杯子查看取样时间点", "Click a cup to view sampling times" },
            { "10 个取样时间点（分钟）", "10 sampling points (min)" },
            { "根据介质容积选择对应取样点位置", "Select sampling position by volume" },
            { "温度设置", "Temperature Settings" },
            { "目标温度", "Target Temperature" },
            { "校验范围 37.0 ± 0.5 ℃", "Range 37.0 ± 0.5 ℃" },
            { "当前温度", "Current Temperature" },
            { "加热设置", "Heating Settings" },
            { "开始加热", "Start Heating" },
            { "预加热设置", "Preheat Settings" },
            { "当前日期", "Current Date" },
            { "当前时间", "Current Time" },
            { "设定日期", "Set Date" },
            { "设定时间", "Set Time" },
            { "应用", "Apply" },
            { "取消应用", "Cancel Apply" },
            { "转速设置", "Speed Settings" },
            { "简体中文", "Simplified Chinese" },
            { "运行模式说明", "Run Mode Help" },
            { "取样点说明", "Sampling Help" },
            { "转速设置说明", "Speed Help" },
            { "温度设置说明", "Temperature Help" },
            { "打印说明", "Print Help" },
            { "设备信息", "Device Info" },
            { "加水", "Add Water" },
            { "倒水", "Pour Water" },
            { "投片", "Add Tablet" },
            { "取消", "Cancel" }
        };

        public static void EnsureExists()
        {
            if (Instance != null) return;
            var go = GameObject.Find("LocalizationManager");
            if (go == null) go = new GameObject("LocalizationManager");
            var lm = go.GetComponent<LocalizationManager>();
            if (lm == null) lm = go.AddComponent<LocalizationManager>();
            Instance = lm;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SetLanguage(UILanguage lang)
        {
            currentLanguage = lang;
            ApplyAll();
        }

        public void ApplyAll()
        {
            var texts = Resources.FindObjectsOfTypeAll<Text>();
            foreach (var t in texts)
            {
                if (t == null || !t.gameObject.scene.IsValid()) continue;
                if (string.IsNullOrEmpty(t.text)) continue;
                if (t.GetComponent<LocalizationIgnore>() != null) continue;
                if (t.transform.parent != null && t.transform.parent.GetComponent<LocalizationIgnore>() != null) continue;
                if (IsEnglish)
                {
                    string en;
                    if (ZhToEn.TryGetValue(t.text, out en)) t.text = en;
                }
                else
                {
                    foreach (var kv in ZhToEn)
                    {
                        if (kv.Value == t.text) { t.text = kv.Key; break; }
                    }
                }
            }
        }
    }
}
