using UnityEngine;
using UnityEngine.UI;

namespace PlayerControls
{
    public interface IPlayerInteractable
    {
        string GetPrompt();
        void Interact();
    }

    /// <summary>通过第一人称准星寻找近处目标，按 E 交互。</summary>
    [RequireComponent(typeof(FirstPersonPlayer))]
    public class PlayerInteractor : MonoBehaviour
    {
        [Min(0.1f)] public float interactionDistance = 1.8f;
        public LayerMask interactionMask = Physics.DefaultRaycastLayers;
        public Text promptText;
        public Image reticleImage;

        FirstPersonPlayer player;

        void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
        }

        void LateUpdate()
        {
            var target = player.CanControl ? GetTarget() : null;
            if (promptText != null)
            {
                string prompt = target != null ? target.GetPrompt() : "";
                promptText.text = prompt;
                promptText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(prompt));
            }
            if (reticleImage != null)
                reticleImage.color = target != null ? new Color(1f, 0.8f, 0.35f) : Color.white;
            if (target != null && Input.GetKeyDown(KeyCode.E)) target.Interact();
        }

        public IPlayerInteractable GetTarget()
        {
            if (player == null || player.playerCamera == null) return null;
            var cameraTransform = player.playerCamera.transform;
            if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward,
                out var hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore)) return null;
            foreach (var component in hit.collider.GetComponentsInParent<MonoBehaviour>())
                if (component is IPlayerInteractable target) return target;
            return null;
        }

        void OnDisable()
        {
            if (promptText != null) promptText.transform.parent.gameObject.SetActive(false);
            if (reticleImage != null) reticleImage.color = Color.white;
        }
    }
}
