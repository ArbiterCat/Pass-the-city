using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SvoiPut
{
    public enum GameState
    {
        Menu,
        Playing,
        Respawning,
        Paused,
        Won
    }

    /// <summary>Центральный менеджер: состояния игры, чекпоинты, пауза, победа и рестарт.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        [Header("Ссылки сцены")]
        public PlayerController player;
        public PlayerCamera playerCamera;
        public TimeSlowAbility timeSlow;
        public UIManager ui;
        public AudioDirector audio;
        public Transform startSpawn;
        public Checkpoint[] checkpoints;

        [Header("Настройки")]
        public float fallLimitY = -12f;
        public float respawnInvulnerability = 1.2f;

        public GameState State { get; private set; } = GameState.Menu;
        public int CurrentCheckpointIndex { get; private set; } = -1;
        public int FallCount { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsPlaying => State == GameState.Playing;
        public bool TutorialMode { get; private set; }

        private const int TutorialStepCount = 4;
        private int tutorialStep;
        private float invulnerableUntil;
        private bool quitting;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;
        }

        private void Start()
        {
            Time.timeScale = 1f;
            if (ui != null) ui.SetCursor(false);
            ResetCourse();
            if (GameSession.AutoStart)
            {
                GameSession.AutoStart = false;
                StartGame();
            }
            else
            {
                EnterMenu();
            }
        }

        private void Update()
        {
            if (State == GameState.Playing)
            {
                Elapsed += Time.unscaledDeltaTime;
                if (ui != null) ui.SetTimer(Elapsed);

                if (player != null && player.transform.position.y < fallLimitY)
                {
                    PlayerFailed("ПАДЕНИЕ");
                }

                UpdateTutorial();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing || State == GameState.Paused)
                {
                    TogglePause();
                }
                else if (State == GameState.Menu && ui != null && ui.RulesVisible)
                {
                    ui.ShowRules(false);
                }
            }

            if (State == GameState.Won && Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }

            if (State == GameState.Menu && !quitting && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                if (ui == null || !ui.RulesVisible) StartGame();
            }

            if (State == GameState.Menu && !quitting && Input.GetKeyDown(KeyCode.T))
            {
                if (ui == null || !ui.RulesVisible) StartTutorial();
            }

            if (Input.GetKeyDown(KeyCode.F11) || (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.LeftAlt)))
            {
                Screen.fullScreen = !Screen.fullScreen;
            }
        }

        private void EnterMenu()
        {
            State = GameState.Menu;
            TutorialMode = false;
            GameSession.Tutorial = false;
            SetCursor(false);
            if (player != null)
            {
                player.Teleport(startSpawn.position, startSpawn.rotation);
                player.SetControlEnabled(false);
            }
            if (playerCamera != null)
            {
                playerCamera.SetCinematic(true);
                playerCamera.SetInputEnabled(false);
            }
            if (timeSlow != null) timeSlow.ForceStop();
            if (ui != null)
            {
                ui.ShowMenu();
                ui.FadeTo(0f, 0.01f);
            }
            if (audio != null) audio.SetPaused(false);
        }

        public void StartGame()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;
            ResetCourse();

            Elapsed = 0f;
            FallCount = 0;
            CurrentCheckpointIndex = -1;
            State = GameState.Playing;
            invulnerableUntil = 0f;
            TutorialMode = GameSession.Tutorial;
            tutorialStep = 0;

            if (player != null)
            {
                player.Teleport(startSpawn.position + Vector3.up * 0.15f, startSpawn.rotation);
                player.SetControlEnabled(true);
            }
            if (playerCamera != null)
            {
                playerCamera.SetCinematic(false);
                playerCamera.SetInputEnabled(true);
                playerCamera.SnapBehind();
            }
            if (timeSlow != null) timeSlow.ForceStop();

            SetCursor(true);
            if (ui != null)
            {
                ui.ShowHUD();
                ui.ShowPause(false);
                ui.ShowVictory(false, 0f, 0, 0);
                ui.HideRespawn();
                ui.FadeTo(0f, 0.4f);
                ui.SetTimer(0f);
                ui.SetFalls(0);
                ui.SetCheckpoint(0, checkpoints != null ? checkpoints.Length : 0);
                ui.SetSector("01");
                ui.SetChrono(1f, false);
                ui.Toast("ПОСЛЕДНИЙ ПОЕЗД", UIColors.Chrome, 2.6f);
                if (TutorialMode)
                {
                    ui.ShowTutorial(true);
                    ui.SetTutorialStep(0, TutorialStepCount, "WASD — ДВИЖЕНИЕ");
                    ui.Toast("ОБУЧЕНИЕ", UIColors.Chrono, 2.2f);
                }
                else
                {
                    ui.ShowTutorial(false);
                }
            }
            if (audio != null)
            {
                audio.SetPaused(false);
                audio.PlayStart();
            }
        }

        private void ResetCourse()
        {
            var elements = FindObjectsByType<CourseElement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var e in elements) e.ResetElement();

            if (checkpoints != null)
            {
                foreach (var cp in checkpoints)
                {
                    if (cp != null) cp.ResetCheckpoint();
                }
            }
            CurrentCheckpointIndex = -1;
        }

        public void ActivateCheckpoint(Checkpoint cp)
        {
            if (State != GameState.Playing || cp == null) return;
            if (cp.index <= CurrentCheckpointIndex) return;

            CurrentCheckpointIndex = cp.index;
            cp.ActivateVisual();
            if (audio != null) audio.PlayCheckpoint();
            if (ui != null)
            {
                ui.SetCheckpoint(CurrentCheckpointIndex + 1, checkpoints != null ? checkpoints.Length : 1);
                ui.Toast("ЧЕКПОИНТ " + (cp.index + 1).ToString("00"), UIColors.Checkpoint, 1.9f);
            }
            if (TutorialMode) CompleteTutorial();
        }

        public void StartTutorial()
        {
            GameSession.Tutorial = true;
            StartGame();
        }

        private void UpdateTutorial()
        {
            if (!TutorialMode || ui == null) return;

            switch (tutorialStep)
            {
                case 0:
                    if (player != null && player.TravelledDistance >= 4f)
                    {
                        AdvanceTutorial(1, "ПРОБЕЛ — ПРЫЖОК");
                    }
                    break;
                case 1:
                    if (player != null && player.JumpCount > 0)
                    {
                        AdvanceTutorial(2, "SHIFT — ХРОНО");
                    }
                    break;
                case 2:
                    if (timeSlow != null && timeSlow.ActivationCount > 0)
                    {
                        AdvanceTutorial(3, "ДОБЕРИСЬ ДО ЧЕКПОИНТА");
                    }
                    break;
            }
        }

        private void AdvanceTutorial(int step, string text)
        {
            tutorialStep = step;
            if (ui != null) ui.SetTutorialStep(step, TutorialStepCount, text);
            if (audio != null) audio.PlayClick();
        }

        private void CompleteTutorial()
        {
            if (!TutorialMode) return;
            TutorialMode = false;
            GameSession.Tutorial = false;
            Elapsed = 0f;
            if (ui != null)
            {
                ui.ShowTutorial(false);
                ui.SetTimer(0f);
                ui.Toast("ОБУЧЕНИЕ ЗАВЕРШЕНО", UIColors.ChromeBright, 2.6f);
            }
        }

        public void AnnounceSection(string title, string subtitle)
        {
            if (State != GameState.Playing) return;
            if (ui != null)
            {
                ui.SetSector(title);
                ui.Toast(subtitle, UIColors.Rust, 2.2f);
            }
            if (audio != null) audio.PlaySection();
        }

        public void PlayerFailed(string reason)
        {
            if (State != GameState.Playing) return;
            if (Time.unscaledTime < invulnerableUntil) return;
            FallCount++;
            if (ui != null) ui.SetFalls(FallCount);
            StartCoroutine(FailRoutine(reason));
        }

        private IEnumerator FailRoutine(string reason)
        {
            State = GameState.Respawning;
            if (player != null) player.SetControlEnabled(false);
            if (timeSlow != null) timeSlow.ForceStop();
            if (audio != null) audio.PlayFail();
            if (ui != null)
            {
                ui.FlashDamage();
                ui.ShowRespawn(reason);
            }

            yield return new WaitForSecondsRealtime(0.85f);

            if (ui != null) ui.FadeTo(1f, 0.22f);
            yield return new WaitForSecondsRealtime(0.32f);

            TeleportToRespawn();
            if (playerCamera != null) playerCamera.SnapBehind();
            yield return new WaitForSecondsRealtime(0.18f);

            if (ui != null) ui.FadeTo(0f, 0.3f);
            yield return new WaitForSecondsRealtime(0.32f);

            if (ui != null)
            {
                ui.HideRespawn();
            }
            State = GameState.Playing;
            invulnerableUntil = Time.unscaledTime + respawnInvulnerability;
            if (player != null) player.SetControlEnabled(true);
        }

        private void TeleportToRespawn()
        {
            Vector3 pos = startSpawn.position + Vector3.up * 0.15f;
            Quaternion rot = startSpawn.rotation;
            if (checkpoints != null && CurrentCheckpointIndex >= 0 && CurrentCheckpointIndex < checkpoints.Length)
            {
                var cp = checkpoints[CurrentCheckpointIndex];
                if (cp != null)
                {
                    pos = cp.GetSpawnPosition();
                    rot = cp.GetSpawnRotation();
                }
            }
            if (player != null) player.Teleport(pos, rot);
        }

        public void Win()
        {
            if (State != GameState.Playing) return;
            State = GameState.Won;
            if (player != null) player.SetControlEnabled(false);
            if (timeSlow != null) timeSlow.ForceStop();
            if (playerCamera != null) playerCamera.SetInputEnabled(false);
            SetCursor(false);
            if (audio != null) audio.PlayWin();
            if (ui != null)
            {
                int cpCount = CurrentCheckpointIndex + 1;
                ui.ShowVictory(true, Elapsed, FallCount, cpCount);
                ui.HideRespawn();
            }
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                State = GameState.Paused;
                if (timeSlow != null) timeSlow.ForceStop();
                Time.timeScale = 0f;
                if (player != null) player.SetControlEnabled(false);
                if (playerCamera != null) playerCamera.SetInputEnabled(false);
                if (audio != null) audio.SetPaused(true);
                SetCursor(false);
                if (ui != null) ui.ShowPause(true);
            }
            else if (State == GameState.Paused)
            {
                State = GameState.Playing;
                Time.timeScale = 1f;
                if (player != null) player.SetControlEnabled(true);
                if (playerCamera != null) playerCamera.SetInputEnabled(true);
                if (audio != null) audio.SetPaused(false);
                SetCursor(true);
                if (ui != null) ui.ShowPause(false);
            }
        }

        public void RestartGame()
        {
            GameSession.AutoStart = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void GoToMenu()
        {
            GameSession.AutoStart = false;
            GameSession.Tutorial = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            quitting = true;
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SetCursor(bool locked)
        {
            if (ui != null) ui.SetCursor(locked);
        }
    }
}
