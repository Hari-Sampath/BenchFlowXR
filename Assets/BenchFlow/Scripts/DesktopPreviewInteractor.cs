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
                CircuitPart[] allParts = FindObjectsByType<CircuitPart>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
            if (Input.GetKeyDown(KeyCode.Z))
            {
                bool commandOrControl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                                        Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
                if (workbench != null)
                {
                    if (commandOrControl) workbench.UndoLastWire();
                    else workbench.ResetWorkbench();
                }
            }
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            {
                if (workbench != null) workbench.ClearWires();
            }
        }

        void HandleLeftClick()
        {
            if (previewCamera == null) return;
            Ray ray = previewCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                if (tutorial != null && tutorial.currentStep == 2 && TrySelectWireEndpoint(hit))
                    return;

                // 1. Check for physical 3D button actuator (WorkbenchButton)
                WorkbenchButton wbBtn = hit.collider.GetComponentInParent<WorkbenchButton>();
                if (wbBtn != null)
                {
                    wbBtn.Press();
                    return;
                }

                // Fallback button checks: matches cap, base, or children
                if (hit.collider.name.IndexOf("Undo", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (tutorial != null && tutorial.undoButtonObject != null && (hit.collider.gameObject == tutorial.undoButtonObject || hit.collider.transform.IsChildOf(tutorial.undoButtonObject.transform))))
                {
                    WorkbenchButton b = tutorial != null && tutorial.undoButtonObject != null ? tutorial.undoButtonObject.GetComponentInChildren<WorkbenchButton>() : null;
                    if (b != null) b.Press();
                    else if (workbench != null) workbench.UndoLastWire();
                    return;
                }

                if (hit.collider.name.IndexOf("Run", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    hit.collider.name.IndexOf("BtnCap", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (tutorial != null && tutorial.runButtonObject != null && (hit.collider.gameObject == tutorial.runButtonObject || hit.collider.transform.IsChildOf(tutorial.runButtonObject.transform))))
                {
                    WorkbenchButton b = tutorial != null && tutorial.runButtonObject != null ? tutorial.runButtonObject.GetComponentInChildren<WorkbenchButton>() : null;
                    if (b != null) b.Press();
                    else if (tutorial != null) tutorial.OnRunButtonPressed();
                    return;
                }

                if (hit.collider.name.IndexOf("Reset", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (tutorial != null && tutorial.resetButtonObject != null && (hit.collider.gameObject == tutorial.resetButtonObject || hit.collider.transform.IsChildOf(tutorial.resetButtonObject.transform))))
                {
                    WorkbenchButton b = tutorial != null && tutorial.resetButtonObject != null ? tutorial.resetButtonObject.GetComponentInChildren<WorkbenchButton>() : null;
                    if (b != null) b.Press();
                    else if (workbench != null) workbench.ResetWorkbench();
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

                // 3. Check for Circuit Part grab (ignore if locked in place)
                CircuitPart part = hit.collider.GetComponentInParent<CircuitPart>();
                if (part != null)
                {
                    if (part.isLocked)
                    {
                        if (tutorial != null)
                        {
                            tutorial.latestFeedback = $"{part.componentId} is locked in position.";
                            tutorial.UpdateStepUI();
                        }
                        return;
                    }
                    heldPart = part;
                    workbench.BeginGrab(heldPart, new Pose(heldPart.transform.position, heldPart.transform.rotation));
                }
            }
        }

        bool TrySelectWireEndpoint(RaycastHit hit)
        {
            if (workbench == null || breadboard == null) return false;
            string node = null;
            Vector3 point = hit.point;
            string colliderName = hit.collider.name;

            if (colliderName.Contains("13") || hit.collider.transform == unoD13)
                node = "UNO:D13";
            else if (colliderName.Contains("GND") || colliderName.Contains("gnd") || hit.collider.transform == unoGnd)
                node = "UNO:GND";
            else if (hit.collider.GetComponentInParent<BreadboardGrid>() != null &&
                     breadboard.TrySnap(hit.point, out HoleAddress hole, out Vector3 snapped))
            {
                node = breadboard.ElectricalNode(hole);
                point = snapped;
            }

            if (node == null) return false;
            workbench.SelectWireEndpoint(node, point);
            return true;
        }

        void MoveHeldPart()
        {
            if (previewCamera == null || breadboard == null || heldPart == null) return;
            Ray ray = previewCamera.ScreenPointToRay(Input.mousePosition);

            // Raycast against breadboard surface plane
            float tableY = breadboard.transform.position.y + breadboard.surfaceY * breadboard.transform.lossyScale.y;
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
            Vector3 start = unoD13 != null ? unoD13.position : new Vector3(-0.165f, 0.523f, -0.075f);
            Vector3 end = breadboard.HolePosition(breadboard.GetHole("8b"));
            workbench.ConnectWireVisual("UNO:D13", "strip:A:8", start, end, Color.red);
        }

        public void ConnectWireGndTo16a()
        {
            if (workbench == null || breadboard == null) return;
            Vector3 start = unoGnd != null ? unoGnd.position : new Vector3(-0.165f, 0.523f, -0.035f);
            Vector3 end = breadboard.HolePosition(breadboard.GetHole("16a"));
            workbench.ConnectWireVisual("strip:A:16", "UNO:GND", end, start, new Color(0.1f, 0.85f, 1f));
        }
    }
}
