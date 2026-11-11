using UnityEngine;

namespace BenchFlow
{
    public sealed class HologramPulse : MonoBehaviour
    {
        public Color glowColor = new Color(0.05f, 0.62f, 1f);
        public float pulseSpeed = 2.8f;
        public float baseSize = 0.008f;
        public float pulseSize = 0.0008f;
        Renderer targetRenderer;
        Material runtimeMaterial;

        void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null) return;
            runtimeMaterial = targetRenderer.material;
            runtimeMaterial.EnableKeyword("_EMISSION");
        }

        void Update()
        {
            if (targetRenderer == null) return;
            float amount = 1.4f + (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.65f;
            runtimeMaterial.SetColor("_EmissionColor", glowColor * amount);
            transform.localScale = Vector3.one * (baseSize + Mathf.Sin(Time.time * pulseSpeed) * pulseSize);
        }

        void OnDestroy() { if (runtimeMaterial != null) Destroy(runtimeMaterial); }
    }
}
