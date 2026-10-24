using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    /// <summary>
    /// 让同一组转速输入框保持同步：主菜单的 L/R 卡与转速设置里的 AB/CD 卡共用一份配置。
    /// 未聚焦时按 ExperimentConfig 刷新文本，避免编辑时被覆盖。
    /// </summary>
    public class SpeedFieldSync : MonoBehaviour
    {
        public InputField input;
        public int pairIndex; // 0=AB(L), 1=CD(R)
        public bool isEnd;    // true=末端转速

        void Awake()
        {
            if (input == null) input = GetComponent<InputField>();
        }

        void OnEnable()
        {
            Sync();
        }

        void Update()
        {
            if (input == null || input.isFocused) return;
            Sync();
        }

        void Sync()
        {
            var cfg = ExperimentConfig.Instance;
            if (cfg == null || input == null) return;
            int v = isEnd ? cfg.GetEndSpeed(pairIndex) : cfg.GetInitialSpeed(pairIndex);
            string s = v.ToString();
            if (input.text != s) input.text = s;
        }
    }
}
