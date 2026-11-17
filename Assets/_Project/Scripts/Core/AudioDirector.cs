using UnityEngine;

namespace SvoiPut
{
    /// <summary>Процедурный звук: все эффекты и эмбиент синтезируются на старте, без внешних ассетов.</summary>
    public class AudioDirector : MonoBehaviour
    {
        public static AudioDirector I { get; private set; }

        private const int SampleRate = 44100;

        private AudioSource ambientSource;
        private AudioSource sfxSource;

        private AudioClip clipJump;
        private AudioClip clipLand;
        private AudioClip clipCheckpoint;
        private AudioClip clipFail;
        private AudioClip clipWin;
        private AudioClip clipClick;
        private AudioClip clipSlowOn;
        private AudioClip clipSlowOff;
        private AudioClip clipStart;
        private AudioClip clipSection;
        private AudioClip clipCrumble;
        private AudioClip clipSlam;

        private bool paused;
        private bool slowed;

        private void Awake()
        {
            I = this;

            clipJump = Tone("jump", 0.14f, 330f, 660f, 0.55f, 0.25f, 0.006f, 0.06f);
            clipLand = Thump("land", 0.2f, 90f, 0.65f);
            clipCheckpoint = Arpeggio("checkpoint", new[] { 659.25f, 987.77f, 1318.51f }, 0.11f, 0.5f);
            clipFail = FailTone();
            clipWin = Arpeggio("win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.17f, 0.55f);
            clipClick = Tone("click", 0.045f, 1500f, 1250f, 0.25f, 0f, 0.003f, 0.03f);
            clipSlowOn = Tone("slow_on", 0.34f, 650f, 150f, 0.35f, 0f, 0.01f, 0.2f);
            clipSlowOff = Tone("slow_off", 0.28f, 180f, 640f, 0.3f, 0f, 0.01f, 0.16f);
            clipStart = Arpeggio("start", new[] { 220f, 329.63f, 440f }, 0.13f, 0.45f);
            clipSection = Arpeggio("section", new[] { 440f, 659.25f }, 0.1f, 0.28f);
            clipCrumble = Crumble();
            clipSlam = Slam();

            var pad = AmbientPad();

            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.clip = pad;
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.spatialBlend = 0f;
            ambientSource.volume = 0.14f;
            ambientSource.Play();

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
        }

        private void Update()
        {
            if (ambientSource == null) return;
            float targetPitch = paused ? 0.72f : (slowed ? 0.58f : 1f);
            float targetVolume = paused ? 0.06f : 0.14f;
            float k = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
            ambientSource.pitch = Mathf.Lerp(ambientSource.pitch, targetPitch, k);
            ambientSource.volume = Mathf.Lerp(ambientSource.volume, targetVolume, k);
        }

        public void SetPaused(bool value)
        {
            paused = value;
        }

        public void SetSlow(bool value)
        {
            slowed = value;
        }

        private void Play(AudioClip clip, float volume = 0.8f)
        {
            if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayJump() => Play(clipJump, Random.Range(0.4f, 0.55f));
        public void PlayLand() => Play(clipLand, 0.5f);
        public void PlayCheckpoint() => Play(clipCheckpoint, 0.7f);
        public void PlayFail() => Play(clipFail, 0.8f);
        public void PlayWin() => Play(clipWin, 0.85f);
        public void PlayClick() => Play(clipClick, 0.4f);
        public void PlaySlowOn() => Play(clipSlowOn, 0.5f);
        public void PlaySlowOff() => Play(clipSlowOff, 0.4f);
        public void PlayStart() => Play(clipStart, 0.6f);
        public void PlaySection() => Play(clipSection, 0.5f);
        public void PlayCrumble() => Play(clipCrumble, 0.35f);
        public void PlaySlam() => Play(clipSlam, 0.9f);

        // ---------- синтез ----------

        private static AudioClip Tone(string name, float duration, float f0, float f1, float volume, float squareMix, float attack, float release)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / duration;
                float freq = Mathf.Lerp(f0, f1, p);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float sine = Mathf.Sin(phase);
                float square = Mathf.Sign(sine) * 0.35f;
                float env = Mathf.Clamp01(t / Mathf.Max(0.0001f, attack)) * Mathf.Clamp01((duration - t) / Mathf.Max(0.0001f, release));
                data[i] = (sine * (1f - squareMix) + square * squareMix) * env * volume;
            }
            return Make(name, data);
        }

