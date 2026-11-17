using System.Collections;
using UnityEngine;

namespace SvoiPut
{
    /// <summary>Контрольная точка: заметная арка, звук, цвет и частицы при активации.</summary>
    public class Checkpoint : MonoBehaviour
    {
        public int index;
        public Transform spawnPoint;
        public Renderer[] glowRenderers;
        public Light beaconLight;
        public Transform icon;
        public ParticleSystem particles;
        public TextMesh label;

        public Color safeColor = new Color(1f, 0.68f, 0.18f);
        public Color activeColor = new Color(0.3f, 1f, 0.58f);

        public bool Activated { get; private set; }

        private Material[] materials;
        private Vector3 iconBasePosition;
        private Vector3 iconBaseScale = Vector3.one;
        private Coroutine pulseRoutine;

        private void Awake()
        {
            if (icon != null)
            {
                iconBasePosition = icon.localPosition;
                iconBaseScale = icon.localScale;
            }

            if (glowRenderers != null)
            {
                materials = new Material[glowRenderers.Length];
                for (int i = 0; i < glowRenderers.Length; i++)
                {
                    if (glowRenderers[i] == null) continue;
                    materials[i] = new Material(glowRenderers[i].sharedMaterial);
                    glowRenderers[i].material = materials[i];
                }
            }
            ApplyColors(false);
        }

        private void Update()
        {
            if (icon != null)
            {
                icon.Rotate(0f, 55f * Time.deltaTime, 0f, Space.Self);
                float bob = Mathf.Sin(Time.time * 2.3f) * 0.09f;
                icon.localPosition = iconBasePosition + new Vector3(0f, bob, 0f);
            }
            if (beaconLight != null && !Activated)
            {
                beaconLight.intensity = 1.6f + Mathf.Sin(Time.time * 3.2f) * 0.4f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Activated) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;
            if (GameManager.I != null) GameManager.I.ActivateCheckpoint(this);
        }

        public void ActivateVisual()
        {
            if (Activated) return;
            Activated = true;
            ApplyColors(true);
            if (beaconLight != null)
            {
                beaconLight.color = activeColor;
                beaconLight.intensity = 3.2f;
            }
            if (label != null) label.color = activeColor;
            if (particles != null)
            {
                var main = particles.main;
                main.startColor = activeColor;
                particles.Play();
                particles.Emit(24);
            }
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseIcon());
        }

        private IEnumerator PulseIcon()
        {
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin(Mathf.Clamp01(t / 0.6f) * Mathf.PI) * 0.9f;
                if (icon != null) icon.localScale = iconBaseScale * s;
                yield return null;
            }
            if (icon != null) icon.localScale = iconBaseScale;
            pulseRoutine = null;
        }

        public void ResetCheckpoint()
        {
            Activated = false;
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }
            if (icon != null) icon.localScale = iconBaseScale;
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (label != null) label.color = safeColor;
            ApplyColors(false);
            if (beaconLight != null)
            {
                beaconLight.color = safeColor;
                beaconLight.intensity = 1.9f;
            }
        }

        private void ApplyColors(bool active)
        {
            Color color = active ? activeColor : safeColor;
            if (materials != null)
            {
                foreach (var mat in materials)
                {
                    if (mat == null) continue;
                    mat.SetColor("_EmissionColor", color * (active ? 1.35f : 1.15f));
                    mat.color = Color.Lerp(new Color(0.04f, 0.05f, 0.07f), color, active ? 0.35f : 0.22f);
                }
            }
        }

        public Vector3 GetSpawnPosition()
        {
            if (spawnPoint != null) return spawnPoint.position;
            return transform.position + Vector3.up * 0.15f;
        }

        public Quaternion GetSpawnRotation()
        {
            if (spawnPoint != null) return spawnPoint.rotation;
            return Quaternion.identity;
        }
    }
}
