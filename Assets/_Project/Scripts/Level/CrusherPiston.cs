using UnityEngine;

namespace SvoiPut
{
    /// <summary>Промышленный пресс: предупреждение, быстрый удар, пауза и подъём.</summary>
    public class CrusherPiston : CourseElement
    {
        [Header("Ссылки")]
        public Transform headGroup;
        public Collider hazardCollider;
        public Light warningLight;
        public Renderer[] glowRenderers;
        public ParticleSystem impactParticles;
        public Light impactLight;

        [Header("Ход")]
        public float topY = 3.15f;
        public float bottomY = 0.55f;

        [Header("Тайминг")]
        public float upTime = 1.25f;
        public float warnTime = 0.55f;
        public float slamTime = 0.10f;
        public float downTime = 0.75f;
        public float riseTime = 1.0f;
        public float phase = 0f;

        private Material[] glowMaterials;
        private Color[] baseEmission;
        private Color[] baseColor;
        private float elapsed;
        private bool impactDone;
        private float impactLightBase;

        private static readonly Color WarnColor = new Color(1f, 0.56f, 0.14f);
        private static readonly Color DangerColor = new Color(1f, 0.18f, 0.06f);

        private void Awake()
        {
            if (glowRenderers != null)
            {
                glowMaterials = new Material[glowRenderers.Length];
                baseEmission = new Color[glowRenderers.Length];
                baseColor = new Color[glowRenderers.Length];
                for (int i = 0; i < glowRenderers.Length; i++)
                {
                    if (glowRenderers[i] == null) continue;
                    glowMaterials[i] = new Material(glowRenderers[i].sharedMaterial);
                    glowRenderers[i].material = glowMaterials[i];
                    baseEmission[i] = glowMaterials[i].GetColor("_EmissionColor");
                    baseColor[i] = glowMaterials[i].color;
                }
            }
            if (impactLight != null) impactLightBase = impactLight.intensity;
            Apply(0f, true);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float cycle = upTime + warnTime + slamTime + downTime + riseTime;
            if (cycle < 0.2f) cycle = 0.2f;
            float t = Mathf.Repeat(elapsed + phase * cycle, cycle);
            Apply(t, false);

            if (impactLight != null)
            {
                impactLight.intensity = Mathf.MoveTowards(impactLight.intensity, impactLightBase, Time.deltaTime * 24f);
            }
        }

        private void Apply(float t, bool initial)
        {
            float slamStart = upTime + warnTime;
            float slamEnd = slamStart + slamTime;
            float downEnd = slamEnd + downTime;

            float y = topY;
            bool dangerous = false;
            bool warn = false;
            Color glow = WarnColor;
            float glowPower = 0.55f;

            if (t < upTime)
            {
                y = topY;
                glow = WarnColor;
                glowPower = 0.35f;
            }
            else if (t < slamStart)
            {
                y = topY;
                warn = true;
                float blink = Mathf.Repeat((t - upTime) * 12f, 1f) > 0.5f ? 1f : 0.25f;
                glow = WarnColor;
                glowPower = 1.6f * blink;
            }
            else if (t < slamEnd)
            {
                float k = Mathf.Clamp01((t - slamStart) / Mathf.Max(0.001f, slamTime));
                y = Mathf.Lerp(topY, bottomY, k * k);
                dangerous = true;
                glow = DangerColor;
                glowPower = 2.8f;
                if (!impactDone && !initial)
                {
                    impactDone = true;
                    if (AudioDirector.I != null) AudioDirector.I.PlaySlam();
                }
            }
            else if (t < downEnd)
            {
                y = bottomY;
                dangerous = true;
                glow = DangerColor;
                glowPower = 2.4f;
                if (!impactDone && !initial)
                {
                    impactDone = true;
                    if (impactParticles != null) impactParticles.Emit(18);
                    if (impactLight != null) impactLight.intensity = impactLightBase + 4.5f;
                }
            }
            else
            {
                float k = Mathf.Clamp01((t - downEnd) / Mathf.Max(0.001f, riseTime));
                y = Mathf.Lerp(bottomY, topY, 1f - (1f - k) * (1f - k));
                glow = WarnColor;
                glowPower = 0.45f;
            }

            if (t < slamStart) impactDone = false;

            if (headGroup != null) headGroup.localPosition = new Vector3(0f, y, 0f);
            if (hazardCollider != null) hazardCollider.enabled = dangerous;

            if (warningLight != null)
            {
                warningLight.color = dangerous ? DangerColor : WarnColor;
                warningLight.intensity = dangerous ? 3.2f : (warn ? 1.8f + Mathf.Sin(Time.time * 22f) * 0.8f : 0.7f);
            }

            if (glowMaterials != null)
            {
                for (int i = 0; i < glowMaterials.Length; i++)
                {
                    if (glowMaterials[i] == null) continue;
                    glowMaterials[i].SetColor("_EmissionColor", glow * glowPower);
                    glowMaterials[i].color = Color.Lerp(baseColor[i], glow, dangerous ? 0.45f : 0.22f);
                }
            }
        }

        public override void ResetElement()
        {
            elapsed = 0f;
            impactDone = false;
            Apply(0f, true);
            if (impactParticles != null) impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (impactLight != null) impactLight.intensity = impactLightBase;
        }
    }
}
