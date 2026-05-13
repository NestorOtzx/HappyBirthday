using HappyBirthday.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HappyBirthday.UI
{
    public class MessagePanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private GameObject messageRoot;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private ScrollRect messageScrollRect;
        [SerializeField] private Scrollbar messageScrollbar;
        [SerializeField] private RectTransform messageViewport;
        [SerializeField] private GameObject giftRoot;
        [SerializeField] private TextMeshProUGUI giftText;
        [SerializeField] private ScrollRect giftScrollRect;
        [SerializeField] private Scrollbar giftScrollbar;
        [SerializeField] private RectTransform giftViewport;
        [SerializeField] private Button closeButton;
        [SerializeField] private float scrollOverflowTolerance = 4f;

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

            CacheScrollReferences();
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

            bool hasMessage = !string.IsNullOrWhiteSpace(message);
            messageRoot.SetActive(hasMessage);

            root.SetActive(true);
            RefreshScrollAreas();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void CacheScrollReferences()
        {
            if (messageText != null && messageScrollRect == null)
            {
                messageScrollRect = messageText.GetComponentInParent<ScrollRect>(true);
            }

            if (giftText != null && giftScrollRect == null)
            {
                giftScrollRect = giftText.GetComponentInParent<ScrollRect>(true);
            }

            if (messageScrollRect != null)
            {
                messageViewport ??= messageScrollRect.viewport;
                messageScrollbar ??= messageScrollRect.verticalScrollbar;
            }

            if (giftScrollRect != null)
            {
                giftViewport ??= giftScrollRect.viewport;
                giftScrollbar ??= giftScrollRect.verticalScrollbar;
            }
        }

        private void RefreshScrollAreas()
        {
            CacheScrollReferences();
            Canvas.ForceUpdateCanvases();

            RefreshScrollArea(messageText, messageScrollRect, messageScrollbar, messageViewport);
            RefreshScrollArea(giftText, giftScrollRect, giftScrollbar, giftViewport);
        }

        private void RefreshScrollArea(
            TextMeshProUGUI text,
            ScrollRect scrollRect,
            Scrollbar scrollbar,
            RectTransform viewport)
        {
            if (text == null || scrollRect == null || viewport == null)
            {
                return;
            }

            text.ForceMeshUpdate();
            LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);

            float availableHeight = viewport.rect.height;
            float preferredHeight = text.GetPreferredValues(text.text, text.rectTransform.rect.width, 0f).y;
            bool hasOverflow = preferredHeight > availableHeight + scrollOverflowTolerance;

            if (scrollbar != null)
            {
                scrollbar.gameObject.SetActive(hasOverflow);
            }

            scrollRect.enabled = hasOverflow;
            scrollRect.vertical = hasOverflow;
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.velocity = Vector2.zero;
        }
    }
}
