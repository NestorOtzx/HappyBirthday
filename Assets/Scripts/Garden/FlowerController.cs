using HappyBirthday.Data;
using HappyBirthday.Interactables;
using UnityEngine;

namespace HappyBirthday.Garden
{
    [RequireComponent(typeof(Collider2D))]
    public class FlowerController : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(1)] private int flowerId = 1;
        [SerializeField] private FlowerData flowerData;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject specialGiftMarker;

        private GardenManager gardenManager;

        public int FlowerId => flowerData != null ? flowerData.flowerId : flowerId;
        public FlowerData Data => flowerData;
        public FlowerState State { get; private set; } = FlowerState.Locked;
        public string PromptText => State == FlowerState.Opened ? "Presiona E para leer" : "Presiona E para abrir";
        public bool CanInteract => State != FlowerState.Locked;
        public Transform Transform => transform;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Initialize(GardenManager manager, FlowerData data)
        {
            gardenManager = manager;
            if (data != null)
            {
                flowerData = data;
                flowerId = data.flowerId;
            }

            RefreshVisual();
        }

        public void SetState(FlowerState state)
        {
            State = state;
            RefreshVisual();
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract)
            {
                return;
            }

            if (gardenManager == null)
            {
                gardenManager = FindFirstObjectByType<GardenManager>();
            }

            gardenManager?.OpenFlower(this);
        }

        private void RefreshVisual()
        {
            if (spriteRenderer != null && flowerData != null)
            {
                Sprite nextSprite = State switch
                {
                    FlowerState.Locked => flowerData.lockedSprite,
                    FlowerState.Available => flowerData.availableSprite,
                    FlowerState.Opened => flowerData.openedSprite,
                    _ => null
                };

                if (nextSprite != null)
                {
                    spriteRenderer.sprite = nextSprite;
                }
            }

            if (specialGiftMarker != null)
            {
                specialGiftMarker.SetActive(State == FlowerState.Opened && flowerData != null && flowerData.hasSpecialGift);
            }
        }
    }
}
