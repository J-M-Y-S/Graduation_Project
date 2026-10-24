using System;
using System.Collections.Generic;
using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>药片定义：名称、质量、溶解速率系数基值。</summary>
    [Serializable]
    public class TabletDefinition
    {
        public string tabletName = "药片";
        public float massMg = 100f;   // 质量（毫克）
        public float baseK = 0.08f;   // 溶解速率系数基值 k（1/min）
        public Color color = Color.white;

        public TabletDefinition() { }

        public TabletDefinition(string name, float massMg, float baseK)
            : this(name, massMg, baseK, Color.white) { }

        public TabletDefinition(string name, float massMg, float baseK, Color color)
        {
            tabletName = name;
            this.massMg = massMg;
            this.baseK = baseK;
            this.color = color;
        }
    }

    /// <summary>常用药片目录。后续可在编辑器里改成 ScriptableObject 资产，便于直接调参。</summary>
    public static class TabletCatalog
    {
        public static readonly List<TabletDefinition> Defaults = new List<TabletDefinition>
        {
            new TabletDefinition("复方丹参片", 250f, 0.07f, new Color(0.9f, 0.25f, 0.25f)),
            new TabletDefinition("板蓝根片", 300f, 0.10f, new Color(0.3f, 0.5f, 0.95f)),
            new TabletDefinition("穿心莲片", 200f, 0.13f, new Color(0.35f, 0.8f, 0.4f)),
            new TabletDefinition("牛黄解毒片", 500f, 0.06f, new Color(0.95f, 0.8f, 0.25f)),
            new TabletDefinition("银翘解毒片", 350f, 0.09f, new Color(0.7f, 0.4f, 0.9f))
        };
    }
}
