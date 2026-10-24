using UnityEngine;
using System.Collections.Generic;

namespace InstrumentAnimation
{
    public class ParameterPanel : MonoBehaviour
    {
        public GameObject mainMenu;
        public List<GameObject> subPanels = new List<GameObject>();
        private Stack<GameObject> _stack = new Stack<GameObject>();

        void OnEnable()
        {
            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            foreach (var p in subPanels)
                if (p != null) p.SetActive(false);
            if (mainMenu != null) mainMenu.SetActive(true);
            _stack.Clear();
        }

        public void OpenPanel(GameObject panel)
        {
            if (panel == null) return;
            if (_stack.Count == 0)
                _stack.Push(mainMenu);
            else if (_stack.Peek() != panel)
                _stack.Peek().SetActive(false);
            panel.SetActive(true);
            _stack.Push(panel);
        }

        public void Back()
        {
            if (_stack.Count <= 1)
            {
                ShowMainMenu();
                return;
            }
            GameObject current = _stack.Pop();
            if (current != null) current.SetActive(false);
            if (_stack.Count > 0 && _stack.Peek() != null)
                _stack.Peek().SetActive(true);
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }
    }
}
