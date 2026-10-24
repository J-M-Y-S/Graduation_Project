using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    /// <summary>语言按钮：点击切换到对应语言。</summary>
    public class LanguageOption : MonoBehaviour
    {
        public UILanguage language;

        void Start()
        {
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(Apply);
        }

        public void Apply()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.SetLanguage(language);
        }
    }
}
