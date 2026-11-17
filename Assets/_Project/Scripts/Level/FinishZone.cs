using UnityEngine;

namespace SvoiPut
{
    /// <summary>Зелёный портал и спасательная капсула: финиш трассы.</summary>
    public class FinishZone : MonoBehaviour
    {
        public Transform ring;
        public ParticleSystem particles;
        public Light finishLight;

        private bool finished;

        private void Update()
        {
            if (ring != null) ring.Rotate(0f, 0f, 25f * Time.deltaTime, Space.Self);
            if (finishLight != null && !finished)
            {
                finishLight.intensity = 3.4f + Mathf.Sin(Time.time * 2.6f) * 0.7f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (finished) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.I == null) return;
            if (GameManager.I.State != GameState.Playing) return;

            finished = true;
            if (particles != null)
            {
                particles.Play();
                particles.Emit(80);
            }
            GameManager.I.Win();
        }
    }
}
