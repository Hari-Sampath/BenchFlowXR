using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Smooth pulsing dark blue glow for breadboard hole sockets and guided pins.
    /// Keeps emission confined strictly to the hole aperture without blowing out the area.
    /// </summary>
    public sealed class HologramPulse : MonoBehaviour
    {
        public Color glowColor = new Color(0.04f, 0.22f, 0.85f, 1f); // Dark saturated royal blue
        public float pulseSpeed = 2.2f;
        public float baseScale = 0.0022f; // Sized precisely to the breadboard hole
        public float pulseScaleRange = 0.0001f;

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
                // Subtle dark blue glow without white blowout or lighting up the surrounding board
                float intensity = 1.2f + (sine + 1f) * 0.3f;
                Color emitted = glowColor * intensity;
                runtimeMaterial.SetColor("_EmissionColor", emitted);
                if (runtimeMaterial.HasProperty("_Color"))
                {
                    runtimeMaterial.SetColor("_Color", glowColor);
                }
                if (runtimeMaterial.HasProperty("_BaseColor"))
                {
                    runtimeMaterial.SetColor("_BaseColor", glowColor);
                }
            }
        }

        void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
