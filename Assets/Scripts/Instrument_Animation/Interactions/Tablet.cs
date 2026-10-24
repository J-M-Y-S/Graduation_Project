using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>药片实体，点击后拾取到手持背包。</summary>
    public class Tablet : MonoBehaviour
    {
        public TabletDefinition definition;

        void OnMouseDown()
        {
            if (definition == null) return;
            var exp = DissolutionExperiment.Instance;
            if (exp != null) exp.PickTablet(definition);
            else
            {
                TabletInventory.EnsureExists();
                TabletInventory.Instance.Pick(definition);
                Toast.Display("已添加药片：" + definition.tabletName);
            }
        }
    }
}
