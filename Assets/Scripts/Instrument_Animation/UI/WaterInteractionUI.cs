using UnityEngine;
using UnityEngine.UI;

namespace InstrumentAnimation
{
    /// <summary>点击溶杯后弹出的加水/倒水按钮面板。</summary>
    public class WaterInteractionUI : MonoBehaviour
    {
        public static WaterInteractionUI Instance { get; private set; }

        public GameObject panel;
        public Text titleText;
        public Text actionText;
        public Button actionButton;
        public Button tabletButton;
        public Button cancelButton;

        private int currentCupIndex;
        private bool currentHasWater;

        void Awake()
        {
            Instance = this;
        }

        public bool IsOpen => panel != null && panel.activeInHierarchy;

        void Start()
        {
            if (actionButton != null) actionButton.onClick.AddListener(OnAction);
            if (tabletButton != null) tabletButton.onClick.AddListener(OnTablet);
            if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
            Hide();
        }

        public void Show(int cupIndex, bool hasWater)
        {
            currentCupIndex = cupIndex;
            currentHasWater = hasWater;
            bool en = LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish;
            if (titleText != null) titleText.text = (en ? "Cup " : "溶杯 ") + (char)('A' + cupIndex);
            if (actionText != null) actionText.text = en ? (hasWater ? "Pour Water" : "Add Water") : (hasWater ? "倒水" : "加水");
            if (tabletButton != null)
            {
                bool holding = TabletInventory.Instance != null && TabletInventory.Instance.IsHolding;
                bool running = DissolutionExperiment.Instance != null && DissolutionExperiment.Instance.IsRunning;
                tabletButton.gameObject.SetActive(holding && !running);
            }
            if (panel != null) panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        void OnAction()
        {
            var exp = DissolutionExperiment.Instance;
            if (exp == null) { Hide(); return; }
            if (currentHasWater) exp.PourWater(currentCupIndex);
            else exp.AddWater(currentCupIndex);
            Hide();
        }

        void OnTablet()
        {
            var exp = DissolutionExperiment.Instance;
            if (exp != null) exp.TryPlaceTablet(currentCupIndex);
            Hide();
        }
    }
}
