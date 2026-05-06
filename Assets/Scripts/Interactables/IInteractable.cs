using UnityEngine;

namespace HappyBirthday.Interactables
{
    public interface IInteractable
    {
        string PromptText { get; }
        bool CanInteract { get; }
        Transform Transform { get; }
        void Interact(GameObject interactor);
    }
}
