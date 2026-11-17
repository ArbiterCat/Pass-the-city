using UnityEngine;

namespace SvoiPut
{
    /// <summary>Платформа, движущаяся по отрезку. Игрок переносится вместе с ней через Delta.</summary>
    public class MovingPlatform : CourseElement
    {
        public Vector3 offset = new Vector3(0f, 0f, 8f);
        public float period = 5f;
        public float startPhase = 0f;

        private Vector3 anchorPosition;
        private float elapsed;
        private Vector3 lastPosition;

        public Vector3 Delta { get; private set; }

        private void Awake()
        {
            anchorPosition = transform.position;
            lastPosition = anchorPosition;
            RestartCycle();
        }

        private void Update()
        {
            if (period < 0.2f) period = 0.2f;
            elapsed += Time.deltaTime;
            float t = elapsed / period;
            float curve = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
            Vector3 position = anchorPosition + offset * curve;
            Delta = position - lastPosition;
            transform.position = position;
            lastPosition = position;
        }

        private void RestartCycle()
        {
            // Платформа всегда стартует в точке A; startPhase сдвигает фазу по времени.
            elapsed = -startPhase * Mathf.Max(0.2f, period);
            transform.position = anchorPosition;
            lastPosition = anchorPosition;
            Delta = Vector3.zero;
        }

        public override void ResetElement()
        {
            RestartCycle();
        }
    }
}
