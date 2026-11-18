using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace BenchFlow
{
    /// <summary>
    /// VR 6-DOF Hand / Controller Interactor.
    /// Supports raycast pointing, direct & distant grab, thumbstick rotation,
    /// breadboard snapping, wire placement, and haptic feedback.
    /// Compatible with Meta Quest, OpenXR, and standard XR controllers.
    /// </summary>
    public sealed class VRHandInteractor : MonoBehaviour
    {
        public XRNode handNode = XRNode.RightHand;
        public WorkbenchState workbench;
        public BreadboardGrid breadboard;
        public TutorialController tutorial;
        public float rayDistance = 3.0f;
        public LayerMask interactionMask = ~0;

        [Header("Visuals")]
        public LineRenderer pointerRay;
        public Transform reticle;
        public Color normalRayColor = new Color(0.2f, 0.7f, 1f, 0.6f);
        public Color hoverRayColor = new Color(0.1f, 1f, 0.5f, 0.9f);

        // State
        InputDevice device;
        CircuitPart heldPart;
        bool prevTrigger = false;
        bool prevGrip = false;
        float rotateCooldown = 0f;

        void Start()
        {
            SetupVisuals();
        }

        void SetupVisuals()
        {
            if (pointerRay == null)
            {
                pointerRay = gameObject.AddComponent<LineRenderer>();
                pointerRay.useWorldSpace = true;
                pointerRay.startWidth = 0.003f;
                pointerRay.endWidth = 0.001f;

                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");

                pointerRay.material = new Material(shader) { color = normalRayColor };
            }

            if (reticle == null)
            {
                GameObject retObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                retObj.name = "Reticle";
                Destroy(retObj.GetComponent<Collider>());
                retObj.transform.localScale = Vector3.one * 0.012f;
                Renderer rend = retObj.GetComponent<Renderer>();

                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");

                rend.material = new Material(shader) { color = hoverRayColor };
                reticle = retObj.transform;
                reticle.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (!device.isValid)
            {
                device = InputDevices.GetDeviceAtXRNode(handNode);
            }

            // Update hand pose from XR tracking
            if (device.isValid)
            {
                if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
                    transform.localPosition = pos;
                if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
                    transform.localRotation = rot;
            }

            // Read inputs
            bool triggerDown = false;
            bool gripDown = false;
            Vector2 thumbstick = Vector2.zero;

            if (device.isValid)
            {
                device.TryGetFeatureValue(CommonUsages.triggerButton, out bool curTrigger);
                if (!curTrigger && device.TryGetFeatureValue(CommonUsages.trigger, out float trigVal))
                    curTrigger = trigVal > 0.5f;
                triggerDown = curTrigger && !prevTrigger;
                prevTrigger = curTrigger;

                device.TryGetFeatureValue(CommonUsages.gripButton, out bool curGrip);
                if (!curGrip && device.TryGetFeatureValue(CommonUsages.grip, out float gripVal))
                    curGrip = gripVal > 0.5f;
                gripDown = curGrip && !prevGrip;
                prevGrip = curGrip;

                device.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick);
            }

            // Rotation with thumbstick while holding a part
            if (rotateCooldown > 0f) rotateCooldown -= Time.deltaTime;
            if (heldPart != null && rotateCooldown <= 0f && Mathf.Abs(thumbstick.x) > 0.6f)
            {
                heldPart.Rotate90();
                rotateCooldown = 0.35f;
                TriggerHaptic(0.2f, 0.05f);
            }

            // Secondary button (B or Y) triggers reset
            if (device.isValid && device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool btnSecondary) && btnSecondary)
            {
                if (workbench != null)
                {
                    workbench.ResetWorkbench();
                    TriggerHaptic(0.4f, 0.1f);
                }
            }

            // Raycasting & Interaction
            Ray ray = new Ray(transform.position, transform.forward);
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactionMask);

            Vector3 endPoint = hasHit ? hit.point : (ray.origin + ray.direction * rayDistance);
            UpdatePointerRay(ray.origin, endPoint, hasHit);

            if (reticle != null)
            {
                reticle.gameObject.SetActive(hasHit);
                if (hasHit) reticle.position = hit.point + hit.normal * 0.002f;
            }

            // Handle Grabbing
            if (heldPart != null)
            {
                // Update held position: follow hand ray at fixed distance or surface hit
                Vector3 targetPos = ray.origin + ray.direction * 0.35f;
                if (hasHit && hit.collider.GetComponentInParent<BreadboardGrid>() != null)
                {
                    targetPos = hit.point;
                }

                workbench.UpdateGrab(heldPart, new Pose(targetPos, heldPart.transform.rotation));

                // Release on trigger or grip release
                if (!prevTrigger && !prevGrip)
                {
                    ReleasePart(targetPos);
                }
            }
            else
            {
                // Look for grab candidates
                if ((triggerDown || gripDown) && hasHit)
                {
                    if (tutorial != null && tutorial.currentStep == 2 && TrySelectWireEndpoint(hit))
                        return;

                    // 1. Check for physical 3D button actuator (WorkbenchButton)
                    WorkbenchButton wbBtn = hit.collider.GetComponentInParent<WorkbenchButton>();
                    if (wbBtn != null)
                    {
                        wbBtn.Press();
                        TriggerHaptic(0.6f, 0.12f);
                        return;
                    }

                    // Fallback button checks: matches cap, base, or children
                    if (hit.collider.name.IndexOf("Undo", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (tutorial != null && tutorial.undoButtonObject != null && (hit.collider.gameObject == tutorial.undoButtonObject || hit.collider.transform.IsChildOf(tutorial.undoButtonObject.transform))))
                    {
                        WorkbenchButton b = tutorial != null && tutorial.undoButtonObject != null ? tutorial.undoButtonObject.GetComponentInChildren<WorkbenchButton>() : null;
                        if (b != null) b.Press();
                        else if (workbench != null) workbench.UndoLastWire();
                        TriggerHaptic(0.5f, 0.1f);
                        return;
                    }

                    if (hit.collider.name.IndexOf("Run", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        hit.collider.name.IndexOf("BtnCap", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (tutorial != null && tutorial.runButtonObject != null && (hit.collider.gameObject == tutorial.runButtonObject || hit.collider.transform.IsChildOf(tutorial.runButtonObject.transform))))
                    {
                        WorkbenchButton b = tutorial != null && tutorial.runButtonObject != null ? tutorial.runButtonObject.GetComponentInChildren<WorkbenchButton>() : null;
                        if (b != null) b.Press();
                        else if (tutorial != null) tutorial.OnRunButtonPressed();
                        TriggerHaptic(0.6f, 0.12f);
                        return;
                    }

                    if (hit.collider.name.IndexOf("Reset", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (tutorial != null && tutorial.resetButtonObject != null && (hit.collider.gameObject == tutorial.resetButtonObject || hit.collider.transform.IsChildOf(tutorial.resetButtonObject.transform))))
                    {
                        WorkbenchButton b = tutorial != null && tutorial.resetButtonObject != null ? tutorial.resetButtonObject.GetComponentInChildren<WorkbenchButton>() : null;
                        if (b != null) b.Press();
                        else if (tutorial != null) tutorial.OnResetButtonPressed();
                        TriggerHaptic(0.5f, 0.1f);
                        return;
                    }

                    // 2. Check for Circuit Part grab (ignore if locked in place)
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
                            TriggerHaptic(0.2f, 0.05f);
                            return;
                        }
                        GrabPart(part);
                        return;
                    }

                    // 3. Check for quick wire shortcuts on Pin 13 / GND
                    if (hit.collider.name.Contains("13") || hit.collider.name.Contains("GND"))
                    {
                        HandlePinTrigger(hit.collider.name, hit.point);
                    }
                }
            }
        }

        bool TrySelectWireEndpoint(RaycastHit hit)
        {
            if (workbench == null || breadboard == null) return false;
            string node = null;
            Vector3 point = hit.point;
            string colliderName = hit.collider.name;

            if (colliderName.Contains("13")) node = "UNO:D13";
            else if (colliderName.Contains("GND") || colliderName.Contains("gnd")) node = "UNO:GND";
            else if (hit.collider.GetComponentInParent<BreadboardGrid>() != null &&
                     breadboard.TrySnap(hit.point, out HoleAddress hole, out Vector3 snapped))
            {
                node = breadboard.ElectricalNode(hole);
                point = snapped;
            }

            if (node == null) return false;
            workbench.SelectWireEndpoint(node, point);
            TriggerHaptic(0.4f, 0.06f);
            return true;
        }

        void GrabPart(CircuitPart part)
        {
            heldPart = part;
            workbench.BeginGrab(heldPart, new Pose(part.transform.position, part.transform.rotation));
            TriggerHaptic(0.4f, 0.08f);
        }

        void ReleasePart(Vector3 releasePos)
        {
            if (heldPart == null) return;
            workbench.EndGrab(heldPart, releasePos);
            TriggerHaptic(0.6f, 0.12f);
            heldPart = null;
        }

        void HandlePinTrigger(string colliderName, Vector3 worldPos)
        {
            if (workbench == null || breadboard == null) return;

            if (colliderName.Contains("13"))
            {
                // Connect Pin 13 to row 8b
                Vector3 holePos = breadboard.HolePosition(breadboard.GetHole("8b"));
                workbench.ConnectWireVisual("UNO:D13", "strip:A:8", worldPos, holePos, Color.red);
                TriggerHaptic(0.5f, 0.1f);
            }
            else if (colliderName.Contains("GND") || colliderName.Contains("gnd"))
            {
                // Connect GND to row 16a
                Vector3 holePos = breadboard.HolePosition(breadboard.GetHole("16a"));
                workbench.ConnectWireVisual("strip:A:16", "UNO:GND", holePos, worldPos, new Color(0.1f, 0.85f, 1f));
                TriggerHaptic(0.5f, 0.1f);
            }
        }

        void UpdatePointerRay(Vector3 start, Vector3 end, bool hitInteractive)
        {
            if (pointerRay == null) return;
            pointerRay.SetPosition(0, start);
            pointerRay.SetPosition(1, end);
            Color col = hitInteractive ? hoverRayColor : normalRayColor;
            pointerRay.startColor = col;
            pointerRay.endColor = col;
        }

        public void TriggerHaptic(float amplitude = 0.5f, float duration = 0.1f)
        {
            if (device.isValid)
            {
                HapticCapabilities capabilities;
                if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
                {
                    device.SendHapticImpulse(0, amplitude, duration);
                }
            }
        }
    }
}