        private static AudioClip Thump(string name, float duration, float frequency, float volume)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            float phase = 0f;
            float noise = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / duration;
                float freq = Mathf.Lerp(frequency, frequency * 0.5f, p);
                phase += 2f * Mathf.PI * freq / SampleRate;
                noise = Mathf.Lerp(noise, Random.Range(-1f, 1f), 0.18f);
                float env = Mathf.Exp(-9f * p);
                data[i] = (Mathf.Sin(phase) * 0.8f + noise * 0.45f) * env * volume;
            }
            return Make(name, data);
        }

        private static AudioClip FailTone()
        {
            float duration = 0.65f;
            int count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            float phase = 0f;
            float noise = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / duration;
                float freq = Mathf.Lerp(280f, 55f, p * p);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float saw = 2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f));
                noise = Mathf.Lerp(noise, Random.Range(-1f, 1f), 0.08f);
                float env = Mathf.Clamp01((duration - t) / 0.3f);
                data[i] = (saw * 0.55f + noise * 0.35f) * env * 0.7f;
            }
            return Make("fail", data);
        }

        private static AudioClip Crumble()
        {
            float duration = 0.3f;
            int count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            float noise = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / duration;
                noise = Mathf.Lerp(noise, Random.Range(-1f, 1f), 0.35f);
                float env = Mathf.Exp(-6f * p);
                data[i] = (noise * 0.7f + Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.4f) * env * 0.45f;
            }
            return Make("crumble", data);
        }

        private static AudioClip Slam()
        {
            float duration = 0.42f;
            int count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            float phase = 0f;
            float noise = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / duration;
                float freq = Mathf.Lerp(92f, 38f, p * p);
                phase += 2f * Mathf.PI * freq / SampleRate;
                noise = Mathf.Lerp(noise, Random.Range(-1f, 1f), 0.22f);
                float env = Mathf.Exp(-7f * p);
                data[i] = (Mathf.Sin(phase) * 0.9f + noise * 0.55f) * env * 0.9f;
            }
            return Make("slam", data);
        }

        private static AudioClip Arpeggio(string name, float[] frequencies, float noteDuration, float volume)
        {
            float duration = noteDuration * frequencies.Length + 0.4f;
            int count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            for (int n = 0; n < frequencies.Length; n++)
            {
                float start = n * noteDuration * 0.82f;
                int startSample = Mathf.FloorToInt(start * SampleRate);
                int noteSamples = Mathf.CeilToInt(noteDuration * 2.6f * SampleRate);
                for (int i = 0; i < noteSamples && startSample + i < count; i++)
                {
                    float t = i / (float)SampleRate;
                    float p = i / (float)noteSamples;
                    float env = Mathf.Clamp01(t / 0.008f) * Mathf.Exp(-5.5f * p);
                    float s = Mathf.Sin(2f * Mathf.PI * frequencies[n] * t)
                              + 0.28f * Mathf.Sin(2f * Mathf.PI * frequencies[n] * 2f * t)
                              + 0.12f * Mathf.Sin(2f * Mathf.PI * frequencies[n] * 3f * t);
                    data[startSample + i] += s * env * volume * 0.45f;
                }
            }
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            return Make(name, data);
        }

        private static AudioClip AmbientPad()
        {
            const float duration = 8f;
            int count = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[count];
            float[] freqs = { 55f, 82.5f, 110f, 165f };
            float[] amps = { 0.5f, 0.28f, 0.2f, 0.08f };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float sum = 0f;
                for (int k = 0; k < freqs.Length; k++)
                {
                    float lfo = 1f + 0.18f * Mathf.Sin(2f * Mathf.PI * (0.125f + k * 0.125f) * t + k);
                    sum += Mathf.Sin(2f * Mathf.PI * freqs[k] * t + Mathf.Sin(2f * Mathf.PI * 0.25f * t) * 0.12f) * amps[k] * lfo;
                }
                data[i] = sum * 0.12f;
            }
            return Make("ambient_pad", data);
        }

        private static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
