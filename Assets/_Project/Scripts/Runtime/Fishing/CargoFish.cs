using System.Collections;
using UnityEngine;

namespace SecondCatch.Fishing
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CargoFish : MonoBehaviour
    {
        private bool _stowed;

        internal bool TryStow(Transform hold, Vector3 position, Quaternion rotation, bool animate)
        {
            if (_stowed) return false;
            _stowed = true;

            var body = GetComponent<Rigidbody>();
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.isKinematic = true;
            }
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            body.detectCollisions = false;
            transform.SetParent(hold, false);
            transform.SetLocalPositionAndRotation(position, rotation);

            if (animate) StartCoroutine(Drop(position));
            return true;
        }

        private IEnumerator Drop(Vector3 destination)
        {
            const float duration = 0.4f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float progress = elapsed / duration;
                transform.localPosition = destination + Vector3.up * (0.8f * (1f - progress * progress));
                yield return null;
            }
            transform.localPosition = destination;
        }
    }
}
