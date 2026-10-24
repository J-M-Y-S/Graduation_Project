using UnityEngine;

namespace InstrumentAnimation
{
    public class MainScreenClicker : MonoBehaviour
    {
        public GameObject parameterPanel;
        private void OnMouseDown()
        {
            if (DissolutionExperiment.IsWaterUIOpen()) return; // 加水/倒水弹窗打开时不响应
            if (parameterPanel != null)
                parameterPanel.SetActive(!parameterPanel.activeSelf);
        }
    }
}
