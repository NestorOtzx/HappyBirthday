using HappyBirthday.Interactables;
using UnityEngine;

namespace HappyBirthday.Garden
{
    [RequireComponent(typeof(Collider2D))]
    public class CatController : MonoBehaviour, IInteractable
    {
        private enum CatState
        {
            Idle,
            Walk,
            Sleep
        }

        [SerializeField, Min(0.1f)] private float moveSpeed = 1.15f;
        [SerializeField] private Vector2 wanderCenter;
        [SerializeField] private Vector2 wanderSize = new Vector2(5f, 3f);
        [SerializeField] private Vector2 idleTimeRange = new Vector2(1.5f, 4f);
        [SerializeField] private Vector2 sleepTimeRange = new Vector2(5f, 9f);
        [SerializeField, Range(0f, 1f)] private float sleepChance = 0.2f;
        [SerializeField] private Animator animator;

        private CatState state;
        private Vector2 target;
        private float timer;

        public string PromptText => "Presiona E para acariciar";
        public bool CanInteract => true;
        public Transform Transform => transform;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Start()
        {
            if (wanderCenter == Vector2.zero)
            {
                wanderCenter = transform.position;
            }

            EnterIdle();
        }

        private void Update()
        {
            timer -= Time.deltaTime;

            if (state == CatState.Walk)
            {
                transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                if (Vector2.Distance(transform.position, target) <= 0.05f)
                {
                    EnterIdle();
                }
            }
            else if (timer <= 0f)
            {
                if (state == CatState.Sleep || Random.value > sleepChance)
                {
                    EnterWalk();
                }
                else
                {
                    EnterSleep();
                }
            }

            UpdateAnimator();
        }

        public void Interact(GameObject interactor)
        {
            EnterIdle();
        }

        private void EnterIdle()
        {
            state = CatState.Idle;
            timer = Random.Range(idleTimeRange.x, idleTimeRange.y);
        }

        private void EnterWalk()
        {
            state = CatState.Walk;
            Vector2 half = wanderSize * 0.5f;
            target = new Vector2(
                Random.Range(wanderCenter.x - half.x, wanderCenter.x + half.x),
                Random.Range(wanderCenter.y - half.y, wanderCenter.y + half.y));
        }

        private void EnterSleep()
        {
            state = CatState.Sleep;
            timer = Random.Range(sleepTimeRange.x, sleepTimeRange.y);
        }

        private void UpdateAnimator()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool("IsWalking", state == CatState.Walk);
            animator.SetBool("IsSleeping", state == CatState.Sleep);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector2 center = wanderCenter == Vector2.zero ? transform.position : wanderCenter;
            Gizmos.DrawWireCube(center, wanderSize);
        }
    }
}
