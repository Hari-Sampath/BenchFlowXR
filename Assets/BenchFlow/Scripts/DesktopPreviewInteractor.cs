using System.Collections.Generic;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>Editor-only mouse/keyboard harness for testing circuit state before the Quest adapter.</summary>
    public sealed class DesktopPreviewInteractor : MonoBehaviour
    {
        public Camera previewCamera;
        public BreadboardGrid breadboard;
        public WorkbenchState workbench;
        public TutorialController tutorial;
        public CircuitPart resistor;
        public CircuitPart led;
        public Transform unoD13;
        public Transform unoGnd;
        public string status = "Drag R/LED onto the board • 1/2/3 add preview wires • Space verifies";

        CircuitPart heldPart;
        readonly List<LineRenderer> wireLines = new List<LineRenderer>();

        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0)) BeginMouseGrab();
            if (heldPart != null)
            {
                MoveHeldPart();
                if (Input.GetMouseButtonUp(0)) EndMouseGrab();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                CircuitCheck result = tutorial.VerifyStep();
                SetStatus(result.message);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1)) Connect("UNO:D13", "RESISTOR:A", Color.red);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Connect("RESISTOR:B", "LED:ANODE", new Color(1f, 0.72f, 0.12f));
            if (Input.GetKeyDown(KeyCode.Alpha3)) Connect("LED:CATHODE", "UNO:GND", Color.black);
#endif
        }

        void BeginMouseGrab()
        {
            if (previewCamera == null || !Physics.Raycast(previewCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 10f)) return;
            heldPart = hit.collider.GetComponentInParent<CircuitPart>();
            if (heldPart != null) workbench.BeginGrab(heldPart, new Pose(heldPart.transform.position, heldPart.transform.rotation));
        }

        void MoveHeldPart()
        {
            if (previewCamera == null || breadboard == null) return;
            Ray ray = previewCamera.ScreenPointToRay(Input.mousePosition);
            Plane boardPlane = new Plane(Vector3.up, breadboard.transform.position + Vector3.up * 0.025f);
            if (boardPlane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);
                Vector3 posePosition = new Vector3(point.x, breadboard.HolePosition(new HoleAddress(0, 0, BreadboardSide.Left, BreadboardRail.None)).y, point.z);
                workbench.UpdateGrab(heldPart, new Pose(posePosition, heldPart.transform.rotation));
            }
        }

        void EndMouseGrab()
        {
            workbench.EndGrab(heldPart, heldPart.transform.position);
            SetStatus(heldPart.isSnapped ? heldPart.componentId + " snapped to " + heldPart.firstTerminalHole : "Release over the breadboard to snap the part.");
            heldPart = null;
        }

        void Connect(string a, string b, Color color)
        {
            if (workbench == null || resistor == null || led == null) return;
            workbench.ConnectWire(a, b);
            DrawWire(a, b, color);
            SetStatus("Preview wire added: " + a + " ↔ " + b);
        }

        void SetStatus(string message)
        {
            status = message;
            if (tutorial != null) tutorial.previewStatus = message;
        }

        void DrawWire(string a, string b, Color color)
        {
            Vector3 start = TerminalPosition(a);
            Vector3 end = TerminalPosition(b);
            GameObject wire = new GameObject("Preview Wire " + (wireLines.Count + 1));
            LineRenderer line = wire.AddComponent<LineRenderer>();
            line.positionCount = 3;
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = 0.004f;
            line.material = new Material(Shader.Find("Sprites/Default")) { color = color };
            line.startColor = line.endColor = color;
            line.SetPosition(0, start);
            line.SetPosition(1, (start + end) * 0.5f + Vector3.up * 0.08f);
            line.SetPosition(2, end);
            wireLines.Add(line);
        }

        Vector3 TerminalPosition(string node)
        {
            switch (node)
            {
                case "UNO:D13": return unoD13.position;
                case "UNO:GND": return unoGnd.position;
                case "RESISTOR:A": return resistor.transform.TransformPoint(resistor.terminalOffsetA);
                case "RESISTOR:B": return resistor.transform.TransformPoint(resistor.terminalOffsetB);
                case "LED:ANODE": return led.transform.TransformPoint(led.terminalOffsetA);
                default: return led.transform.TransformPoint(led.terminalOffsetB);
            }
        }

    }
}
