using System.Collections;
using UnityEngine;
using PlayerControls;

namespace InstrumentAnimation
{
    /// <summary>存药柜抽屉：沿本地负 Z 方向抽出，再回到原位。</summary>
    public class MedicineDrawer : MonoBehaviour, IPlayerInteractable
    {
        public string medicineName;
        public Tablet storedTablet;
        public BoxCollider frontCollider;
        [Min(0.01f)] public float pullDistance = 0.27f;
        [Min(0.01f)] public float moveDuration = 0.35f;

        public bool IsOpen { get; private set; }
        public bool IsMoving { get; private set; }

        Vector3 insertedPosition;
        bool targetOpen;
        float blockedUntil;
        Coroutine motion;

        void Awake()
        {
            insertedPosition = transform.localPosition;
        }

        public string GetPrompt()
        {
            bool english = LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish;
            if (Time.unscaledTime < blockedUntil)
                return english ? "Step back, then press E to open" : "请后退一步，再按 E 抽出";
            if (IsMoving)
                return english ? (targetOpen ? "Opening..." : "Closing...")
                    : (targetOpen ? "正在抽出…" : "正在插入…");
            string action = english ? (IsOpen ? "E  Close" : "E  Open") : (IsOpen ? "E  插入" : "E  抽出");
            return action + " · " + medicineName;
        }

        public void Interact()
        {
            TryToggle();
        }

        public bool TryToggle()
        {
            if (IsMoving || !isActiveAndEnabled) return false;
            if (!IsOpen && PlayerBlocksOpening())
            {
                blockedUntil = Time.unscaledTime + 1.3f;
                return false;
            }
            blockedUntil = 0f;
            targetOpen = !IsOpen;
            motion = StartCoroutine(MoveDrawer());
            return true;
        }

        bool PlayerBlocksOpening()
        {
            if (frontCollider == null) return false;
            var sweep = frontCollider.bounds;
            var end = sweep;
            var localTravel = Vector3.back * pullDistance;
            end.center += transform.parent != null ? transform.parent.TransformVector(localTravel) : localTravel;
            sweep.Encapsulate(end);
            foreach (var obstacle in Physics.OverlapBox(sweep.center, sweep.extents,
                Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (obstacle is CharacterController) return true;
            return false;
        }

        IEnumerator MoveDrawer()
        {
            IsMoving = true;
            var start = transform.localPosition;
            var end = insertedPosition + (targetOpen ? Vector3.back * pullDistance : Vector3.zero);
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, moveDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(start, end,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            transform.localPosition = end;
            IsOpen = targetOpen;
            IsMoving = false;
            motion = null;
        }

        void OnDisable()
        {
            if (!IsMoving) return;
            if (motion != null) StopCoroutine(motion);
            transform.localPosition = insertedPosition + (targetOpen ? Vector3.back * pullDistance : Vector3.zero);
            IsOpen = targetOpen;
            IsMoving = false;
            motion = null;
        }
    }
}
