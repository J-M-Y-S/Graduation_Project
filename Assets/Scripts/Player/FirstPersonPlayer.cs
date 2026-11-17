using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerControls
{
    /// <summary>第一人称行走与视角控制；打开操作面板时自动释放鼠标。</summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonPlayer : MonoBehaviour
    {
        public Camera playerCamera;
        public GameObject reticle;
        public GameObject[] modalPanels;
        [Min(0.1f)] public float moveSpeed = 2.2f;
        [Min(0.1f)] public float mouseSensitivity = 1.8f;
        [Range(30f, 89f)] public float maxPitch = 80f;
        public float gravity = -20f;

        CharacterController controller;
        float pitch;
        float verticalSpeed;
        bool cursorReleased;

        public bool IsPanelOpen
        {
            get
            {
                if (modalPanels == null) return false;
                foreach (var panel in modalPanels)
                    if (panel != null && panel.activeInHierarchy) return true;
                return false;
            }
        }

        public bool CanControl => !cursorReleased && !IsPanelOpen
            && Cursor.lockState == CursorLockMode.Locked;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
                pitch = Mathf.DeltaAngle(0f, playerCamera.transform.localEulerAngles.x);
        }

        void OnEnable()
        {
            cursorReleased = false;
            RefreshCursor();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) cursorReleased = true;
            if (cursorReleased && !IsPanelOpen && Input.GetMouseButtonDown(0)
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                cursorReleased = false;

            RefreshCursor();
            if (CanControl)
            {
                ApplyLook(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")));
                var input = Vector2.ClampMagnitude(new Vector2(
                    Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
                Move(input);
            }
            else Move(Vector2.zero);
        }

        void ApplyLook(Vector2 delta)
        {
            transform.Rotate(0f, delta.x * mouseSensitivity, 0f);
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -maxPitch, maxPitch);
            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void Move(Vector2 input)
        {
            if (!controller.enabled) return;
            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed += gravity * Time.deltaTime;
            var horizontal = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
            controller.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        void RefreshCursor()
        {
            bool locked = !cursorReleased && !IsPanelOpen && Application.isFocused;
            var mode = locked ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != mode) Cursor.lockState = mode;
            Cursor.visible = !locked;
            if (reticle != null && reticle.activeSelf != locked) reticle.SetActive(locked);
        }

        void OnApplicationFocus(bool focused)
        {
            RefreshCursor();
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (reticle != null) reticle.SetActive(false);
        }
    }
}
