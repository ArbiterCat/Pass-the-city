using UnityEngine;

namespace SvoiPut
{
    /// <summary>Управление персонажем: движение относительно камеры, прыжок с coyote-time и буфером.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Ссылки")]
        public Transform cameraTransform;
        public Transform visualRoot;

        [Header("Движение")]
        public float moveSpeed = 6.4f;
        public float groundAcceleration = 52f;
        public float airAcceleration = 26f;
        public float gravity = 24f;
        public float jumpHeight = 2.15f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        public float rotationSpeed = 15f;

        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool grounded;
        private bool controlEnabled;
        private MovingPlatform groundPlatform;
        private bool wasGrounded;
        private Vector3 conveyorVelocity;
        private float conveyorUntil;
        private float travelledDistance;

        public bool Grounded => grounded;
        public float PlanarSpeed => new Vector3(planarVelocity.x, 0f, planarVelocity.z).magnitude;
        public float VerticalSpeed => verticalVelocity;
        public bool ControlEnabled => controlEnabled;
        public float TravelledDistance => travelledDistance;
        public int JumpCount { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void SetControlEnabled(bool value)
        {
            controlEnabled = value;
            if (!value)
            {
                planarVelocity = Vector3.zero;
                verticalVelocity = 0f;
                conveyorVelocity = Vector3.zero;
                conveyorUntil = 0f;
            }
        }

        /// <summary>Внешняя скорость от конвейера. Вызывается каждый кадр, пока игрок стоит на ленте.</summary>
        public void SetConveyorPush(Vector3 velocity)
        {
            conveyorVelocity = velocity;
            conveyorUntil = Time.time + 0.12f;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
            conveyorVelocity = Vector3.zero;
            conveyorUntil = 0f;
            groundPlatform = null;
            grounded = false;
            if (visualRoot != null) visualRoot.localRotation = Quaternion.identity;
        }

        public void Bounce(float power)
        {
            verticalVelocity = power;
            grounded = false;
            coyoteTimer = 0f;
        }

        private void Update()
        {
            if (GameManager.I == null || GameManager.I.State != GameState.Playing || !controlEnabled)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            // --- ввод ---
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 wish = forward * inputZ + right * inputX;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            Vector3 targetVelocity = wish * moveSpeed;

            float acceleration = grounded ? groundAcceleration : airAcceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, acceleration * dt);

            // --- земля / coyote time ---
            if (grounded)
            {
                coyoteTimer = coyoteTime;
            }
            else
            {
                coyoteTimer -= dt;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer -= dt;
            }

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                grounded = false;
                JumpCount++;
                if (AudioDirector.I != null) AudioDirector.I.PlayJump();
                var visual = GetComponentInChildren<PlayerVisual>();
                if (visual != null) visual.OnJump();
            }

            if (Input.GetKeyUp(KeyCode.Space) && verticalVelocity > 0f)
            {
                verticalVelocity *= 0.55f;
            }

            verticalVelocity -= gravity * dt;

            Vector3 motion = (planarVelocity + (Time.time <= conveyorUntil ? conveyorVelocity : Vector3.zero)) * dt;
            motion.y += verticalVelocity * dt;
            if (groundPlatform != null) motion += groundPlatform.Delta;

            float impactSpeed = verticalVelocity;
            wasGrounded = grounded;
            Vector3 beforeMove = transform.position;
            controller.Move(motion);
            Vector3 moved = transform.position - beforeMove;
            moved.y = 0f;
            travelledDistance += moved.magnitude;

            grounded = controller.isGrounded;
            if (grounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (!wasGrounded && impactSpeed < -6f)
                {
                    if (AudioDirector.I != null) AudioDirector.I.PlayLand();
                    var visual = GetComponentInChildren<PlayerVisual>();
                    if (visual != null) visual.OnLand();
                }
            }

            FindGroundPlatform();

            // --- поворот модели по направлению движения ---
            Vector3 flat = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
            if (visualRoot != null && flat.sqrMagnitude > 0.5f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(flat, Vector3.up);
                visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetRotation, 1f - Mathf.Exp(-rotationSpeed * dt));
            }
        }

        private void FindGroundPlatform()
        {
            groundPlatform = null;
            if (!grounded) return;

            Vector3 origin = transform.position + Vector3.up * 0.4f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 1.1f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.GetComponentInParent<PlayerController>() != null) continue;
                if (hit.distance > best) continue;
                var platform = hit.collider.GetComponentInParent<MovingPlatform>();
                if (platform != null)
                {
                    best = hit.distance;
                    groundPlatform = platform;
                }
            }
        }
    }
}
