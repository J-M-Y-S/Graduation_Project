using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>实验参数校验：温度、转速、取样时间顺序。</summary>
    public static class ParameterValidator
    {
        public const float MinTemperature = 36.5f;
        public const float MaxTemperature = 37.5f;
        public const int MinRpm = 25;
        public const int MaxRpm = 200;

        public static bool ValidateTemperature(float t, bool showToast = true)
        {
            bool ok = t >= MinTemperature - 0.0001f && t <= MaxTemperature + 0.0001f;
            if (!ok && showToast) Toast.Display("温度超出 37.0±0.5℃ 范围");
            return ok;
        }

        public static bool ValidateSpeed(int rpm, bool showToast = true)
        {
            bool ok = rpm >= MinRpm && rpm <= MaxRpm;
            if (!ok && showToast) Toast.Display("转速应在 " + MinRpm + "~" + MaxRpm + " rpm 之间");
            return ok;
        }

        public static bool ValidateSampleTimes(float[] times, bool showToast = true)
        {
            if (times == null) return true;
            float last = -1f;
            foreach (var t in times)
            {
                if (t <= 0f) continue;
                if (t < last)
                {
                    if (showToast) Toast.Display("取样时间必须从小到大填写");
                    return false;
                }
                last = t;
            }
            return true;
        }
    }
}
