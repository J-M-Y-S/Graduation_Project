using UnityEngine;

namespace InstrumentAnimation
{
    public class PanelButtonBridge : MonoBehaviour
    {
        public ParameterPanel panel;
        public GameObject targetPanel;

        public void OpenTarget()
        {
            if (panel != null && targetPanel != null)
                panel.OpenPanel(targetPanel);
        }

        public void Back()
        {
            if (panel != null) panel.Back();
        }

        public void ClosePanel()
        {
            if (panel != null) panel.ClosePanel();
        }
    }
}
