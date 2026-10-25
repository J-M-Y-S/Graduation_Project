using UnityEngine;
using PlayerControls;

namespace InstrumentAnimation
{
    /// <summary>药片实体；柜内药片可反复取用，独立展示药片支持点击。</summary>
    public class Tablet : MonoBehaviour, IPlayerInteractable
    {
        public TabletDefinition definition;
        public MedicineDrawer storageDrawer;

        public string GetPrompt()
        {
            if (definition == null || !gameObject.activeInHierarchy) return "";
            bool english = LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish;
            if (storageDrawer != null)
            {
                if (!storageDrawer.IsOpen || storageDrawer.IsMoving)
                    return english ? "Open the drawer first" : "请先抽出抽屉";
                if (TabletInventory.Instance != null && TabletInventory.Instance.IsHolding)
                    return english ? "Place the held tablet into a cup first" : "请先将手中药片投入溶杯";
            }
            return (english ? "E  Take · " : "E  拿取 · ") + definition.tabletName;
        }

        public void Interact()
        {
            TryPickup();
        }

        public bool TryPickup()
        {
            if (definition == null || !isActiveAndEnabled) return false;
            if (storageDrawer != null)
            {
                if (!storageDrawer.IsOpen || storageDrawer.IsMoving) return false;
                var player = FindObjectOfType<FirstPersonPlayer>();
                if (player != null && player.IsPanelOpen) return false;
                if (TabletInventory.Instance != null && TabletInventory.Instance.IsHolding) return false;
            }
            if (DissolutionExperiment.IsParameterPanelOpen()) return false;

            var exp = DissolutionExperiment.Instance;
            if (exp != null) exp.PickTablet(definition);
            else
            {
                TabletInventory.EnsureExists();
                TabletInventory.Instance.Pick(definition);
                Toast.Display("已添加药片：" + definition.tabletName);
            }
            return true;
        }

        void OnMouseDown()
        {
            if (storageDrawer == null) TryPickup();
        }
    }
}
