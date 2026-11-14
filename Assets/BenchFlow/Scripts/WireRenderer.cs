using System.Collections.Generic;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Renders a natural 3D curved jumper wire between two connection endpoints.
    /// Can animate current flow pulses when the circuit is live.
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

        LineRenderer lineRenderer;
        bool isCurrentFlowing = false;
        float currentPulseTime = 0f;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.0035f;
            lineRenderer.endWidth = 0.0035f;
            lineRenderer.positionCount = pointCount;

            // Simple unlit sprite or mobile diffuse material for high VR performance
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material mat = new Material(shader) { color = wireColor };
            lineRenderer.material = mat;
            lineRenderer.startColor = wireColor;
            lineRenderer.endColor = wireColor;
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
            if (!active && lineRenderer != null)
            {
                lineRenderer.startColor = wireColor;
                lineRenderer.endColor = wireColor;
            }
        }

        void Update()
        {
            if (isCurrentFlowing && lineRenderer != null)
            {
                currentPulseTime += Time.deltaTime * 4f;
                float pulse = 0.7f + 0.3f * Mathf.Sin(currentPulseTime);
                Color pulsed = wireColor * pulse;
                pulsed.a = 1f;
                lineRenderer.startColor = pulsed;
                lineRenderer.endColor = pulsed;
            }
        }

        public void UpdateCurve()
        {
            if (lineRenderer == null) return;
            lineRenderer.positionCount = pointCount;

            // Quadratic / cubic Bezier arc
            Vector3 mid = (startPosition + endPosition) * 0.5f;
            mid.y += archHeight;

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                // Quadratic bezier: (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
                Vector3 p = (1f - t) * (1f - t) * startPosition + 2f * (1f - t) * t * mid + t * t * endPosition;
                lineRenderer.SetPosition(i, p);
            }
        }
    }
}

