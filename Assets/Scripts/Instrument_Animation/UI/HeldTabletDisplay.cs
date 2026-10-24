using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    /// <summary>左下角显示当前手持药片。</summary>
    public class HeldTabletDisplay : MonoBehaviour
    {
        public Text text;

        void Start()
        {
            Refresh();
            if (TabletInventory.Instance != null)
                TabletInventory.Instance.OnTabletChanged += OnChanged;
        }

        void OnDestroy()
        {
            if (TabletInventory.Instance != null)
                TabletInventory.Instance.OnTabletChanged -= OnChanged;
        }

        void OnChanged(TabletDefinition tablet)
        {
            Refresh();
        }

        void Refresh()
        {
            if (text == null) return;
            var inv = TabletInventory.Instance;
            bool en = LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish;
            if (inv != null && inv.IsHolding)
                text.text = (en ? "Tablet: " : "当前药片：") + inv.currentTablet.tabletName;
            else
                text.text = en ? "Tablet: None" : "当前药片：无";
        }
    }
}
