using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    /// <summary>打印纸条 UI：显示实验参数与结果，带淡入和关闭。</summary>
    public class PrintPaperUI : MonoBehaviour
    {
        public GameObject root;
        public CanvasGroup group;
        public Text titleText;
        public Text bodyText;
        public Button closeButton;

        void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            Hide();
        }

        public void Show(string title, string body)
        {
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
            if (root != null) root.SetActive(true);
            if (group != null)
            {
                group.alpha = 0f;
                StartCoroutine(FadeIn());
            }
        }

        IEnumerator FadeIn()
        {
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                if (group != null) group.alpha = Mathf.Clamp01(t / 0.3f);
                yield return null;
            }
            if (group != null) group.alpha = 1f;
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
