using HappyBirthday.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HappyBirthday.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public const string IntroSceneName = "IntroScene";
        public const string GardenSceneName = "GardenScene";

        public void LoadIntro()
        {
            SceneManager.LoadScene(IntroSceneName);
        }

        public void LoadGarden()
        {
            SceneManager.LoadScene(GardenSceneName);
        }

        public void LoadFirstSceneForPlayer()
        {
            bool introSeen = SaveManager.Instance != null && SaveManager.Instance.Data.introSeen;
            SceneManager.LoadScene(introSeen ? GardenSceneName : IntroSceneName);
        }

        public void Quit()
        {
            Application.Quit();
        }
    }
}
