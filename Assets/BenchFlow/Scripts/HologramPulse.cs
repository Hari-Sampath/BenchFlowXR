using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Smooth pulsing dark blue glow for breadboard hole sockets and guided pins.
    /// Keeps emission confined strictly to the hole aperture without blowing out the area.
    /// </summary>
    public sealed class HologramPulse : MonoBehaviour
    {
        // This source color is also assigned by TutorialController at runtime; darkBlueTint
        // keeps the rendered marker deep blue even when the scene/controller supplies cyan.
        public Color glowColor = new Color(0.04f, 0.22f, 0.85f, 1f);
        public Color darkBlueTint = new Color(0.375f, 0.41f, 0.706f, 1f);
        public float pulseSpeed = 2.2f;
        public float baseScale = 0.0022f; // Sized precisely to the breadboard hole
        public float pulseScaleRange = 0.0001f;
        public float blinkInterval = 3.2f;
        public float blinkDuration = 0.12f;
        public float blinkGap = 0.16f;
        public float blinkBoost = 1.8f;

        Renderer targetRenderer;
        Material runtimeMaterial;

        void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                runtimeMaterial = targetRenderer.material;
                if (runtimeMaterial != null)
                {
                    runtimeMaterial.EnableKeyword("_EMISSION");
                }
            }
        }

        void Update()
        {
            float sine = Mathf.Sin(Time.time * pulseSpeed);
            float scale = baseScale + sine * pulseScaleRange;
            transform.localScale = new Vector3(scale, transform.localScale.y, scale);

            if (runtimeMaterial != null)
            {
                // Keep the marker a deep blue between flashes, with a brief double flash for visibility.
                float intensity = 1.2f + (sine + 1f) * 0.15f;
                float interval = Mathf.Max(0.1f, blinkInterval);
                float duration = Mathf.Clamp(blinkDuration, 0.02f, interval * 0.2f);
                float phase = Mathf.Repeat(Time.time, interval);
                float flash = Flash(phase, 0f, duration)
                    + Flash(phase, duration + Mathf.Max(0f, blinkGap), duration);
                intensity += blinkBoost * flash;
                Color effectiveGlow = new Color(
                    glowColor.r * darkBlueTint.r,
                    glowColor.g * darkBlueTint.g,
                    glowColor.b * darkBlueTint.b,
                    glowColor.a);
                Color emitted = effectiveGlow * intensity;
                runtimeMaterial.SetColor("_EmissionColor", emitted);
                if (runtimeMaterial.HasProperty("_Color"))
                {
                    runtimeMaterial.SetColor("_Color", effectiveGlow);
                }
                if (runtimeMaterial.HasProperty("_BaseColor"))
                {
                    runtimeMaterial.SetColor("_BaseColor", effectiveGlow);
                }
            }
        }

        static float Flash(float phase, float start, float duration)
        {
            float progress = (phase - start) / duration;
            return progress < 0f || progress > 1f ? 0f : Mathf.Sin(Mathf.PI * progress);
        }

        void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
