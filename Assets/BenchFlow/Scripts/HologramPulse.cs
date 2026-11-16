using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Smooth pulsing glow and scale animation for tutorial holograms, target holes, and guided paths.
    /// </summary>
    public sealed class HologramPulse : MonoBehaviour
    {
        public Color glowColor = new Color(0.10f, 0.75f, 1f, 1f);
        public float pulseSpeed = 3.5f;
        public float baseScale = 0.007f;
        public float pulseScaleRange = 0.002f;

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
            transform.localScale = Vector3.one * scale;

            if (runtimeMaterial != null)
            {
                float intensity = 3.0f + (sine + 1f) * 2.5f;
                Color emitted = glowColor * intensity;
                runtimeMaterial.SetColor("_EmissionColor", emitted);
                if (runtimeMaterial.HasProperty("_Color"))
                {
                    Color baseCol = glowColor;
                    baseCol.a = 0.9f;
                    runtimeMaterial.SetColor("_Color", baseCol);
                }
            }
        }

        void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
