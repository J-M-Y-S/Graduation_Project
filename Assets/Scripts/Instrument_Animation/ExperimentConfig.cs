using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>
    /// 实验参数的运行时单例，保存用户在参数面板上设置的各项参数。
    /// 后续状态机（DissolutionExperiment）与溶出模型都从这里读取配置。
    /// </summary>
    public class ExperimentConfig : MonoBehaviour
    {
        public static ExperimentConfig Instance { get; private set; }

        [Header("Temperature")]
        public float targetTemperature = 37f;
        public float currentTemperature = 26f;

        [Header("Speed (rpm)")]
        public int initialSpeedAB = 50;
        public int endSpeedAB = 50;
        public int initialSpeedCD = 50;
        public int endSpeedCD = 50;

        [Header("Sampling position (200 / 500 / 900 mL)")]
        public string samplePositionAB = "900";
        public string samplePositionCD = "900";

        [Header("Sampling time points (minutes, per cup)")]
        public float[] sampleTimesA = new float[10];
        public float[] sampleTimesB = new float[10];
        public float[] sampleTimesC = new float[10];
        public float[] sampleTimesD = new float[10];

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>编辑器构建 UI 时调用，保证场景里存在配置单例。</summary>
        public static void EnsureExists()
        {
            if (Instance != null) return;
            var go = GameObject.Find("ExperimentConfig");
            if (go == null) go = new GameObject("ExperimentConfig");
            var cfg = go.GetComponent<ExperimentConfig>();
            if (cfg == null) cfg = go.AddComponent<ExperimentConfig>();
            Instance = cfg;
        }

        public float[] GetSampleTimes(int cupIndex)
        {
            switch (cupIndex)
            {
                case 0: return sampleTimesA;
                case 1: return sampleTimesB;
                case 2: return sampleTimesC;
                default: return sampleTimesD;
            }
        }

        public void SetSampleTime(int cupIndex, int slot, float minutes)
        {
            var arr = GetSampleTimes(cupIndex);
            if (slot >= 0 && slot < arr.Length) arr[slot] = Mathf.Max(0f, minutes);
        }

        public string GetSamplePosition(int pairIndex)
        {
            return pairIndex == 0 ? samplePositionAB : samplePositionCD;
        }

        public void SetSamplePosition(int pairIndex, string volume)
        {
            if (pairIndex == 0) samplePositionAB = volume;
            else samplePositionCD = volume;
        }

        public int GetInitialSpeed(int pairIndex)
        {
            return pairIndex == 0 ? initialSpeedAB : initialSpeedCD;
        }

        public int GetEndSpeed(int pairIndex)
        {
            return pairIndex == 0 ? endSpeedAB : endSpeedCD;
        }

        public void SetInitialSpeed(int pairIndex, int value)
        {
            value = Mathf.Clamp(value, 0, 999);
            if (pairIndex == 0) initialSpeedAB = value;
            else initialSpeedCD = value;
        }

        public void SetEndSpeed(int pairIndex, int value)
        {
            value = Mathf.Clamp(value, 0, 999);
            if (pairIndex == 0) endSpeedAB = value;
            else endSpeedCD = value;
        }
    }
}
