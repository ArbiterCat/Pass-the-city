using UnityEngine;

namespace SvoiPut
{
    /// <summary>Пульсирующий лазерный барьер: предупреждение -> опасность -> пауза.</summary>
    public class PulsingLaser : CourseElement
    {
        public Renderer[] beams;
        public Collider hazardCollider;
        public Light glowLight;
        public float offTime = 1.35f;
        public float warnTime = 0.6f;
        public float onTime = 1.15f;
        public float phase = 0f;

        private Material[] materials;
        private float elapsed;

        private static readonly Color OffColor = new Color(0.025f, 0.03f, 0.035f);
        private static readonly Color WarnColor = new Color(1f, 0.44f, 0.10f);
        private static readonly Color OnColor = new Color(0.78f, 0.93f, 1f);

        private void Awake()
        {
            materials = new Material[beams != null ? beams.Length : 0];
            for (int i = 0; i < materials.Length; i++)
            {
                if (beams[i] == null) continue;
                materials[i] = new Material(beams[i].sharedMaterial);
                beams[i].material = materials[i];
            }
            ApplyState(false, false);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float cycle = offTime + warnTime + onTime;
            float t = Mathf.Repeat(elapsed + phase, cycle);

            if (t < offTime)
            {
                ApplyState(false, false);
            }
            else if (t < offTime + warnTime)
            {
                bool blink = Mathf.Repeat(t * 14f, 1f) > 0.42f;
                ApplyState(blink, false);
            }
            else
            {
                ApplyState(true, true);
            }
        }

        private void ApplyState(bool visible, bool dangerous)
        {
            if (beams != null)
            {
                for (int i = 0; i < beams.Length; i++)
                {
                    if (beams[i] == null) continue;
                    beams[i].enabled = visible;
                    if (materials != null && i < materials.Length && materials[i] != null && visible)
                    {
                        Color color = dangerous ? OnColor : WarnColor;
                        materials[i].SetColor("_EmissionColor", color * (dangerous ? 3.4f : 1.1f));
                        materials[i].color = Color.Lerp(OffColor, color, 0.75f);
                    }
                }
            }
            if (hazardCollider != null) hazardCollider.enabled = dangerous;
            if (glowLight != null)
            {
                glowLight.enabled = visible;
                glowLight.color = dangerous ? OnColor : WarnColor;
                glowLight.intensity = dangerous ? 3.2f : 0.9f;
            }
        }

        public override void ResetElement()
        {
            elapsed = 0f;
            ApplyState(false, false);
        }
    }
}
