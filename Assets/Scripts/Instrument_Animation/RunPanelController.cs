using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    /// <summary>
    /// “运行”面板的显示刷新。面板每次显示时（OnEnable）从 ExperimentConfig 读取当前配置并刷新。
    /// 运行时间、取样时间、取样倒计时的实时更新由后续状态机接入。
    /// </summary>
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
            if (runTimeText != null) runTimeText.text = "0s";
            if (sampleTimeText != null) sampleTimeText.text = "0s";
            if (countdownText != null) countdownText.text = "0s";
        }

        void RefreshSpeeds()
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null) return;
            if (abSpeedText != null)
                abSpeedText.text = "A: " + cfg.GetInitialSpeed(0) + " rpm    B: " + cfg.GetInitialSpeed(0) + " rpm";
            if (cdSpeedText != null)
                cdSpeedText.text = "C: " + cfg.GetInitialSpeed(1) + " rpm    D: " + cfg.GetInitialSpeed(1) + " rpm";
        }
    }
}
