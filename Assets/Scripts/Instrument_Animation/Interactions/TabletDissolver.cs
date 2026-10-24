using System.Collections;
using UnityEngine;

namespace InstrumentAnimation
{
    /// <summary>驱动杯内药片的溶解进度（0~1），由溶出模型计算后传入。</summary>
    public class TabletDissolver : MonoBehaviour
    {
        public DissolveController dissolve;
        public int cupIndex;
        public float dissolveSeconds = 10f;
        [Range(0f, 1f)] public float progress;

        private Coroutine _routine;

        void Awake()
        {
            if (dissolve == null) dissolve = GetComponent<DissolveController>();
        }

        /// <summary>在“开始运行”时调用，开始固定时长的消融。</summary>
        public void StartDissolve()
        {
            if (progress >= 1f) return; // 已完全消融，不再重播
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(DissolveOverTime());
        }

        IEnumerator DissolveOverTime()
        {
            float t = Mathf.Clamp01(progress) * dissolveSeconds; // 从当前进度继续
            while (t < dissolveSeconds)
            {
                t += Time.deltaTime;
                SetProgress(t / dissolveSeconds);
                yield return null;
            }
            SetProgress(1f);
            _routine = null;
        }

        public void SetProgress(float p)
        {
            progress = Mathf.Clamp01(p);
            if (dissolve != null) dissolve.SetProgress(progress);
        }

        public void ResetTablet()
        {
            progress = 0f;
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            if (dissolve != null) dissolve.ResetDissolve();
        }
    }
}
