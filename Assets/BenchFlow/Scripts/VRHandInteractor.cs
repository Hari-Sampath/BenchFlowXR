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
                    CircuitPart part = hit.collider.GetComponentInParent<CircuitPart>();
                    if (part != null)
                    {
                        GrabPart(part);
                        return;
                    }

                    // Check for RUN button
                    if (hit.collider.name.Contains("Run") || (tutorial != null && hit.collider.gameObject == tutorial.runButtonObject))
                    {
                        tutorial.OnRunButtonPressed();
                        TriggerHaptic(0.5f, 0.1f);
                        return;
                    }

                    // Check for RESET button
                    if (hit.collider.name.Contains("Reset") || (tutorial != null && hit.collider.gameObject == tutorial.resetButtonObject))
                    {
                        tutorial.OnResetButtonPressed();
                        TriggerHaptic(0.5f, 0.1f);
                        return;
                    }

                    // Check for quick wire shortcuts on Pin 13 / GND
                    if (hit.collider.name.Contains("13") || hit.collider.name.Contains("GND"))
                    {
                        HandlePinTrigger(hit.collider.name, hit.point);
                    }
                }
            }
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
