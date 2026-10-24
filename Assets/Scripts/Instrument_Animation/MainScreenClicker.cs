using UnityEngine;

namespace InstrumentAnimation
{
    public class MainScreenClicker : MonoBehaviour
    {
        public GameObject parameterPanel;
        private void OnMouseDown()
        {
            if (parameterPanel != null)
                parameterPanel.SetActive(!parameterPanel.activeSelf);
        }
    }
}
