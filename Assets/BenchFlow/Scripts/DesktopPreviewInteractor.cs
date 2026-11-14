using System.Collections.Generic;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Complete desktop mouse and keyboard interaction harness.
    /// Allows full testing of grab, rotate, placement, snapping, wires, and verification
    /// directly in the Unity Editor or non-VR standalone mode.
    /// </summary>
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

        CircuitPart heldPart;

        void Awake()
        {
            if (previewCamera == null) previewCamera = Camera.main;
            if (breadboard == null) breadboard = FindAnyObjectByType<BreadboardGrid>();
            if (workbench == null) workbench = FindAnyObjectByType<WorkbenchState>();
            if (tutorial == null) tutorial = FindAnyObjectByType<TutorialController>();

            if (resistor == null || led == null)
            {
                CircuitPart[] allParts = FindObjectsByType<CircuitPart>();
                foreach (var p in allParts)
                {
                    if (p.componentId == "RESISTOR" && resistor == null) resistor = p;
                    else if (p.componentId == "LED" && led == null) led = p;
                }
            }

            if (unoD13 == null)
            {
                GameObject p13 = GameObject.Find("UNO-compatible digital pin 13") ?? GameObject.Find("UNO Digital Pin 13");
                if (p13 != null) unoD13 = p13.transform;
            }
            if (unoGnd == null)
            {
                GameObject pgnd = GameObject.Find("UNO-compatible GND") ?? GameObject.Find("UNO GND");
                if (pgnd != null) unoGnd = pgnd.transform;
            }
        }

        void Update()
        {
            // Mouse Grab & Drag
            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }

            if (heldPart != null)
            {
                MoveHeldPart();

                // Rotate with R key or ScrollWheel
                if (Input.GetKeyDown(KeyCode.R) || Mathf.Abs(Input.mouseScrollDelta.y) > 0.4f)
                {
                    heldPart.Rotate90();
                }

                if (Input.GetMouseButtonUp(0))
                {
                    EndMouseGrab();
                }
            }

            // Keyboard Shortcuts for quick wiring & testing
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ConnectWireD13To8b();
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ConnectWireGndTo16a();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (tutorial != null) tutorial.OnRunButtonPressed();
            }
        }

        void HandleLeftClick()
        {
            if (previewCamera == null) return;
            Ray ray = previewCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                // 1. Check for RUN button
                if (hit.collider.name.Contains("Run") || (tutorial != null && hit.collider.gameObject == tutorial.runButtonObject))
                {
                    tutorial.OnRunButtonPressed();
                    return;
                }

                // 2. Check for Pin 13 / GND
                if (hit.collider.name.Contains("13") || hit.collider.transform == unoD13)
                {
                    ConnectWireD13To8b();
                    return;
                }
                if (hit.collider.name.Contains("GND") || hit.collider.name.Contains("gnd") || hit.collider.transform == unoGnd)
                {
                    ConnectWireGndTo16a();
                    return;
                }

                // 3. Check for Circuit Part grab
                CircuitPart part = hit.collider.GetComponentInParent<CircuitPart>();
                if (part != null)
                {
                    heldPart = part;
                    workbench.BeginGrab(heldPart, new Pose(heldPart.transform.position, heldPart.transform.rotation));
                }
            }
        }

        void MoveHeldPart()
        {
            if (previewCamera == null || breadboard == null || heldPart == null) return;
            Ray ray = previewCamera.ScreenPointToRay(Input.mousePosition);

            // Raycast against breadboard surface plane
            float tableY = breadboard.transform.position.y + breadboard.surfaceY;
            Plane boardPlane = new Plane(Vector3.up, new Vector3(0f, tableY, 0f));

            if (boardPlane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);
                workbench.UpdateGrab(heldPart, new Pose(point, heldPart.transform.rotation));
            }
        }

        void EndMouseGrab()
        {
            if (heldPart == null) return;
            workbench.EndGrab(heldPart, heldPart.transform.position);
            heldPart = null;
        }

        public void ConnectWireD13To8b()
        {
            if (workbench == null || breadboard == null) return;
            Vector3 start = unoD13 != null ? unoD13.position : new Vector3(-0.102f, 0.523f, -0.005f);
            Vector3 end = breadboard.HolePosition(breadboard.GetHole("8b"));
            workbench.ConnectWireVisual("UNO:D13", "strip:A:8", start, end, Color.red);
        }

        public void ConnectWireGndTo16a()
        {
            if (workbench == null || breadboard == null) return;
            Vector3 start = unoGnd != null ? unoGnd.position : new Vector3(-0.102f, 0.523f, 0.025f);
            Vector3 end = breadboard.HolePosition(breadboard.GetHole("16a"));
            workbench.ConnectWireVisual("strip:A:16", "UNO:GND", end, start, new Color(0.1f, 0.85f, 1f));
        }
    }
}
