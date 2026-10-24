using System.Collections;
using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>
    /// 水体模型的水位控制器。加水：从下到上逐渐出现；倒水：从上到下逐渐消失（底部固定）。
    /// 水体高度沿本地 Z 轴（映射到世界 Y），用渲染包围盒做世界坐标锚定，避免受父级旋转影响。
    /// </summary>
    public class WaterLevelController : MonoBehaviour
    {
        [SerializeField] private bool captured;
        [SerializeField] private Vector3 fullLocalScale;
        [SerializeField] private float bottomWorldY;

        public float fillFraction { get; private set; }

        /// <summary>在满水状态下记录基准数据（由构建器调用一次）。</summary>
        public void CaptureFull()
        {
            fullLocalScale = transform.localScale;
            bottomWorldY = GetComponent<Renderer>().bounds.min.y;
            captured = true;
        }

        void Awake()
        {
            if (!captured)
            {
                CaptureFull();
                SetLevel(0f);
            }
        }

        public void SetLevel(float f)
        {
            if (!captured) CaptureFull();
            f = Mathf.Clamp01(f);
            fillFraction = f;
            if (f <= 0.001f)
            {
                gameObject.SetActive(false);
                return;
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            ApplyScaleAndAnchor(f);
        }

        public Coroutine Fill(float duration = 1.5f)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);   // 先激活，才能启动协程
                fillFraction = 0f;
                ApplyScaleAndAnchor(0f);      // 置空并锚定底部（不隐藏）
            }
            return StartCoroutine(AnimateTo(1f, duration));
        }

        public Coroutine Pour(float duration = 1.5f)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            return StartCoroutine(AnimateTo(0f, duration));
        }

        void ApplyScaleAndAnchor(float f)
        {
            // 高度沿本地 Z 缩放
            var s = fullLocalScale;
            s.z = fullLocalScale.z * f;
            transform.localScale = s;

            // 底部固定：按当前包围盒把底部平移到满水时的底部
            var b = GetComponent<Renderer>().bounds;
            var p = transform.position;
            p.y += bottomWorldY - b.min.y;
            transform.position = p;
        }

        IEnumerator AnimateTo(float target, float duration)
        {
            float start = fillFraction;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetLevel(Mathf.Lerp(start, target, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetLevel(target);
        }
    }
}
