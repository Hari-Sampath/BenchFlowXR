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
        [Min(1f)] public float wireEndpointPickRadiusPixels = 30f;

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

            // Wire targets are much smaller than a desktop pointer can reliably hit at
            // the workbench camera distance. Resolve the Arduino terminal in screen
            // space first, even when the pointer narrowly misses its physics collider.
            if (tutorial != null && tutorial.currentStep == 2 && TrySelectWireEndpoint(ray))
                return;

            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                // 1. Check for physical 3D button actuator (WorkbenchButton)
                WorkbenchButton wbBtn = hit.collider.GetComponentInParent<WorkbenchButton>();
                if (wbBtn != null)
                {
                    wbBtn.Press();
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

        bool TrySelectWireEndpoint(Ray ray)
        {
            if (workbench == null || breadboard == null) return false;

            if (TrySelectArduinoEndpointFromPointer(out string endpointNode, out Vector3 endpointPosition))
            {
                workbench.SelectWireEndpoint(endpointNode, endpointPosition);
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(ray, 10f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit candidate in hits)
            {
                Collider candidateCollider = candidate.collider;
                if (candidateCollider == null) continue;

                // Let the ordinary interaction handler activate a button cap.
                if (candidateCollider.GetComponentInParent<WorkbenchButton>() != null) return false;

                string node = null;
                Vector3 point = candidate.point;
                WireEndpointAnchor terminal = candidateCollider.GetComponentInParent<WireEndpointAnchor>();

                if (terminal != null)
                {
                    node = terminal.nodeId;
                    point = terminal.WorldPosition;
                }
                else if (candidateCollider.GetComponentInParent<BreadboardGrid>() != null &&
                         breadboard.TrySnap(candidate.point, out HoleAddress hole, out _))
                {
                    node = breadboard.ElectricalNode(hole);
                    point = breadboard.HoleConnectionPosition(hole);
                }

                if (node != null)
                {
                    workbench.SelectWireEndpoint(node, point);
                    return true;
                }

                // Locked components can cover the target hole visually and physically.
                // Continue down the ray so the board grid beneath them remains selectable.
                if (candidateCollider.GetComponentInParent<CircuitPart>() != null) continue;

                // The Arduino body collider covers its pin header. Keep scanning so the
                // dedicated D13/GND target behind it can be selected.
                if (candidateCollider.GetComponentInParent<BreadboardGrid>() != null) return false;
            }
            return false;
        }

        bool TrySelectArduinoEndpointFromPointer(out string node, out Vector3 worldPosition)
        {
            node = null;
            worldPosition = default;
            if (previewCamera == null) return false;

            Vector2 pointerPosition = Input.mousePosition;
            WireEndpointAnchor[] anchors = FindObjectsByType<WireEndpointAnchor>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            float bestDistance = wireEndpointPickRadiusPixels;
            WireEndpointAnchor bestAnchor = null;
            foreach (WireEndpointAnchor anchor in anchors)
            {
                if (anchor == null || (anchor.nodeId != "UNO:D13" && anchor.nodeId != "UNO:GND"))
                    continue;

                Vector3 screenPosition = previewCamera.WorldToScreenPoint(anchor.WorldPosition);
                if (screenPosition.z <= 0f) continue;

                float distance = Vector2.Distance(pointerPosition, new Vector2(screenPosition.x, screenPosition.y));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestAnchor = anchor;
                }
            }

            if (bestAnchor == null) return false;
            node = bestAnchor.nodeId;
            worldPosition = bestAnchor.WorldPosition;
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
            Vector3 end = breadboard.HoleConnectionPosition(breadboard.GetHole("8b"));
            workbench.ConnectWireVisual("UNO:D13", "strip:A:8", start, end, Color.red);
        }

        public void ConnectWireGndTo16a()
        {
            if (workbench == null || breadboard == null) return;
            Vector3 start = unoGnd != null ? unoGnd.position : new Vector3(-0.165f, 0.523f, -0.035f);
            Vector3 end = breadboard.HoleConnectionPosition(breadboard.GetHole("16a"));
            workbench.ConnectWireVisual("strip:A:16", "UNO:GND", end, start, new Color(0.1f, 0.85f, 1f));
        }
    }
}
