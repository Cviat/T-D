using UnityEngine;

namespace RPGTable.Runtime
{
    /// <summary>
    /// Smoothly moves a Player View token clone to a new grid position.
    /// Destroys itself when the token reaches its target.
    /// </summary>
    public sealed class PlayerViewTokenMover : MonoBehaviour
    {
        private const float MoveSpeed = 8f;
        private const float StopDistance = 0.01f;

        private Vector3 targetPosition;
        private bool moving;

        public void Initialize(Vector3 target)
        {
            targetPosition = target;
            moving = true;
        }

        private void Update()
        {
            if (!moving)
            {
                return;
            }

            transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-MoveSpeed * Time.deltaTime));

            if ((transform.position - targetPosition).sqrMagnitude > StopDistance * StopDistance)
            {
                return;
            }

            transform.position = targetPosition;
            moving = false;
            Destroy(this);
        }
    }
}
