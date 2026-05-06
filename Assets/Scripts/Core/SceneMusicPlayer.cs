using UnityEngine;

namespace HappyBirthday.Core
{
    public class SceneMusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip musicClip;

        private void Start()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(musicClip);
            }
        }
    }
}
