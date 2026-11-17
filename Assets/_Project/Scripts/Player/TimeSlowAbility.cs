using UnityEngine;

namespace SvoiPut
{
    /// <summary>Собственная механика: хроно-модуль замедляет мир, но не игрока. Расходует и восстанавливает энергию.</summary>
    public class TimeSlowAbility : MonoBehaviour
    {
        [Header("Ссылки")]
        public PlayerCamera playerCamera;

        [Header("Настройки")]
        public float maxEnergy = 100f;
        public float drainPerSecond = 26f;
        public float regenPerSecond = 17f;
        public float regenDelay = 0.65f;
        public float slowScale = 0.42f;
        public float minEnergyToActivate = 9f;

        private float energy;
        private float regenTimer;
        private bool active;
        private bool depleted;

        public bool Active => active;
        public float Energy01 => Mathf.Clamp01(energy / maxEnergy);
        public int ActivationCount { get; private set; }

        private void Awake()
        {
            energy = maxEnergy;
        }

        private void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;

            if (gm.State != GameState.Playing)
            {
                PushUI(gm);
                return;
            }

            float dt = Time.unscaledDeltaTime;
            bool want = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!want) depleted = false;

            if (active)
            {
                energy -= drainPerSecond * dt;
                regenTimer = regenDelay;
                if (energy <= 0f)
                {
                    energy = 0f;
                    depleted = true;
                    SetActive(false);
                }
                else if (!want)
                {
                    SetActive(false);
                }
            }
            else
            {
                if (regenTimer > 0f)
                {
                    regenTimer -= dt;
                }
                else
                {
                    energy = Mathf.Min(maxEnergy, energy + regenPerSecond * dt);
                }

                if (want && !depleted && energy >= minEnergyToActivate)
                {
                    SetActive(true);
                }
            }

            PushUI(gm);
        }

        private void PushUI(GameManager gm)
        {
            if (gm.ui != null) gm.ui.SetChrono(Energy01, active);
        }

        private void SetActive(bool value)
        {
            if (active == value) return;
            active = value;
            if (value) ActivationCount++;
            Time.timeScale = active ? slowScale : 1f;

            if (AudioDirector.I != null)
            {
                if (active) AudioDirector.I.PlaySlowOn();
                else AudioDirector.I.PlaySlowOff();
                AudioDirector.I.SetSlow(active);
            }
            if (playerCamera != null) playerCamera.SetSlow(active);
            if (GameManager.I != null && GameManager.I.ui != null) GameManager.I.ui.SetSlowMo(active);
        }

        /// <summary>Немедленно выключить без побочных эффектов паузы.</summary>
        public void ForceStop()
        {
            if (active)
            {
                active = false;
                Time.timeScale = 1f;
                if (AudioDirector.I != null) AudioDirector.I.SetSlow(false);
                if (playerCamera != null) playerCamera.SetSlow(false);
                if (GameManager.I != null && GameManager.I.ui != null) GameManager.I.ui.SetSlowMo(false);
            }
        }

        private void OnDisable()
        {
            if (active)
            {
                active = false;
                Time.timeScale = 1f;
            }
        }
    }
}
