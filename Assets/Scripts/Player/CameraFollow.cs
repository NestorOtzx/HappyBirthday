using Nivelo.Player;
using UnityEngine;

namespace HappyBirthday.Player
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerMovement playerTarget;
        [SerializeField, Min(0f)] private float smoothTime = 0.16f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        [SerializeField] private bool snapToPixelGrid;
        [SerializeField, Min(1f)] private float pixelsPerUnit = 32f;

        [Header("Optional Bounds")]
        [SerializeField] private bool useBounds;
        [SerializeField] private Vector2 minBounds = new Vector2(-12f, -8f);
        [SerializeField] private Vector2 maxBounds = new Vector2(12f, 8f);

        private Vector3 velocity;

        private void Start()
        {
            if (playerTarget == null)
            {
                playerTarget = FindFirstObjectByType<PlayerMovement>();
            }

            if (target == null && playerTarget != null)
            {
                target = playerTarget.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null && playerTarget == null)
            {
                return;
            }

            Vector3 followPosition = playerTarget != null ? playerTarget.transform.position : target.position;
            Vector3 desired = followPosition + offset;
            if (useBounds)
            {
                desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
                desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);
            }

            Vector3 nextPosition = smoothTime <= 0f
                ? desired
                : Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);

            transform.position = snapToPixelGrid ? SnapPosition(nextPosition) : nextPosition;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            playerTarget = newTarget != null ? newTarget.GetComponent<PlayerMovement>() : null;
            velocity = Vector3.zero;
        }

        private Vector3 SnapPosition(Vector3 position)
        {
            float unitsPerPixel = 1f / pixelsPerUnit;
            position.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
            position.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
            return position;
        }
    }
}
