using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    /// <summary>加热面板的实时显示：当前温度、目标温度、加热进度条。</summary>
    public class HeatingPanelUI : MonoBehaviour
    {
        public Text currentText;
        public Text targetText;
        public RectTransform fillRect;

        void Update()
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg != null)
            {
                if (currentText != null) currentText.text = cfg.currentTemperature.ToString("F1") + "°C";
                if (targetText != null) targetText.text = cfg.targetTemperature.ToString("F1") + "°C";
            }
            if (fillRect != null)
            {
                var exp = DissolutionExperiment.Instance;
                float p = exp != null ? exp.heatingProgress : 0f;
                fillRect.anchorMax = new Vector2(Mathf.Clamp01(p), 1f);
            }
        }
    }
}
