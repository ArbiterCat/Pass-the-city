using UnityEngine;

namespace SvoiPut
{
    /// <summary>Камера от третьего лица с орбитальным обзором, коллизией и кинематографическим режимом меню.</summary>
    public class PlayerCamera : MonoBehaviour
    {
        public Transform target;
        public Camera cam;

        public float distance = 5.6f;
        public float minDistance = 3f;
        public float maxDistance = 9f;
        public float sensitivity = 2.2f;
        public float pivotHeight = 1.5f;
        public float minPitch = -28f;
        public float maxPitch = 66f;
        public float baseFov = 60f;
        public float slowFov = 70f;

        private float yaw;
        private float pitch = 17f;
        private float cinematicAngle;
        private bool cinematic;
        private bool inputEnabled = true;
        private bool slow;
        private Vector3 smoothedPivot;

        private void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target != null)
            {
                smoothedPivot = target.position + Vector3.up * pivotHeight;
                SnapBehind();
            }
        }

        public void SetCinematic(bool value)
        {
            cinematic = value;
            if (value)
            {
                cinematicAngle = target != null ? target.eulerAngles.y + 24f : 24f;
                if (cam != null) cam.fieldOfView = baseFov;
            }
        }

        public void SetInputEnabled(bool value)
        {
            inputEnabled = value;
        }

        public void SetSlow(bool value)
        {
            slow = value;
        }

        public void SnapBehind()
        {
            if (target == null)
            {
                return;
            }
            yaw = target.eulerAngles.y;
            pitch = 17f;
            smoothedPivot = target.position + Vector3.up * pivotHeight;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = GetDesiredPosition(rotation);
            transform.position = desired;
            transform.rotation = Quaternion.LookRotation(smoothedPivot - transform.position, Vector3.up);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            if (cinematic)
            {
                cinematicAngle += 2.2f * dt;
                Vector3 center = target.position + Vector3.up * 1.3f;
                Quaternion orbit = Quaternion.Euler(7f, cinematicAngle, 0f);
                Vector3 desired = center + orbit * new Vector3(0f, 2.6f, -10.5f);
                float k = 1f - Mathf.Exp(-2.4f * dt);
                transform.position = Vector3.Lerp(transform.position, desired, k);
                transform.rotation = Quaternion.LookRotation(center + Vector3.up * 0.4f - transform.position, Vector3.up);
                return;
            }

            if (inputEnabled)
            {
                yaw += Input.GetAxis("Mouse X") * sensitivity;
                pitch -= Input.GetAxis("Mouse Y") * sensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    distance = Mathf.Clamp(distance - scroll * 0.55f, minDistance, maxDistance);
                }
            }

            Vector3 pivotTarget = target.position + Vector3.up * pivotHeight;
            float pk = 1f - Mathf.Exp(-16f * dt);
            smoothedPivot = Vector3.Lerp(smoothedPivot, pivotTarget, pk);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPos = GetDesiredPosition(rotation);

            float ck = 1f - Mathf.Exp(-24f * dt);
            transform.position = Vector3.Lerp(transform.position, desiredPos, ck);
            transform.rotation = Quaternion.LookRotation(smoothedPivot + Vector3.up * 0.15f - transform.position, Vector3.up);

            if (cam != null)
            {
                float targetFov = slow ? slowFov : baseFov;
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-6f * dt));
            }
        }

        private Vector3 GetDesiredPosition(Quaternion rotation)
        {
            Vector3 direction = rotation * Vector3.back;
            float allowed = distance;

            var hits = Physics.SphereCastAll(smoothedPivot, 0.26f, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger) continue;
                if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
                if (hit.distance < 0.1f) continue;
                allowed = Mathf.Min(allowed, hit.distance - 0.18f);
            }

            allowed = Mathf.Clamp(allowed, 1.1f, distance);
            return smoothedPivot + direction * allowed;
        }
    }
}
