using HappyBirthday.Data;
using UnityEngine;
using UnityEngine.UI;

namespace HappyBirthday.UI
{
    public class MessagePanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject giftRoot;
        [SerializeField] private Text giftText;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        public void Show(FlowerData data)
        {
            if (data == null)
            {
                Debug.LogWarning("Cannot show message. FlowerData is null.");
                return;
            }

            Show(data.title, data.message, data.hasSpecialGift, data.giftDescription);
        }

        public void Show(string title, string message, bool hasSpecialGift, string giftDescription)
        {
            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;

            bool hasGift = hasSpecialGift && !string.IsNullOrWhiteSpace(giftDescription);
            if (giftRoot != null) giftRoot.SetActive(hasGift);
            if (giftText != null) giftText.text = hasGift ? giftDescription : string.Empty;

            root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}
