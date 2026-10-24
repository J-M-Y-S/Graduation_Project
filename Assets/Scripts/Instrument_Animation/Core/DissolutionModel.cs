using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>
    /// 溶出度计算模型：Q(t)=100*(1-e^(-k*t))，k 受温度、转速影响。
    /// 后续状态机与曲线屏都用这里的函数计算实时溶出度。
    /// </summary>
    public static class DissolutionModel
    {
        public const float ReferenceTemperature = 37f;
        public const float ReferenceRpm = 50f;

        /// <summary>溶出百分比。t 为分钟，k 为有效溶解速率（1/min）。</summary>
        public static float DissolutionPercent(float tMinutes, float k)
        {
            if (tMinutes <= 0f || k <= 0f) return 0f;
            return Mathf.Clamp(100f * (1f - Mathf.Exp(-k * tMinutes)), 0f, 100f);
        }

        /// <summary>由药片、温度、转速计算有效溶解速率 k。</summary>
        public static float EffectiveK(TabletDefinition tablet, float temperatureC, float rpm)
        {
            if (tablet == null) return 0f;
            // 温度因子：37℃ 时为 1，偏离 37℃ 按高斯曲线衰减
            float tempFactor = Mathf.Exp(-0.5f * Mathf.Pow((temperatureC - ReferenceTemperature) / 4f, 2f));
            // 转速因子：以 50rpm 为基准线性缩放，最低 0.1
            float rpmFactor = Mathf.Max(0.1f, rpm / ReferenceRpm);
            return tablet.baseK * tempFactor * rpmFactor;
        }

        /// <summary>某杯从投片起经过 elapsedMinutes 后的溶出百分比。</summary>
        public static float PercentAt(Cup cup, float elapsedMinutes, float rpm)
        {
            if (cup == null || !cup.HasTablet) return 0f;
            float k = EffectiveK(cup.tablet, cup.currentTemperature, rpm);
            return DissolutionPercent(elapsedMinutes, k);
        }
    }
}
