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
        public float paddleSpeed = 100f;
        private bool _paddlesRunning = false;

        private Coroutine _headMove;

        void Update()
        {
            if (_paddlesRunning && paddles != null)
            {
                foreach (var p in paddles)
                {
                    if (p != null)
                        p.Rotate(0f, paddleSpeed * Time.deltaTime, 0f, Space.Self);
                }
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
            HeadDown();
            StartPaddles();
        }

        public void StopDissolution()
        {
            StopPaddles();
        }

        // 以下为参数面板接入的临时桩方法，具体逻辑由后续状态机实现。
        public void StartHeating()
        {
            Debug.Log("开始加热（桩）");
        }

        public void StopHeating()
        {
            Debug.Log("停止加热（桩）");
        }

        public void Print()
        {
            Debug.Log("打印（桩）");
        }
    }
}
