using UnityEngine;

namespace SvoiPut
{
    /// <summary>Качающийся груз на цепи: маятник с опасной зоной вокруг шара.</summary>
    public class SwingingHazard : CourseElement
    {
        [Header("Ссылки")]
        public Transform arm;
        public Transform ball;
        public Light glow;

        [Header("Настройки")]
        public float amplitude = 58f;
        public float period = 2.6f;
        public float phase = 0f;
        public float ballSpin = 35f;

        private Quaternion armBase;
        private float elapsed;
        private float spin;
        private float glowBase = 2.2f;

        private void Awake()
        {
            if (arm != null) armBase = arm.localRotation;
            if (glow != null) glowBase = glow.intensity;
            Apply(0f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin((elapsed / Mathf.Max(0.2f, period) + phase) * Mathf.PI * 2f) * amplitude;
            if (arm != null) arm.localRotation = armBase * Quaternion.Euler(0f, 0f, angle);
            spin += ballSpin * Time.deltaTime;
            if (ball != null) ball.localRotation = Quaternion.Euler(0f, spin, 0f);
            if (glow != null) glow.intensity = glowBase + Mathf.Sin(Time.time * 11f) * 0.35f;
        }

        private void Apply(float angle)
        {
            if (arm != null) arm.localRotation = armBase * Quaternion.Euler(0f, 0f, angle);
        }

        public override void ResetElement()
        {
            elapsed = 0f;
            spin = 0f;
            Apply(0f);
            if (ball != null) ball.localRotation = Quaternion.identity;
            if (glow != null) glow.intensity = glowBase;
        }
    }
}
