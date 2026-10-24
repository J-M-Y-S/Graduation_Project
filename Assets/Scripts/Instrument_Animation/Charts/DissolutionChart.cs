using UnityEngine;
using XCharts.Runtime;

namespace InstrumentAnimation
{
    /// <summary>
    /// 溶出曲线图表的封装。初始化 LineChart（深色主题、4 条 A/B/C/D 曲线、时间/溶出度坐标轴），
    /// 并提供清空、加点、示例曲线等接口。后续状态机通过 CurveScreenController 调用。
    /// </summary>
    public class DissolutionChart : MonoBehaviour
    {
        public LineChart chart;
        public bool showDemoOnStart = true;

        void Start()
        {
            EnsureChart();
            Configure();
            if (showDemoOnStart) ShowDemoCurve();
        }

        void EnsureChart()
        {
            if (chart == null) chart = GetComponent<LineChart>();
            if (chart == null) chart = gameObject.AddComponent<LineChart>();
        }

        /// <summary>完整初始化图表。可在编辑器与运行时重复调用（内部先重置再重建）。</summary>
        public void Configure()
        {
            EnsureChart();
            chart.Init();
            chart.RemoveData();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var title = chart.EnsureChartComponent<Title>();
            title.show = true;
            title.text = "实时溶出曲线";
            if (font != null) title.labelStyle.textStyle.font = font;

            var legend = chart.EnsureChartComponent<Legend>();
            legend.show = true;

            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Value;
            xAxis.minMaxType = Axis.AxisMinMaxType.Default;
            xAxis.axisName.show = true;
            xAxis.axisName.name = "时间/min";
            if (font != null) xAxis.axisName.labelStyle.textStyle.font = font;

            var yAxis = chart.EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Value;
            yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            yAxis.min = 0;
            yAxis.max = 100;
            yAxis.axisName.show = true;
            yAxis.axisName.name = "溶出度/%";
            if (font != null) yAxis.axisName.labelStyle.textStyle.font = font;

            string[] cups = { "A", "B", "C", "D" };
            foreach (var cup in cups)
            {
                var line = chart.AddSerie<Line>(cup);
                if (line != null)
                {
                    line.symbol.show = false;
                    line.lineType = LineType.Smooth;
                }
            }

            chart.RefreshChart();
        }

        public void ClearCurve()
        {
            if (chart != null) chart.ClearData();
        }

        public void AddPoint(int cupIndex, float timeMinutes, float percent)
        {
            if (chart == null) return;
            if (cupIndex < 0 || cupIndex > 3) return;
            chart.AddData(cupIndex, timeMinutes, percent);
        }

        /// <summary>生成一段示例溶出曲线，用于验收曲线屏效果。</summary>
        public void ShowDemoCurve()
        {
            if (chart == null) return;
            ClearCurve();
            float[] k = { 0.075f, 0.065f, 0.06f, 0.08f };
            for (int i = 0; i <= 30; i++)
            {
                float t = i * 2f;
                for (int c = 0; c < 4; c++)
                    chart.AddData(c, t, 100f * (1f - Mathf.Exp(-k[c] * t)));
            }
        }
    }
}
