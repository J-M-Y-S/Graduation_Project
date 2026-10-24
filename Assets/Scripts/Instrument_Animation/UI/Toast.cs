using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    /// <summary>屏幕中央下方的短暂提示。</summary>
    public class Toast : MonoBehaviour
    {
        public static Toast Instance { get; private set; }
        public Text text;
        public float visibleDuration = 2f;
        public float fadeDuration = 0.18f;

        GameObject container;
        CanvasGroup group;
        Coroutine displayRoutine;

        void Awake()
        {
            Instance = this;
            if (text == null) return;

            container = text.transform.parent != null
                ? text.transform.parent.gameObject
                : text.gameObject;
            group = container.GetComponent<CanvasGroup>();
            if (group == null) group = container.AddComponent<CanvasGroup>();

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            container.SetActive(false);
        }

        public void ShowMessage(string message)
        {
            if (text == null) return;
            text.text = message;

            if (displayRoutine != null) StopCoroutine(displayRoutine);
            displayRoutine = StartCoroutine(DisplayRoutine());
        }

        IEnumerator DisplayRoutine()
        {
            text.gameObject.SetActive(true);
            container.SetActive(true);

            if (!Mathf.Approximately(group.alpha, 1f))
                yield return FadeTo(1f);

            if (visibleDuration > 0f)
                yield return new WaitForSecondsRealtime(visibleDuration);

            yield return FadeTo(0f);
            container.SetActive(false);
            displayRoutine = null;
        }

        IEnumerator FadeTo(float target)
        {
            if (fadeDuration <= 0f)
            {
                group.alpha = target;
                yield break;
            }

            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            group.alpha = target;
        }

        public static void Display(string message)
        {
            if (Instance != null) Instance.ShowMessage(message);
        }
    }
}
