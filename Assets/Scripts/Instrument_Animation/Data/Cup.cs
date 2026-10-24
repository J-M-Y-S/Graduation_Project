using System;
using System.Collections.Generic;

namespace InstrumentAnimation
{
    /// <summary>单个溶杯的运行数据模型。状态机（模块五）会维护 4 个 Cup 实例。</summary>
    [Serializable]
    public class Cup
    {
        public int cupIndex;              // 0=A, 1=B, 2=C, 3=D
        public string cupName;            // "A" / "B" / "C" / "D"
        public bool hasWater;             // 是否已加水（有水时不能重复加水）
        public float currentTemperature = 26f;
        public TabletDefinition tablet;   // 当前放入的药片（null 表示无）
        public float dissolutionProgress; // 溶解进度 0~1
        public float[] sampleTimes = new float[10]; // 10 个取样时间点（分钟）
        public float needleHeight;        // 取样针高度，由介质体积决定

        public Cup(int index)
        {
            cupIndex = index;
            cupName = ((char)('A' + index)).ToString();
        }

        public bool HasTablet => tablet != null;
        public bool CanAddWater => !hasWater;

        public void Reset()
        {
            hasWater = false;
            currentTemperature = 26f;
            tablet = null;
            dissolutionProgress = 0f;
            for (int i = 0; i < sampleTimes.Length; i++) sampleTimes[i] = 0f;
            needleHeight = 0f;
        }

        /// <summary>返回已填写的有效取样时间点（>0）。</summary>
        public List<float> GetValidSampleTimes()
        {
            var list = new List<float>();
            for (int i = 0; i < sampleTimes.Length; i++)
                if (sampleTimes[i] > 0f) list.Add(sampleTimes[i]);
            return list;
        }

        /// <summary>取样时间是否按从小到大递增填写。</summary>
        public bool IsSampleTimesAscending()
        {
            float last = -1f;
            foreach (var t in GetValidSampleTimes())
            {
                if (t < last) return false;
                last = t;
            }
            return true;
        }
    }
}
