using System.Collections.Generic;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Animates the Arduino Blink loop execution when the circuit is verified and running.
    /// Blinks the real circuit LED, drives built-in D13 indicator, and traces current flow.
    /// </summary>
    public sealed class BlinkAnimator : MonoBehaviour
    {
        [Header("LED Components")]
        public Renderer ledRenderer;
        public Light ledLight;
        public Material ledLitMaterial;
        public Material ledOffMaterial;

        [Header("Arduino D13 Indicator")]
        public Renderer unoD13LedRenderer;
        public Material unoLedLitMaterial;
        public Material unoLedOffMaterial;

        [Header("Timing")]
        public float blinkInterval = 0.5f; // 500 ms high, 500 ms low

        [Header("State")]
        public bool isRunning = false;
        public bool isHigh = false;

        float timer = 0f;
        readonly List<WireRenderer> wires = new List<WireRenderer>();

        public void RegisterWire(WireRenderer wire)
        {
            if (wire != null && !wires.Contains(wire)) wires.Add(wire);
        }

        public void StartBlinking()
        {
            isRunning = true;
            timer = 0f;
            SetLedState(true);
            foreach (var w in wires) if (w != null) w.SetCurrentFlow(true);
        }

        public void StopBlinking()
        {
            isRunning = false;
            SetLedState(false);
            foreach (var w in wires) if (w != null) w.SetCurrentFlow(false);
        }

        void Update()
        {
            if (!isRunning) return;

            timer += Time.deltaTime;
            if (timer >= blinkInterval)
            {
                timer -= blinkInterval;
                SetLedState(!isHigh);
            }
        }

        void SetLedState(bool high)
        {
            isHigh = high;

            // Breadboard LED
            if (ledRenderer != null)
            {
                if (high && ledLitMaterial != null) ledRenderer.sharedMaterial = ledLitMaterial;
                else if (!high && ledOffMaterial != null) ledRenderer.sharedMaterial = ledOffMaterial;
            }

            if (ledLight != null)
            {
                ledLight.enabled = high;
            }

            // Arduino D13 Built-in LED
            if (unoD13LedRenderer != null)
            {
                if (high && unoLedLitMaterial != null) unoD13LedRenderer.sharedMaterial = unoLedLitMaterial;
                else if (!high && unoLedOffMaterial != null) unoD13LedRenderer.sharedMaterial = unoLedOffMaterial;
            }

            if (high && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayTick(transform.position);
            }
        }
    }
}

