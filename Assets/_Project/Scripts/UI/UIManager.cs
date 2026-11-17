using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SvoiPut
{
    /// <summary>Интерфейс в графитово-хромовой гамме: меню, HUD, пауза, победа и служебные оверлеи.</summary>
    public class UIManager : MonoBehaviour
    {
        private Font font;
        private Canvas canvas;

        private GameObject menuRoot;
        private GameObject rulesRoot;
        private GameObject hudRoot;
        private GameObject pauseRoot;
        private GameObject victoryRoot;
        private GameObject respawnRoot;
        private GameObject tutorialRoot;

        private Text timerText;
        private Text checkpointText;
        private Text fallsText;
        private Text sectorText;
        private Text chronoValueText;
        private Text toastText;
        private Text tutorialTitleText;
        private Text tutorialStepText;
        private Text respawnReasonText;
        private Text victoryTimeText;
        private Text victoryFallsText;
        private Text victoryCheckpointText;
        private Image[] checkpointPips;
        private Image[] tutorialPips;

        private Image toastAccent;
        private RectTransform chronoFill;
        private Image chronoFillImage;
        private CanvasGroup toastGroup;
        private CanvasGroup respawnGroup;
        private CanvasGroup slowGroup;
        private Image damageImage;
        private Image fadeImage;

        private Coroutine toastRoutine;
        private Coroutine damageRoutine;
        private Coroutine fadeRoutine;

        private bool slowTarget;
        private float fadeTarget;
        private float fadeDuration;
        private float fadeAlpha;

        public bool RulesVisible => rulesRoot != null && rulesRoot.activeSelf;

        private static readonly Vector2 TL = new Vector2(0f, 1f);
        private static readonly Vector2 TC = new Vector2(0.5f, 1f);
        private static readonly Vector2 TR = new Vector2(1f, 1f);
        private static readonly Vector2 CC = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 BL = new Vector2(0f, 0f);
        private static readonly Vector2 BR = new Vector2(1f, 0f);

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            BuildCanvas();
            BuildHud();
            BuildTutorial();
            BuildRespawn();
            BuildPause();
            BuildVictory();
            BuildMenu();
            BuildRules();
            BuildEffects();

            ShowMenu();
            SetCursor(false);
        }

        private void Update()
        {
            if (slowGroup != null)
            {
                slowGroup.alpha = Mathf.MoveTowards(slowGroup.alpha, slowTarget ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            }
        }

        // ---------------------------------------------------------------- public API

        public void ShowMenu()
        {
            if (menuRoot != null) menuRoot.SetActive(true);
            if (hudRoot != null) hudRoot.SetActive(false);
            if (pauseRoot != null) pauseRoot.SetActive(false);
            if (victoryRoot != null) victoryRoot.SetActive(false);
            if (rulesRoot != null) rulesRoot.SetActive(false);
            if (respawnGroup != null) respawnGroup.alpha = 0f;
            if (toastGroup != null) toastGroup.alpha = 0f;
            if (slowGroup != null) slowGroup.alpha = 0f;
            if (tutorialRoot != null) tutorialRoot.SetActive(false);
            slowTarget = false;
            SetCursor(false);
        }

        public void ShowHUD()
        {
            if (menuRoot != null) menuRoot.SetActive(false);
            if (rulesRoot != null) rulesRoot.SetActive(false);
            if (hudRoot != null) hudRoot.SetActive(true);
            if (pauseRoot != null) pauseRoot.SetActive(false);
            if (victoryRoot != null) victoryRoot.SetActive(false);
            SetCursor(true);
        }

        public void ShowPause(bool show)
        {
            if (pauseRoot != null) pauseRoot.SetActive(show);
            if (show) SetCursor(false);
        }

        public void ShowRules(bool show)
        {
            if (rulesRoot != null) rulesRoot.SetActive(show);
            SetCursor(false);
        }

        public void ShowVictory(bool show, float time, int falls, int checkpoints)
        {
            if (victoryRoot == null) return;
            victoryRoot.SetActive(show);
            if (!show) return;
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
                toastRoutine = null;
            }
            if (toastGroup != null) toastGroup.alpha = 0f;
            if (victoryTimeText != null) victoryTimeText.text = FormatTime(time);
            if (victoryFallsText != null) victoryFallsText.text = falls.ToString();
            if (victoryCheckpointText != null) victoryCheckpointText.text = checkpoints + " / 3";
            SetCursor(false);
        }

        public void SetTimer(float seconds)
        {
            if (timerText != null) timerText.text = FormatTime(seconds);
        }

        public void SetFalls(int value)
        {
            if (fallsText != null) fallsText.text = "× " + value;
        }

        public void SetCheckpoint(int active, int total)
        {
            if (checkpointText != null) checkpointText.text = Mathf.Max(0, active).ToString("00") + " / " + total.ToString("00");
            if (checkpointPips == null) return;
            for (int i = 0; i < checkpointPips.Length; i++)
            {
                if (checkpointPips[i] == null) continue;
                bool on = i < active;
                checkpointPips[i].color = on ? UIColors.Checkpoint : UIColors.GraphiteLighter;
            }
        }

        public void SetSector(string title)
        {
            if (sectorText != null) sectorText.text = title;
        }

        public void SetChrono(float value, bool active)
        {
            value = Mathf.Clamp01(value);
            if (chronoFill != null)
            {
                var max = chronoFill.anchorMax;
                max.x = value;
                chronoFill.anchorMax = max;
            }
            if (chronoValueText != null)
            {
                chronoValueText.text = active ? "ON" : "SHIFT";
                chronoValueText.color = active ? UIColors.Chrono : UIColors.Dim;
            }
            if (chronoFillImage != null)
            {
                if (active)
                {
                    float pulse = 0.72f + 0.28f * Mathf.Sin(Time.unscaledTime * 9f);
                    chronoFillImage.color = new Color(UIColors.ChromeBright.r, UIColors.ChromeBright.g, UIColors.ChromeBright.b, 0.65f + 0.35f * pulse);
                }
                else if (value < 0.22f) chronoFillImage.color = UIColors.Danger;
                else if (value < 0.5f) chronoFillImage.color = UIColors.Rust;
                else chronoFillImage.color = UIColors.Chrono;
            }
        }

        public void Toast(string message, Color color, float duration = 2.4f)
        {
            if (toastGroup == null || toastText == null) return;
            toastText.text = message;
            toastText.color = color;
            if (toastAccent != null) toastAccent.color = color;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine(duration));
        }

        private IEnumerator ToastRoutine(float hold)
        {
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.unscaledDeltaTime;
                toastGroup.alpha = Mathf.Clamp01(t / 0.16f);
                yield return null;
            }
            toastGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(hold);
            t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                toastGroup.alpha = 1f - Mathf.Clamp01(t / 0.35f);
                yield return null;
            }
            toastGroup.alpha = 0f;
            toastRoutine = null;
        }

        public void FlashDamage()
        {
            if (damageImage == null) return;
            if (damageRoutine != null) StopCoroutine(damageRoutine);
            damageRoutine = StartCoroutine(DamageRoutine());
        }

        private IEnumerator DamageRoutine()
        {
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(0.40f, 0f, Mathf.Clamp01(t / 0.6f));
                damageImage.color = new Color(UIColors.RustBright.r, UIColors.RustBright.g, UIColors.RustBright.b, a);
                yield return null;
            }
            damageImage.color = new Color(UIColors.RustBright.r, UIColors.RustBright.g, UIColors.RustBright.b, 0f);
            damageRoutine = null;
        }

        public void ShowRespawn(string reason)
        {
            if (respawnRoot == null) return;
            respawnRoot.SetActive(true);
            if (respawnReasonText != null) respawnReasonText.text = reason;
            if (respawnGroup != null) respawnGroup.alpha = 1f;
        }

        public void HideRespawn()
        {
            if (respawnGroup != null) respawnGroup.alpha = 0f;
            if (respawnRoot != null) respawnRoot.SetActive(false);
        }

        public void SetSlowMo(bool value)
        {
            slowTarget = value;
        }

        public void ShowTutorial(bool show)
        {
            if (tutorialRoot != null) tutorialRoot.SetActive(show);
        }

        public void SetTutorialStep(int index, int total, string step)
        {
            if (tutorialRoot == null) return;
            tutorialRoot.SetActive(true);
            if (tutorialTitleText != null)
            {
                tutorialTitleText.text = "ОБУЧЕНИЕ " + (index + 1).ToString("00") + " / " + total.ToString("00");
            }
            if (tutorialStepText != null)
            {
                tutorialStepText.text = step;
            }
            if (tutorialPips == null) return;
            for (int i = 0; i < tutorialPips.Length; i++)
            {
                if (tutorialPips[i] == null) continue;
                tutorialPips[i].color = i < index ? UIColors.Chrome : (i == index ? UIColors.Chrono : UIColors.GraphiteLighter);
            }
        }

        public void FadeTo(float alpha, float duration)
        {
            fadeTarget = Mathf.Clamp01(alpha);
            fadeDuration = Mathf.Max(0.0001f, duration);
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            float start = fadeAlpha;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                fadeAlpha = Mathf.Lerp(start, fadeTarget, Mathf.Clamp01(t / fadeDuration));
                if (fadeImage != null) fadeImage.color = new Color(0.015f, 0.018f, 0.022f, fadeAlpha);
                yield return null;
            }
            fadeAlpha = fadeTarget;
            if (fadeImage != null) fadeImage.color = new Color(0.015f, 0.018f, 0.022f, fadeAlpha);
            fadeRoutine = null;
        }

        public void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // ---------------------------------------------------------------- построение

        private void BuildCanvas()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void BuildHud()
        {
            hudRoot = MakeStretch("HUD", canvas.transform, Color.clear).gameObject;
            var root = (RectTransform)hudRoot.transform;

            checkpointText = MakeText("CP_VALUE", root, "00 / 03", 38, UIColors.Text, TextAnchor.MiddleLeft, TL, new Vector2(40f, -24f), new Vector2(320f, 48f), FontStyle.Bold);
            checkpointPips = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                checkpointPips[i] = MakeImage("CP_PIP_" + i, root, TL, new Vector2(40f + i * 54f, -82f), new Vector2(46f, 4f), UIColors.GraphiteLighter);
            }

            timerText = MakeText("TIME", root, "00:00.0", 42, UIColors.Text, TextAnchor.MiddleCenter, TC, new Vector2(0f, -24f), new Vector2(360f, 52f), FontStyle.Bold);
            fallsText = MakeText("FALLS", root, "× 0", 20, UIColors.Dim, TextAnchor.MiddleRight, TR, new Vector2(-40f, -26f), new Vector2(180f, 30f));
            sectorText = MakeText("SECTOR", root, "01", 13, UIColors.Dim, TextAnchor.MiddleRight, TR, new Vector2(-40f, -56f), new Vector2(180f, 20f));

            var controls = MakePanel("CONTROLS", root, BL, new Vector2(40f, 30f), new Vector2(660f, 42f), new Color(0.125f, 0.145f, 0.169f, 0.72f), BL);
            MakeImage("ACCENT", controls, BL, Vector2.zero, new Vector2(3f, 42f), UIColors.Rust);
            MakeText("TEXT", controls, "WASD  ·  ПРОБЕЛ  ·  SHIFT  ·  ESC",
                14, UIColors.Dim, TextAnchor.MiddleLeft, BL, new Vector2(18f, 21f), new Vector2(620f, 26f), FontStyle.Normal, new Vector2(0f, 0.5f));

            var chrono = MakePanel("CHRONO", root, BR, new Vector2(-40f, 30f), new Vector2(360f, 56f), new Color(0.125f, 0.145f, 0.169f, 0.80f), BR);
            MakeImage("ACCENT", chrono, BL, Vector2.zero, new Vector2(3f, 56f), UIColors.Chrome);
            MakeText("LABEL", chrono, "ХРОНО", 12, UIColors.Chrome, TextAnchor.MiddleLeft, TL, new Vector2(16f, -7f), new Vector2(180f, 18f), FontStyle.Bold);
            chronoValueText = MakeText("VALUE", chrono, "SHIFT", 11, UIColors.Dim, TextAnchor.MiddleRight, TR, new Vector2(-16f, -8f), new Vector2(140f, 18f));

            var barBack = MakeImage("BAR_BG", chrono, BL, new Vector2(16f, 10f), new Vector2(328f, 7f), UIColors.Graphite);
            chronoFillImage = MakeImage("FILL", barBack.rectTransform, BL, Vector2.zero, new Vector2(328f, 7f), UIColors.Chrono);
            chronoFill = chronoFillImage.rectTransform;
            chronoFill.anchorMin = Vector2.zero;
            chronoFill.anchorMax = Vector2.one;
            chronoFill.pivot = new Vector2(0f, 0.5f);
            chronoFill.offsetMin = Vector2.zero;
            chronoFill.offsetMax = Vector2.zero;

            var toast = MakePanel("TOAST", root, TC, new Vector2(0f, -100f), new Vector2(720f, 44f), new Color(0.125f, 0.145f, 0.169f, 0.92f), TC);
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastAccent = MakeImage("ACCENT", toast, TL, Vector2.zero, new Vector2(4f, 44f), UIColors.Rust);
            toastText = MakeText("TEXT", toast, "", 19, UIColors.Chrome, TextAnchor.MiddleCenter, CC, Vector2.zero, new Vector2(690f, 34f), FontStyle.Bold);
        }

        private void BuildTutorial()
        {
            tutorialRoot = MakePanel("TUTORIAL", canvas.transform, BL, new Vector2(40f, 92f), new Vector2(560f, 96f),
                new Color(0.11f, 0.13f, 0.15f, 0.92f), BL).gameObject;
            var root = (RectTransform)tutorialRoot.transform;
            MakeImage("ACCENT", root, TL, Vector2.zero, new Vector2(5f, 96f), UIColors.Chrono);
            tutorialTitleText = MakeText("TITLE", root, "ОБУЧЕНИЕ 01 / 04", 11, UIColors.Chrono, TextAnchor.MiddleLeft,
                TL, new Vector2(22f, -10f), new Vector2(500f, 18f), FontStyle.Bold);
            tutorialStepText = MakeText("STEP", root, "WASD — ДВИЖЕНИЕ", 21, UIColors.Text, TextAnchor.MiddleLeft,
                TL, new Vector2(22f, -34f), new Vector2(510f, 30f), FontStyle.Bold);

            tutorialPips = new Image[4];
            for (int i = 0; i < tutorialPips.Length; i++)
            {
                tutorialPips[i] = MakeImage("PIP_" + i, root, BL, new Vector2(22f + i * 42f, 16f), new Vector2(34f, 4f),
                    i == 0 ? UIColors.Chrono : UIColors.GraphiteLighter);
            }
            tutorialRoot.SetActive(false);
        }

        private void BuildRespawn()
        {
            respawnRoot = MakePanel("RESPAWN", canvas.transform, CC, new Vector2(0f, 20f), new Vector2(600f, 112f), new Color(0.125f, 0.145f, 0.169f, 0.95f), CC).gameObject;
            var root = (RectTransform)respawnRoot.transform;
            respawnGroup = respawnRoot.AddComponent<CanvasGroup>();
            respawnGroup.alpha = 0f;
            MakeImage("ACCENT", root, TL, Vector2.zero, new Vector2(4f, 112f), UIColors.Rust);
            MakeText("TITLE", root, "ОШИБКА", 22, UIColors.Rust, TextAnchor.MiddleLeft, TL, new Vector2(28f, -14f), new Vector2(520f, 28f), FontStyle.Bold);
            respawnReasonText = MakeText("REASON", root, "ПАДЕНИЕ", 20, UIColors.Text, TextAnchor.MiddleLeft, TL, new Vector2(28f, -50f), new Vector2(540f, 30f));
            respawnRoot.SetActive(false);
        }

        private void BuildPause()
        {
            pauseRoot = MakeStretch("PAUSE", canvas.transform, new Color(0.02f, 0.025f, 0.03f, 0.68f)).gameObject;
            var root = (RectTransform)pauseRoot.transform;
            var panel = MakePanel("PANEL", root, CC, Vector2.zero, new Vector2(600f, 470f), UIColors.Panel, CC);
            MakeImage("TOP", panel, TC, Vector2.zero, new Vector2(600f, 3f), UIColors.Rust);
            MakeText("TITLE", panel, "ПАУЗА", 44, UIColors.Text, TextAnchor.MiddleCenter, TC, new Vector2(0f, -34f), new Vector2(520f, 56f), FontStyle.Bold);

            MakeButton(panel, "ПРОДОЛЖИТЬ", new Vector2(100f, -110f), new Vector2(400f, 56f), true, () => GameManager.I.TogglePause());
            MakeButton(panel, "ЗАНОВО", new Vector2(100f, -178f), new Vector2(400f, 56f), false, () => GameManager.I.RestartGame());
            MakeButton(panel, "МЕНЮ", new Vector2(100f, -246f), new Vector2(400f, 56f), false, () => GameManager.I.GoToMenu());
            MakeButton(panel, "ВЫХОД", new Vector2(100f, -314f), new Vector2(400f, 56f), false, () => GameManager.I.QuitGame());
            pauseRoot.SetActive(false);
        }

        private void BuildVictory()
        {
            victoryRoot = MakeStretch("VICTORY", canvas.transform, new Color(0.02f, 0.025f, 0.03f, 0.82f)).gameObject;
            var root = (RectTransform)victoryRoot.transform;
            var panel = MakePanel("PANEL", root, CC, Vector2.zero, new Vector2(680f, 500f), UIColors.Panel, CC);
            MakeImage("TOP", panel, TC, Vector2.zero, new Vector2(680f, 4f), UIColors.Rust);
            MakeText("TITLE", panel, "ФИНИШ", 56, UIColors.Checkpoint, TextAnchor.MiddleCenter, TC, new Vector2(0f, -30f), new Vector2(600f, 72f), FontStyle.Bold);

            MakeText("L1", panel, "ВРЕМЯ", 15, UIColors.Dim, TextAnchor.MiddleLeft, TL, new Vector2(64f, -132f), new Vector2(300f, 28f));
            victoryTimeText = MakeText("V1", panel, "00:00.0", 26, UIColors.Text, TextAnchor.MiddleRight, TR, new Vector2(-64f, -130f), new Vector2(300f, 32f), FontStyle.Bold);
            MakeImage("SEP1", panel, TL, new Vector2(64f, -170f), new Vector2(552f, 1f), UIColors.Line);

            MakeText("L2", panel, "СБОИ", 15, UIColors.Dim, TextAnchor.MiddleLeft, TL, new Vector2(64f, -190f), new Vector2(300f, 28f));
            victoryFallsText = MakeText("V2", panel, "0", 26, UIColors.Text, TextAnchor.MiddleRight, TR, new Vector2(-64f, -188f), new Vector2(300f, 32f), FontStyle.Bold);
            MakeImage("SEP2", panel, TL, new Vector2(64f, -228f), new Vector2(552f, 1f), UIColors.Line);

            MakeText("L3", panel, "ЧЕКПОИНТЫ", 15, UIColors.Dim, TextAnchor.MiddleLeft, TL, new Vector2(64f, -248f), new Vector2(360f, 28f));
            victoryCheckpointText = MakeText("V3", panel, "3 / 3", 26, UIColors.Checkpoint, TextAnchor.MiddleRight, TR, new Vector2(-64f, -246f), new Vector2(260f, 32f), FontStyle.Bold);

            MakeButton(panel, "ЕЩЁ РАЗ", new Vector2(110f, -304f), new Vector2(460f, 56f), true, () => GameManager.I.RestartGame());
            MakeButton(panel, "В МЕНЮ", new Vector2(110f, -372f), new Vector2(460f, 56f), false, () => GameManager.I.GoToMenu());
            MakeText("HINT", panel, "R", 11, UIColors.Dim, TextAnchor.MiddleCenter, TC, new Vector2(0f, -442f), new Vector2(300f, 18f));
            victoryRoot.SetActive(false);
        }

        private void BuildMenu()
        {
            menuRoot = MakeStretch("MENU", canvas.transform, Color.clear).gameObject;
            var root = (RectTransform)menuRoot.transform;

            var gradient = MakeImage("GRADIENT", root, BL, Vector2.zero, new Vector2(1920f, 1080f), Color.white);
            gradient.sprite = CreateGradientSprite();
            gradient.type = Image.Type.Simple;
            gradient.preserveAspect = false;
            gradient.rectTransform.anchorMin = Vector2.zero;
            gradient.rectTransform.anchorMax = Vector2.one;
            gradient.rectTransform.offsetMin = Vector2.zero;
            gradient.rectTransform.offsetMax = Vector2.zero;

            MakeImage("EDGE", root, TL, Vector2.zero, new Vector2(7f, 1080f), UIColors.Rust);

            MakeText("KICKER", root, "UNITY  ·  2026", 12, UIColors.Dim, TextAnchor.MiddleLeft, TL, new Vector2(140f, -48f), new Vector2(520f, 20f));
            MakeText("TITLE", root, "СВОЙ ПУТЬ", 96, UIColors.Text, TextAnchor.MiddleLeft, TL, new Vector2(136f, -80f), new Vector2(900f, 118f), FontStyle.Bold);
            MakeImage("LINE", root, TL, new Vector2(140f, -210f), new Vector2(82f, 4f), UIColors.Rust);
            MakeText("SUBTITLE", root, "РЖАВЫЙ КВАРТАЛ", 26, UIColors.Chrome, TextAnchor.MiddleLeft, TL, new Vector2(140f, -230f), new Vector2(820f, 38f));
            MakeText("DESC", root, "ПОСЛЕДНИЙ ПОЕЗД ЖДЁТ", 18, UIColors.Dim, TextAnchor.MiddleLeft, TL, new Vector2(140f, -286f), new Vector2(760f, 28f));

            MakeButton(root, "ИГРАТЬ", new Vector2(140f, -360f), new Vector2(400f, 64f), true, () => GameManager.I.StartGame());
            MakeButton(root, "ОБУЧЕНИЕ", new Vector2(140f, -432f), new Vector2(400f, 56f), false, () => GameManager.I.StartTutorial());
            MakeButton(root, "УПРАВЛЕНИЕ", new Vector2(140f, -502f), new Vector2(400f, 56f), false, () => ShowRules(true));
            MakeButton(root, "ВЫХОД", new Vector2(140f, -572f), new Vector2(400f, 56f), false, () => GameManager.I.QuitGame());

            MakeText("KEYHINT", root, "ENTER — ИГРАТЬ  ·  T — ОБУЧЕНИЕ", 11, UIColors.Dim, TextAnchor.MiddleLeft, TL, new Vector2(144f, -646f), new Vector2(520f, 20f));
            MakeText("FOOTER", root, "ИНДИВИДУАЛЬНЫЙ ПРОЕКТ", 11, UIColors.Dim, TextAnchor.MiddleLeft, BL, new Vector2(140f, 40f), new Vector2(900f, 20f));
            MakeText("F11", root, "F11", 11, UIColors.Dim, TextAnchor.MiddleRight, BR, new Vector2(-40f, 40f), new Vector2(120f, 20f));

            var card = MakePanel("ROUTE_CARD", root, TR, new Vector2(-140f, -160f), new Vector2(420f, 320f), UIColors.Panel, TR);
            MakeImage("TOP", card, TC, Vector2.zero, new Vector2(420f, 3f), UIColors.Rust);
            MakeText("TITLE", card, "МАРШРУТ", 13, UIColors.Rust, TextAnchor.MiddleLeft, TL, new Vector2(24f, -22f), new Vector2(260f, 22f), FontStyle.Bold);
            MakeImage("SPINE", card, TL, new Vector2(40f, -72f), new Vector2(2f, 156f), UIColors.Line);

            string[] numbers = { "01", "02", "03", "→" };
            string[] names = { "КРЫШИ", "ЦЕХ", "ДЕПО", "ПОЕЗД" };
            Color[] colors = { UIColors.Rust, UIColors.Rust, UIColors.Rust, UIColors.Chrome };
            for (int i = 0; i < 4; i++)
            {
                float y = -72f - i * 52f;
                var node = MakeImage("NODE_" + i, card, TL, new Vector2(31f, y - 10f), new Vector2(20f, 20f), colors[i]);
                node.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                MakeText("NUMBER_" + i, card, numbers[i], 12, colors[i], TextAnchor.MiddleLeft, TL, new Vector2(66f, y - 12f), new Vector2(42f, 24f), FontStyle.Bold);
                MakeText("NAME_" + i, card, names[i], 17, UIColors.Text, TextAnchor.MiddleLeft, TL, new Vector2(120f, y - 12f), new Vector2(260f, 24f), i == 3 ? FontStyle.Bold : FontStyle.Normal);
            }
            MakeText("CLOCK", card, "03:07", 11, UIColors.Dim, TextAnchor.MiddleRight, BR, new Vector2(-20f, 16f), new Vector2(160f, 18f));
        }

        private void BuildRules()
        {
            var rulesBackdrop = MakeStretch("RULES", canvas.transform, new Color(0.02f, 0.025f, 0.03f, 0.82f));
            rulesBackdrop.raycastTarget = true;
            rulesRoot = rulesBackdrop.gameObject;
            var root = (RectTransform)rulesRoot.transform;
            var panel = MakePanel("PANEL", root, CC, Vector2.zero, new Vector2(900f, 600f), UIColors.Panel, CC);
            MakeImage("TOP", panel, TC, Vector2.zero, new Vector2(900f, 3f), UIColors.Rust);
            MakeText("TITLE", panel, "УПРАВЛЕНИЕ", 32, UIColors.Text, TextAnchor.MiddleLeft, TL, new Vector2(48f, -32f), new Vector2(600f, 42f), FontStyle.Bold);

            string[] keys = { "WASD", "МЫШЬ", "ПРОБЕЛ", "SHIFT", "ESC" };
            string[] actions = { "ДВИЖЕНИЕ", "КАМЕРА", "ПРЫЖОК", "ЗАМЕДЛЕНИЕ", "ПАУЗА" };
            for (int i = 0; i < keys.Length; i++)
            {
                float y = -88f - i * 52f;
                var chip = MakePanel("KEY_" + i, panel, TL, new Vector2(48f, y), new Vector2(150f, 36f), UIColors.GraphiteLight, TL);
                MakeImage("ACCENT", chip, BL, Vector2.zero, new Vector2(3f, 36f), UIColors.Rust);
                MakeText("KEY_TEXT", chip, keys[i], 13, UIColors.Chrome, TextAnchor.MiddleCenter, CC, Vector2.zero, new Vector2(140f, 30f), FontStyle.Bold);
                MakeText("ACTION_" + i, panel, actions[i], 17, UIColors.Text, TextAnchor.MiddleLeft, TL, new Vector2(220f, y), new Vector2(240f, 36f));
            }

            MakeText("H1", panel, "ЦЕЛЬ", 13, UIColors.Rust, TextAnchor.MiddleLeft, TL, new Vector2(520f, -88f), new Vector2(300f, 22f), FontStyle.Bold);
            MakeText("B1", panel, "ПОСЛЕДНИЙ ПОЕЗД", 19, UIColors.Text, TextAnchor.MiddleLeft, TL, new Vector2(520f, -116f), new Vector2(340f, 28f));

            MakeText("H2", panel, "ОШИБКА", 13, UIColors.Rust, TextAnchor.MiddleLeft, TL, new Vector2(520f, -170f), new Vector2(300f, 22f), FontStyle.Bold);
            MakeText("B2", panel, "ВОЗВРАТ К ЧЕКПОИНТУ", 18, UIColors.Text, TextAnchor.MiddleLeft, TL, new Vector2(520f, -198f), new Vector2(340f, 28f));

            MakeText("H3", panel, "ХРОНО", 13, UIColors.Chrome, TextAnchor.MiddleLeft, TL, new Vector2(520f, -252f), new Vector2(300f, 22f), FontStyle.Bold);
            MakeText("B3", panel, "ВРЕМЯ 0.4×", 18, UIColors.Text, TextAnchor.MiddleLeft, TL, new Vector2(520f, -280f), new Vector2(340f, 28f));

            MakeButton(panel, "НАЗАД", new Vector2(520f, -510f), new Vector2(170f, 52f), false, () => ShowRules(false));
            MakeButton(panel, "ИГРАТЬ", new Vector2(706f, -510f), new Vector2(170f, 52f), true, StartFromRules);
            rulesRoot.SetActive(false);
        }

        private void StartFromRules()
        {
            ShowRules(false);
            GameManager.I.StartGame();
        }

        private void BuildEffects()
        {
            var slow = MakeImage("SLOW", canvas.transform, BL, Vector2.zero, new Vector2(1920f, 1080f), new Color(0.62f, 0.68f, 0.72f, 0.08f));
            StretchFull(slow.rectTransform);
            slowGroup = slow.gameObject.AddComponent<CanvasGroup>();
            slowGroup.alpha = 0f;
            slowGroup.blocksRaycasts = false;

            damageImage = MakeImage("DAMAGE", canvas.transform, BL, Vector2.zero, new Vector2(1920f, 1080f), new Color(UIColors.RustBright.r, UIColors.RustBright.g, UIColors.RustBright.b, 0f));
            StretchFull(damageImage.rectTransform);
            damageImage.raycastTarget = false;

            var fade = MakeImage("FADE", canvas.transform, BL, Vector2.zero, new Vector2(1920f, 1080f), new Color(0.015f, 0.018f, 0.022f, 0f));
            StretchFull(fade.rectTransform);
            fadeImage = fade;
            fadeImage.raycastTarget = false;
            fade.rectTransform.SetAsLastSibling();
        }

        // ---------------------------------------------------------------- фабрика UI

        private static Vector2 PivotFor(Vector2 anchor)
        {
            return anchor;
        }

        private RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? PivotFor(anchor);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        private Image MakeImage(string name, Image parent, Vector2 anchor, Vector2 position, Vector2 size, Color color, Vector2? pivot = null)
        {
            return MakeImage(name, parent.transform, anchor, position, size, color, pivot);
        }

        private Image MakeImage(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color, Vector2? pivot = null)
        {
            var rt = NewRect(name, parent, anchor, position, size, pivot);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private Image MakeStretch(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            StretchFull(rt);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private Image MakePanel(string name, Image parent, Vector2 anchor, Vector2 position, Vector2 size, Color color, Vector2 pivot)
        {
            return MakePanel(name, parent.transform, anchor, position, size, color, pivot);
        }

        private Image MakePanel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color, Vector2 pivot)
        {
            return MakeImage(name, parent, anchor, position, size, color, pivot);
        }

        private Text MakeText(string name, Image parent, string value, int size, Color color, TextAnchor alignment,
            Vector2 anchor, Vector2 position, Vector2 rect, FontStyle style = FontStyle.Normal, Vector2? pivot = null)
        {
            return MakeText(name, parent.transform, value, size, color, alignment, anchor, position, rect, style, pivot);
        }

        private Text MakeText(string name, Transform parent, string value, int size, Color color, TextAnchor alignment,
            Vector2 anchor, Vector2 position, Vector2 rect, FontStyle style = FontStyle.Normal, Vector2? pivot = null)
        {
            var rt = NewRect(name, parent, anchor, position, rect, pivot);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.12f;
            text.raycastTarget = false;
            text.supportRichText = false;

            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private Button MakeButton(Image parent, string label, Vector2 position, Vector2 size, bool primary, System.Action onClick)
        {
            return MakeButton(parent.transform, label, position, size, primary, onClick);
        }

        private Button MakeButton(Transform parent, string label, Vector2 position, Vector2 size, bool primary, System.Action onClick)
        {
            var go = new GameObject("BTN_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = TL;
            rt.anchorMax = TL;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var bg = go.GetComponent<Image>();
            Color normalBg = primary ? new Color(0.60f, 0.20f, 0.12f, 0.98f) : new Color(0.18f, 0.20f, 0.23f, 0.96f);
            Color hoverBg = primary ? new Color(0.78f, 0.30f, 0.17f, 1f) : new Color(0.26f, 0.29f, 0.32f, 1f);
            Color pressedBg = primary ? new Color(0.42f, 0.13f, 0.07f, 1f) : new Color(0.12f, 0.14f, 0.16f, 1f);
            bg.color = normalBg;
            bg.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = bg;
            button.onClick.AddListener(() =>
            {
                if (AudioDirector.I != null) AudioDirector.I.PlayClick();
                onClick?.Invoke();
            });

            var accent = MakeImage("ACCENT", rt, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, size.y), primary ? UIColors.ChromeBright : UIColors.Rust, new Vector2(0f, 0.5f));
            var text = MakeText("LABEL", rt, label, primary ? 21 : 19, primary ? UIColors.ChromeBright : UIColors.Chrome, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(size.x - 44f, size.y - 8f), FontStyle.Bold, new Vector2(0f, 0.5f));

            var fx = go.AddComponent<ButtonFx>();
            fx.background = bg;
            fx.accent = accent;
            fx.label = text;
            fx.normalBackground = normalBg;
            fx.hoverBackground = hoverBg;
            fx.pressedBackground = pressedBg;
            fx.normalLabel = primary ? UIColors.ChromeBright : UIColors.Chrome;
            fx.hoverLabel = Color.white;
            fx.normalAccent = primary ? UIColors.ChromeBright : UIColors.Rust;
            fx.hoverAccent = primary ? Color.white : UIColors.RustBright;

            return button;
        }

        private Sprite CreateGradientSprite()
        {
            const int width = 256;
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[width];
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float alpha = Mathf.Pow(1f - t, 1.25f) * 0.94f;
                pixels[x] = new Color(0.065f, 0.072f, 0.078f, alpha);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, width, 1f), new Vector2(0.5f, 0.5f), 100f);
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int tenths = Mathf.FloorToInt(seconds * 10f) % 10;
            return minutes.ToString("00") + ":" + secs.ToString("00") + "." + tenths;
        }
    }
}
