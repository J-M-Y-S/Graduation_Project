using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    /// <summary>运行面板的实时显示：当前转速、运行时间、取样时间、取样倒计时。</summary>
    public class RunPanelController : MonoBehaviour
    {
        public Text abSpeedText;
        public Text cdSpeedText;
        public Text runTimeText;
        public Text sampleTimeText;
        public Text countdownText;

        void OnEnable()
        {
            RefreshSpeeds();
        }

        void Update()
        {
            var exp = DissolutionExperiment.Instance;
            if (exp == null) return;
            RefreshSpeeds();
            if (runTimeText != null) runTimeText.text = Format(exp.RunTimeMinutes);
            if (sampleTimeText != null) sampleTimeText.text = Format(exp.SampleElapsedMinutes);
            if (countdownText != null)
                countdownText.text = exp.CountdownSeconds > 0.05f ? Mathf.CeilToInt(exp.CountdownSeconds) + "s" : "0s";
        }

        void RefreshSpeeds()
        {
            var cfg = ExperimentConfig.Instance;
            var exp = DissolutionExperiment.Instance;
            if (cfg == null) return;
            int ab = (exp != null && exp.IsRunning) ? exp.CurrentSpeedAB : cfg.initialSpeedAB;
            int cd = (exp != null && exp.IsRunning) ? exp.CurrentSpeedCD : cfg.initialSpeedCD;
            if (abSpeedText != null)
                abSpeedText.text = "A: " + ab + " rpm    B: " + ab + " rpm";
            if (cdSpeedText != null)
                cdSpeedText.text = "C: " + cd + " rpm    D: " + cd + " rpm";
        }

        static string Format(float minutes)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(minutes * 60f));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }
}
