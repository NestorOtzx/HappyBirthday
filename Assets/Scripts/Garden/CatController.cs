using System.Collections;
using HappyBirthday.Interactables;
using Nivelo.Enemies;
using UnityEngine;

namespace HappyBirthday.Garden
{
    public class Cat : MonoBehaviour, IInteractable
    {
        public string PromptText => "Presiona [E] para acariciar";
        public bool CanInteract => true;
        public Transform Transform => transform;

        public AudioClip meowSound;

        AudioSource audioSource;
        

        EnemyWaypointPatrol patrol;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            patrol = GetComponent<EnemyWaypointPatrol>();           
        }
        public void Interact(GameObject interactor)
        {
            StartCoroutine(MeowRoutine());
        }

        IEnumerator MeowRoutine()
        {
            patrol.EnterLookAtPlayerState();
            if (audioSource != null && meowSound != null)
            {
                audioSource.PlayOneShot(meowSound);
            }
            yield return new WaitForSeconds(2f);
            patrol.ExitLookAtPlayerState();
        }
    }
}
