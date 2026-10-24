using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    public enum ConfigField
    {
        TargetTemperature,
        InitialSpeedAB,
        EndSpeedAB,
        InitialSpeedCD,
        EndSpeedCD,
        SampleTime,     // 使用 cupIndex(0..3) + slotIndex(0..9)
        SamplePosition  // 使用 cupIndex(0=AB / 1=CD) + value("200"/"500"/"900")
    }

    /// <summary>
    /// 把 UI 控件绑定到 ExperimentConfig 的桥接组件。
    /// 所有监听都通过 UnityEventTools 持久化，场景保存后依然有效。
    /// </summary>
    public class ConfigFieldBinding : MonoBehaviour
    {
        public ConfigField field;
        public int cupIndex;
        public int slotIndex;
        public string value;
        public Text valueText;

        public void SetIntFromText(string s)
        {
            int v;
            if (!int.TryParse(s, out v)) v = 0;
            SetInt(v);
        }

        public void SetInt(int v)
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null) return;
            switch (field)
            {
                case ConfigField.InitialSpeedAB: cfg.SetInitialSpeed(0, v); break;
                case ConfigField.EndSpeedAB: cfg.SetEndSpeed(0, v); break;
                case ConfigField.InitialSpeedCD: cfg.SetInitialSpeed(1, v); break;
                case ConfigField.EndSpeedCD: cfg.SetEndSpeed(1, v); break;
            }
            ParameterValidator.ValidateSpeed(v);
        }

        public void SetFloatFromText(string s)
        {
            float v;
            if (!float.TryParse(s, out v)) v = 0f;
            SetFloat(v);
        }

        public void SetFloat(float v)
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null) return;
            switch (field)
            {
                case ConfigField.TargetTemperature:
                    cfg.targetTemperature = Mathf.Clamp(v, 0f, 100f);
                    ParameterValidator.ValidateTemperature(cfg.targetTemperature);
                    break;
                case ConfigField.SampleTime:
                    cfg.SetSampleTime(cupIndex, slotIndex, v);
                    ParameterValidator.ValidateSampleTimes(cfg.GetSampleTimes(cupIndex));
                    break;
            }
            RefreshValueText();
        }

        public void TempUp()
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null) return;
            cfg.targetTemperature = Mathf.Clamp(cfg.targetTemperature + 0.1f, 0f, 100f);
            ParameterValidator.ValidateTemperature(cfg.targetTemperature);
            RefreshValueText();
        }

        public void TempDown()
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null) return;
            cfg.targetTemperature = Mathf.Clamp(cfg.targetTemperature - 0.1f, 0f, 100f);
            ParameterValidator.ValidateTemperature(cfg.targetTemperature);
            RefreshValueText();
        }

        public void OnVolumeToggle(bool on)
        {
            if (!on) return;
            SelectVolume(value);
        }

        public void SelectVolume(string vol)
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null) return;
            cfg.SetSamplePosition(cupIndex, vol);
            value = vol;
        }

        public void RefreshValueText()
        {
            var cfg = ExperimentConfig.Instance;
            if (valueText == null || cfg == null) return;
            if (field == ConfigField.TargetTemperature)
                valueText.text = cfg.targetTemperature.ToString("F1") + "°C";
        }
    }
}
