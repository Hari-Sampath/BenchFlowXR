using System;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Procedural 3D Spatial Audio synthesizer for BenchFlow XR.
    /// Generates high-quality tactile clicks, wire plug sounds, success chimes,
    /// and blink ticks in pure C# with zero external audio assets.
    /// </summary>
    public sealed class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        AudioSource audioSource;
        AudioClip snapClip;
        AudioClip wireClip;
        AudioClip successClip;
        AudioClip buttonClip;
        AudioClip errorClip;
        AudioClip tickClip;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 0.85f; // 3D spatialized
            audioSource.playOnAwake = false;

            GenerateClips();
        }

        void GenerateClips()
        {
            snapClip = CreateSnapSound();
            wireClip = CreateWireSound();
            successClip = CreateSuccessChime();
            buttonClip = CreateButtonClickSound();
            errorClip = CreateErrorSound();
            tickClip = CreateTickSound();
        }

        public void PlaySnap(Vector3 position) => PlayAt(snapClip, position, 0.72f, UnityEngine.Random.Range(0.96f, 1.04f));
        public void PlayWire(Vector3 position) => PlayAt(wireClip, position, 0.68f, UnityEngine.Random.Range(0.97f, 1.03f));
        public void PlaySuccess(Vector3 position) => PlayAt(successClip, position, 0.85f, 1.0f);
        public void PlayButton(Vector3 position) => PlayAt(buttonClip, position, 0.6f, 1.0f);
        public void PlayError(Vector3 position) => PlayAt(errorClip, position, 0.55f, 0.9f);
        public void PlayTick(Vector3 position) => PlayAt(tickClip, position, 0.4f, 1.1f);

        void PlayAt(AudioClip clip, Vector3 pos, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            GameObject temp = new GameObject("OneShotAudio");
            temp.transform.position = pos;
            AudioSource a = temp.AddComponent<AudioSource>();
            a.clip = clip;
            a.volume = volume;
            a.pitch = pitch;
            a.spatialBlend = 0.85f;
            a.minDistance = 0.1f;
            a.maxDistance = 4f;
            a.rolloffMode = AudioRolloffMode.Logarithmic;
            a.dopplerLevel = 0f;
            a.Play();
            Destroy(temp, clip.length + 0.1f);
        }

        AudioClip CreateSnapSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.085f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float tick = Mathf.Exp(-t * 520f);
                float body = Mathf.Exp(-t * 68f);
                float noise = UnityEngine.Random.value * 2f - 1f;
                float click = noise * tick * 0.26f;
                float resonance = Mathf.Sin(2f * Mathf.PI * 1180f * t) * body * 0.33f +
                                  Mathf.Sin(2f * Mathf.PI * 620f * t) * Mathf.Exp(-t * 105f) * 0.22f;
                samples[i] = Mathf.Clamp(click + resonance, -0.8f, 0.8f);
            }
            AudioClip clip = AudioClip.Create("ComponentSeatClick", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateWireSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.14f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float first = Mathf.Exp(-Mathf.Abs(t - 0.008f) * 420f);
                float second = Mathf.Exp(-Mathf.Abs(t - 0.052f) * 360f);
                float noise = UnityEngine.Random.value * 2f - 1f;
                float connectorClicks = noise * (first * 0.18f + second * 0.16f);
                float firstResonance = Mathf.Sin(2f * Mathf.PI * 1680f * t) * first * 0.25f +
                                       Mathf.Sin(2f * Mathf.PI * 980f * t) * first * 0.12f;
                float secondResonance = Mathf.Sin(2f * Mathf.PI * 1320f * t) * second * 0.24f +
                                        Mathf.Sin(2f * Mathf.PI * 760f * t) * second * 0.11f;
                float softCableTone = Mathf.Sin(2f * Mathf.PI * 420f * t) * Mathf.Exp(-t * 30f) * 0.06f;
                samples[i] = Mathf.Clamp(connectorClicks + firstResonance + secondResonance + softCableTone, -0.8f, 0.8f);
            }
            AudioClip clip = AudioClip.Create("JumperWireConnect", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateSuccessChime()
        {
            // 3-note ascending arpeggio: C5 (523Hz), E5 (659Hz), G5 (784Hz)
            int sampleRate = 44100;
            float noteDuration = 0.12f;
            int length = (int)(sampleRate * (noteDuration * 3f + 0.4f));
            float[] samples = new float[length];
            float[] freqs = { 523.25f, 659.25f, 783.99f };

            for (int n = 0; n < 3; n++)
            {
                int startIdx = (int)(n * noteDuration * sampleRate);
                float f = freqs[n];
                for (int i = startIdx; i < length; i++)
                {
                    float t = (i - startIdx) / (float)sampleRate;
                    float env = Mathf.Exp(-t * 6f);
                    float wave = Mathf.Sin(2f * Mathf.PI * f * t) + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.35f;
                    samples[i] += wave * env * 0.28f;
                }
            }
            AudioClip clip = AudioClip.Create("SuccessChime", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateButtonClickSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.04f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * 120f);
                float wave = Mathf.Sin(2f * Mathf.PI * 600f * t) + (UnityEngine.Random.value * 2f - 1f) * 0.2f;
                samples[i] = wave * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("ButtonClick", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateErrorSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.15f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * 20f);
                float wave = Mathf.Sin(2f * Mathf.PI * 180f * t);
                samples[i] = wave * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("ErrorBuzz", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateTickSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.025f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float env = Mathf.Exp(-t * 160f);
                float wave = Mathf.Sin(2f * Mathf.PI * 1400f * t);
                samples[i] = wave * env * 0.35f;
            }
            AudioClip clip = AudioClip.Create("BlinkTick", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
