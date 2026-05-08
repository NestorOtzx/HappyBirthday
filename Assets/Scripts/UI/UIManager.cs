using HappyBirthday.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HappyBirthday.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private MessagePanelUI messagePanel;
        [SerializeField] private GameObject interactionPromptRoot;
        [SerializeField] private TextMeshProUGUI interactionPromptText;
        [SerializeField] private TextMeshProUGUI shadowPromptText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (messagePanel == null)
            {
                messagePanel = GetComponentInChildren<MessagePanelUI>(true);
            }

            HideInteractionPrompt();
        }

        public void ShowFlowerMessage(FlowerData data)
        {
            if (messagePanel == null)
            {
                Debug.LogWarning("MessagePanelUI is missing.");
                return;
            }

            messagePanel.Show(data);
        }

        public void ShowPlainMessage(string title, string message)
        {
            if (messagePanel == null)
            {
                Debug.LogWarning("MessagePanelUI is missing.");
                return;
            }

            messagePanel.Show(title, message, false, string.Empty);
        }

        public void ShowInteractionPrompt(string prompt)
        {
            if (interactionPromptRoot != null)
            {
                interactionPromptRoot.SetActive(true);
            }

            if (interactionPromptText != null)
            {
                interactionPromptText.text = prompt;
            }

            if (shadowPromptText != null)
            {
                shadowPromptText.text = prompt;
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptRoot != null)
            {
                interactionPromptRoot.SetActive(false);
            }
        }
    }
}
