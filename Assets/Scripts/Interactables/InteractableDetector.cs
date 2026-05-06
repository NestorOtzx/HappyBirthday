using HappyBirthday.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HappyBirthday.Interactables
{
    public class InteractableDetector : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float detectionRadius = 1.1f;
        [SerializeField] private LayerMask interactableLayers = ~0;

        private readonly Collider2D[] hits = new Collider2D[16];
        private IInteractable current;

        private void Update()
        {
            current = FindNearest();
            UpdatePrompt();

            if (current != null && current.CanInteract && WasInteractPressed())
            {
                current.Interact(gameObject);
            }
        }

        private IInteractable FindNearest()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(interactableLayers);
            filter.useTriggers = true;

            int count = Physics2D.OverlapCircle(transform.position, detectionRadius, filter, hits);
            IInteractable nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                IInteractable interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract)
                {
                    continue;
                }

                float distance = Vector2.SqrMagnitude(interactable.Transform.position - transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = interactable;
                }
            }

            return nearest;
        }

        private void UpdatePrompt()
        {
            UIManager uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                return;
            }

            if (current != null && current.CanInteract)
            {
                uiManager.ShowInteractionPrompt(current.PromptText);
            }
            else
            {
                uiManager.HideInteractionPrompt();
            }
        }

        private static bool WasInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }

        private void OnDisable()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideInteractionPrompt();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
