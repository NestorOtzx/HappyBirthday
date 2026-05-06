using HappyBirthday.UI;
using UnityEngine;

namespace HappyBirthday.Interactables
{
    [RequireComponent(typeof(Collider2D))]
    public class MessageInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string promptText = "Presiona E";
        [SerializeField] private string title = "Garden note";

        [TextArea(3, 8)]
        [SerializeField] private string message = "A small note for the player.";

        public string PromptText => promptText;
        public bool CanInteract => true;
        public Transform Transform => transform;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Interact(GameObject interactor)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPlainMessage(title, message);
            }
            else
            {
                Debug.Log($"{title}: {message}");
            }
        }
    }
}
