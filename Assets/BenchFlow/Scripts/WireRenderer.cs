using System.Collections.Generic;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Renders a natural 3D curved jumper wire with animated electric current pulses.
    /// When live, luminous current orbs travel along the curve from anode to cathode / ground.
    /// </summary>
    public sealed class WireRenderer : MonoBehaviour
    {
        public string terminalA;
        public string terminalB;
        public Vector3 startPosition;
        public Vector3 endPosition;
        public Color wireColor = Color.red;
        public float archHeight = 0.055f;
        public int pointCount = 20;

        [Header("Current Flow Animation")]
        public float currentSpeed = 1.6f;
        public Color pulseColor = new Color(1f, 0.92f, 0.25f, 1f); // Glowing yellow electron pulse

        LineRenderer lineRenderer;
        bool isCurrentFlowing = false;
        readonly List<Transform> pulseBeads = new List<Transform>();
        const int PulseCount = 3;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.0028f;
            lineRenderer.endWidth = 0.0028f;
            lineRenderer.numCapVertices = 4;
            lineRenderer.positionCount = pointCount;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material mat = new Material(shader) { color = wireColor };
            lineRenderer.material = mat;
            lineRenderer.startColor = wireColor;
            lineRenderer.endColor = wireColor;

            CreateCurrentPulses();
        }

        void CreateCurrentPulses()
        {
            Shader pulseShader = Shader.Find("Sprites/Default");
            if (pulseShader == null) pulseShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (pulseShader == null) pulseShader = Shader.Find("Unlit/Color");

            Material pulseMat = new Material(pulseShader) { color = pulseColor };
            pulseMat.EnableKeyword("_EMISSION");
            pulseMat.SetColor("_EmissionColor", pulseColor * 2.5f);

            for (int i = 0; i < PulseCount; i++)
            {
                GameObject bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bead.name = $"CurrentPulse_{i}";
                bead.transform.SetParent(transform, true);
                bead.transform.localScale = Vector3.one * 0.0065f;
                Destroy(bead.GetComponent<Collider>());
                bead.GetComponent<Renderer>().sharedMaterial = pulseMat;
                bead.SetActive(false);
                pulseBeads.Add(bead.transform);
            }
        }

        public void Setup(string from, string to, Vector3 start, Vector3 end, Color color, float height = 0.055f)
        {
            terminalA = from;
            terminalB = to;
            startPosition = start;
            endPosition = end;
            wireColor = color;
            archHeight = height;

            if (lineRenderer != null)
            {
                lineRenderer.material.color = color;
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }

            UpdateCurve();
        }

        public void SetCurrentFlow(bool active)
        {
            isCurrentFlowing = active;
            foreach (var b in pulseBeads)
            {
                if (b != null) b.gameObject.SetActive(active);
            }

            if (!active && lineRenderer != null)
            {
                lineRenderer.startColor = wireColor;
                lineRenderer.endColor = wireColor;
            }
        }

        void Update()
        {
            if (!isCurrentFlowing) return;

            // Animate pulses travelling from start to end along the Bezier curve
            for (int i = 0; i < pulseBeads.Count; i++)
            {
                Transform bead = pulseBeads[i];
                if (bead == null) continue;

                float offset = i / (float)pulseBeads.Count;
                float t = Mathf.Repeat(Time.time * currentSpeed + offset, 1.0f);
                bead.position = EvaluateBezier(t);
            }
        }

        public Vector3 EvaluateBezier(float t)
        {
            Vector3 mid = (startPosition + endPosition) * 0.5f + Vector3.up * archHeight;
            // Quadratic Bezier: (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
            return (1f - t) * (1f - t) * startPosition + 2f * (1f - t) * t * mid + t * t * endPosition;
        }

        public void UpdateCurve()
        {
            if (lineRenderer == null) return;
            lineRenderer.positionCount = pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                lineRenderer.SetPosition(i, EvaluateBezier(t));
            }
        }
    }
}
