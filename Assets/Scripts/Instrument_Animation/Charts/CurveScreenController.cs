using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>
    /// 溶出仪旁边的曲线屏幕控制器：负责屏幕显隐，并为后续状态机提供实时数据入口。
    /// </summary>
    public class CurveScreenController : MonoBehaviour
    {
        public static CurveScreenController Instance { get; private set; }

        public DissolutionChart chart;

        void Awake()
        {
            Instance = this;
            if (chart == null) chart = GetComponentInChildren<DissolutionChart>();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void ClearCurve()
        {
            if (chart != null) chart.ClearCurve();
        }

        public void AddCupPoint(int cupIndex, float timeMinutes, float percent)
        {
            if (chart != null) chart.AddPoint(cupIndex, timeMinutes, percent);
        }

        public void ShowDemoCurve()
        {
            if (chart != null) chart.ShowDemoCurve();
        }
    }
}
