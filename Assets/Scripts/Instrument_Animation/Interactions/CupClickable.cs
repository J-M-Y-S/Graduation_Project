using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>溶杯点击：点击后弹出加水/倒水按钮。</summary>
    public class CupClickable : MonoBehaviour
    {
        public int cupIndex; // 0=A, 1=B, 2=C, 3=D

        void OnMouseDown()
        {
            if (DissolutionExperiment.Instance != null)
                DissolutionExperiment.Instance.OnCupClicked(cupIndex);
        }
    }
}
