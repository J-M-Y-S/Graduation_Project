using System;
using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>手持药片背包：每次只能持一种药片，拾取新药片会替换当前手持。</summary>
    public class TabletInventory : MonoBehaviour
    {
        public static TabletInventory Instance { get; private set; }

        public TabletDefinition currentTablet;
        public event Action<TabletDefinition> OnTabletChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // [Serializable] 字段在 Unity 中会被序列化成默认实例，这里显式清空，保证开局未持有药片
            currentTablet = null;
        }

        public bool IsHolding => currentTablet != null;

        /// <summary>拾取药片（替换当前手持）。</summary>
        public void Pick(TabletDefinition tablet)
        {
            currentTablet = tablet;
            OnTabletChanged?.Invoke(currentTablet);
        }

        /// <summary>取出当前手持药片（投入杯子后清空）。</summary>
        public TabletDefinition Take()
        {
            var t = currentTablet;
            currentTablet = null;
            OnTabletChanged?.Invoke(null);
            return t;
        }

        public static void EnsureExists()
        {
            if (Instance != null) return;
            var go = GameObject.Find("TabletInventory");
            if (go == null) go = new GameObject("TabletInventory");
            var inv = go.GetComponent<TabletInventory>();
            if (inv == null) inv = go.AddComponent<TabletInventory>();
            Instance = inv;
        }
    }
}
