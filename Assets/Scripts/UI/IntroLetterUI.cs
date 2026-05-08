using HappyBirthday.Core;
using HappyBirthday.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HappyBirthday.UI
{
    public class IntroLetterUI : MonoBehaviour
    {
        [SerializeField] private bool skipIfAlreadySeen;
        [SerializeField] private string title = "Una carta para ti";

        [TextArea(5, 14)]
        [SerializeField] private string body = "Bienvenida a tu pequeño jardín.";

        [SerializeField] private Sprite illustration;
        [SerializeField] private AudioClip continueSfx;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Image illustrationImage;
        [SerializeField] private Button continueButton;

        private void Start()
        {
            ApplyContent();

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(ContinueToGarden);
            }

            if (skipIfAlreadySeen && SaveManager.Instance != null && SaveManager.Instance.Data.introSeen)
            {
                ContinueToGarden();
            }
        }

        public void ContinueToGarden()
        {
            AudioManager.Instance?.PlaySfx(continueSfx);
            SaveManager.Instance?.SetIntroSeen(true);

            SceneLoader loader = FindFirstObjectByType<SceneLoader>();
            if (loader != null)
            {
                loader.LoadGarden();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.GardenSceneName);
            }
        }

        public void ApplyContent()
        {
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;

            if (illustrationImage != null)
            {
                illustrationImage.sprite = illustration;
                illustrationImage.gameObject.SetActive(illustration != null);
            }
        }
    }
}
