using UnityEngine;
using System.Collections;

namespace InstrumentAnimation
{
    public class DissolutionTesterController : MonoBehaviour
    {
        [Header("Head Animation")]
        public Transform headGroup;
        public float headUpY = 0.5f;
        // 下降行程：机头组沿 Y 轴下移约 4.78，使搅拌棒（Cylinder021/024/027/030）
        // 的局部 Y 从 17.28 降到约 12.5，桨叶落到溶杯内靠近杯底的位置。
        public float headDownY = -4.78f;
        public float moveDuration = 1f;

        [Header("Paddles")]
        public Transform[] paddles;
        public float speedAB = 50f;   // rpm
        public float speedCD = 50f;   // rpm
        private bool _paddlesRunning = false;

        private Coroutine _headMove;

        void Update()
        {
            if (!_paddlesRunning || paddles == null) return;
            for (int i = 0; i < paddles.Length; i++)
            {
                var p = paddles[i];
                if (p == null) continue;
                float rpm = i < 2 ? speedAB : speedCD; // 前两个为 AB 杯，后两个为 CD 杯
                if (rpm <= 0.01f) continue;
                p.Rotate(0f, rpm * 6f * Time.deltaTime, 0f, Space.World); // rpm -> 度/秒
            }
        }

        public void HeadUp()
        {
            if (headGroup == null) return;
            if (_headMove != null) StopCoroutine(_headMove);
            _headMove = StartCoroutine(MoveHead(headUpY));
        }

        public void HeadDown()
        {
            if (headGroup == null) return;
            if (_headMove != null) StopCoroutine(_headMove);
            _headMove = StartCoroutine(MoveHead(headDownY));
        }

        IEnumerator MoveHead(float targetY)
        {
            Vector3 pos = headGroup.localPosition;
            float startY = pos.y;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / moveDuration;
                pos.y = Mathf.Lerp(startY, targetY, Mathf.Clamp01(t));
                headGroup.localPosition = pos;
                yield return null;
            }
            pos.y = targetY;
            headGroup.localPosition = pos;
        }

        public void StartPaddles()
        {
            _paddlesRunning = true;
        }

        public void StopPaddles()
        {
            _paddlesRunning = false;
        }

        public void StartDissolution()
        {
            if (DissolutionExperiment.Instance != null) DissolutionExperiment.Instance.StartRun();
            else { HeadDown(); StartPaddles(); }
        }

        public void StopDissolution()
        {
            if (DissolutionExperiment.Instance != null) DissolutionExperiment.Instance.StopRun();
            else StopPaddles();
        }

        /// <summary>机头是否已下降到位。</summary>
        public bool IsHeadDown()
        {
            if (headGroup == null) return true;
            return Mathf.Abs(headGroup.localPosition.y - headDownY) < 0.2f;
        }

        // 以下为参数面板接入的临时桩方法，具体逻辑由后续状态机实现。
        public void StartHeating()
        {
            if (DissolutionExperiment.Instance != null) DissolutionExperiment.Instance.StartHeating();
        }

        public void StopHeating()
        {
            if (DissolutionExperiment.Instance != null) DissolutionExperiment.Instance.StopHeating();
        }

        public void Print()
        {
            if (DissolutionExperiment.Instance != null) DissolutionExperiment.Instance.PrintReport();
        }
    }
}
