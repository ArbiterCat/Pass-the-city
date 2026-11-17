using System.Collections;
using UnityEngine;

namespace SvoiPut
{
    /// <summary>Исчезающий пол: предупреждает миганием, пропадает и возвращается через несколько секунд.</summary>
    public class VanishingTile : CourseElement
    {
        public Renderer tileRenderer;
        public Collider solidCollider;
        public Transform triggerVolume;
        public float warnDuration = 0.55f;
        public float hiddenDuration = 2.6f;

        private Material material;
        private Color baseColor;
        private Color baseEmission;
        private bool triggered;
        private Coroutine routine;

        private void Awake()
        {
            if (tileRenderer != null)
            {
                material = new Material(tileRenderer.sharedMaterial);
                tileRenderer.material = material;
                baseColor = material.color;
                baseEmission = material.GetColor("_EmissionColor");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            if (GameManager.I == null || GameManager.I.State != GameState.Playing) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;

            triggered = true;
            routine = StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            float t = 0f;
            while (t < warnDuration)
            {
                t += Time.deltaTime;
                float blink = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 22f));
                if (material != null)
                {
                    material.SetColor("_EmissionColor", new Color(1f, 0.28f, 0.09f) * (1.7f * blink));
                    material.color = Color.Lerp(baseColor, new Color(0.38f, 0.09f, 0.04f), blink);
                }
                yield return null;
            }

            if (AudioDirector.I != null) AudioDirector.I.PlayCrumble();
            if (tileRenderer != null) tileRenderer.enabled = false;
            if (solidCollider != null) solidCollider.enabled = false;

            yield return new WaitForSeconds(hiddenDuration);

            if (tileRenderer != null) tileRenderer.enabled = true;
            if (solidCollider != null) solidCollider.enabled = true;
            if (material != null)
            {
                material.color = baseColor;
                material.SetColor("_EmissionColor", baseEmission);
            }
            triggered = false;
            routine = null;
        }

        public override void ResetElement()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            triggered = false;
            if (tileRenderer != null) tileRenderer.enabled = true;
            if (solidCollider != null) solidCollider.enabled = true;
            if (material != null)
            {
                material.color = baseColor;
                material.SetColor("_EmissionColor", baseEmission);
            }
        }
    }
}
