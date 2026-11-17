using UnityEngine;

namespace SvoiPut
{
    /// <summary>Простая процедурная анимация скафандра: покачивание, наклон, сопла и пыль при посадке.</summary>
    public class PlayerVisual : MonoBehaviour
    {
        public PlayerController controller;
        public Transform rig;
        public ParticleSystem thruster;
        public ParticleSystem landPuff;
        public Light suitLight;

        private Vector3 rigBasePosition;
        private float bobPhase;
        private float lightBaseIntensity = 0.8f;

        private void Awake()
        {
            if (rig != null) rigBasePosition = rig.localPosition;
            if (suitLight != null) lightBaseIntensity = suitLight.intensity;
        }

        private void Update()
        {
            if (controller == null || rig == null) return;

            float dt = Time.unscaledDeltaTime;
            float speed = controller.PlanarSpeed;
            bool grounded = controller.Grounded;

            if (grounded && speed > 0.2f)
            {
                bobPhase += dt * (6f + speed * 0.55f);
                float bob = Mathf.Sin(bobPhase) * 0.035f;
                rig.localPosition = rigBasePosition + new Vector3(0f, bob, 0f);
                float lean = Mathf.Clamp(speed / 6.4f, 0f, 1f) * 7f;
                rig.localRotation = Quaternion.Slerp(rig.localRotation, Quaternion.Euler(lean, 0f, Mathf.Sin(bobPhase * 0.5f) * 2f), 1f - Mathf.Exp(-10f * dt));
            }
            else
            {
                rig.localPosition = Vector3.Lerp(rig.localPosition, rigBasePosition, 1f - Mathf.Exp(-12f * dt));
                rig.localRotation = Quaternion.Slerp(rig.localRotation, Quaternion.identity, 1f - Mathf.Exp(-10f * dt));
            }

            if (thruster != null)
            {
                var emission = thruster.emission;
                emission.enabled = !grounded;
            }

            if (suitLight != null)
            {
                suitLight.intensity = lightBaseIntensity + Mathf.Sin(Time.time * 2.2f) * 0.12f;
            }
        }

        public void OnJump()
        {
            if (landPuff != null) landPuff.Emit(8);
        }

        public void OnLand()
        {
            if (landPuff != null) landPuff.Emit(16);
        }
    }
}
