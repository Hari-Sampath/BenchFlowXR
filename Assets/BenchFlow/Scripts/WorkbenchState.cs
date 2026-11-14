using System.Collections.Generic;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Owns the active circuit graph, placed parts, wires, and validation state.
    /// Acts as the interaction port for both VR controller/hand input and Desktop preview.
    /// Supports resetting, magnetic ghost preview, and spatial audio feedback.
    /// </summary>
    public sealed class WorkbenchState : MonoBehaviour, IWorkbenchInteractionPort
    {
        public BreadboardGrid breadboard;
        public TutorialController tutorial;
        public CircuitPart resistor;
        public CircuitPart led;
        public BlinkAnimator blinkAnimator;

        public CircuitGraph Circuit { get; } = new CircuitGraph();

        readonly List<CircuitPart> placedParts = new List<CircuitPart>();
        readonly List<(string from, string to, WireRenderer renderer)> activeWires = new List<(string, string, WireRenderer)>();

        void Awake()
        {
            AutoWireReferences();
            RebuildCircuit();
        }

        public void AutoWireReferences()
        {
            if (breadboard == null)
                breadboard = FindAnyObjectByType<BreadboardGrid>();

            if (tutorial == null)
                tutorial = FindAnyObjectByType<TutorialController>();

            if (resistor == null || led == null)
            {
                CircuitPart[] allParts = FindObjectsByType<CircuitPart>();
                foreach (var p in allParts)
                {
                    if (p.componentId == "RESISTOR" && resistor == null) resistor = p;
                    else if (p.componentId == "LED" && led == null) led = p;
                }
            }

            if (blinkAnimator == null)
                blinkAnimator = FindAnyObjectByType<BlinkAnimator>();

            if (FindAnyObjectByType<VRRig>() == null)
            {
                Camera cam = Camera.main ?? FindAnyObjectByType<Camera>();
                if (cam != null)
                {
                    GameObject rigObj = new GameObject("[XR Rig] Player");
                    rigObj.transform.position = cam.transform.position;
                    VRRig rig = rigObj.AddComponent<VRRig>();
                    rig.centerEyeCamera = cam;
                    rig.headTransform = cam.transform;
                    rig.desktopPreview = FindAnyObjectByType<DesktopPreviewInteractor>();
                }
            }
        }

        public void BeginGrab(CircuitPart part, Pose handPose)
        {
            if (part == null) return;

            // Show holographic ghost target for held part
            if (GhostPreview.Instance != null)
            {
                GhostPreview.Instance.ShowGhost(part.componentId, true);
            }

            if (part.isSnapped)
            {
                part.isSnapped = false;
                RebuildCircuit();
            }
        }

        public void UpdateGrab(CircuitPart part, Pose handPose)
        {
            if (part == null) return;
            part.transform.SetPositionAndRotation(handPose.position, handPose.rotation);
        }

        public void EndGrab(CircuitPart part, Vector3 releasePosition)
        {
            if (part == null || breadboard == null) return;

            if (GhostPreview.Instance != null)
            {
                GhostPreview.Instance.HideAll();
            }

            // Attempt snapping
            bool snapped = part.TrySnapToBoard(breadboard, releasePosition, part.transform.rotation);
            if (!snapped)
            {
                part.ReturnHome();
            }
            else
            {
                // Play tactile snap click
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySnap(part.transform.position);
                }
            }

            if (!placedParts.Contains(part)) placedParts.Add(part);
            RebuildCircuit();

            if (tutorial != null)
            {
                tutorial.OnComponentStateChanged();
            }
        }

        public void ConnectWire(string terminalA, string terminalB)
        {
            ConnectWireVisual(terminalA, terminalB, Vector3.zero, Vector3.zero, Color.red);
        }

        public WireRenderer ConnectWireVisual(string terminalA, string terminalB, Vector3 startPos, Vector3 endPos, Color color)
        {
            foreach (var w in activeWires)
            {
                if ((w.from == terminalA && w.to == terminalB) || (w.from == terminalB && w.to == terminalA))
                    return w.renderer;
            }

            GameObject wireObj = new GameObject($"Wire_{terminalA}_{terminalB}");
            wireObj.transform.SetParent(transform, true);
            WireRenderer wireComp = wireObj.AddComponent<WireRenderer>();
            wireComp.Setup(terminalA, terminalB, startPos, endPos, color);

            activeWires.Add((terminalA, terminalB, wireComp));
            if (blinkAnimator != null) blinkAnimator.RegisterWire(wireComp);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayWire(endPos != Vector3.zero ? endPos : transform.position);
            }

            RebuildCircuit();

            if (tutorial != null)
            {
                tutorial.OnComponentStateChanged();
            }

            return wireComp;
        }

        public bool HasWire(string nodeA, string nodeB)
        {
            foreach (var w in activeWires)
            {
                if ((w.from == nodeA && w.to == nodeB) || (w.from == nodeB && w.to == nodeA))
                    return true;
            }
            return false;
        }

        public void ClearWires()
        {
            foreach (var w in activeWires)
            {
                if (w.renderer != null) Destroy(w.renderer.gameObject);
            }
            activeWires.Clear();
            RebuildCircuit();
        }

        /// <summary>Resets the entire workbench to fresh lesson state.</summary>
        public void ResetWorkbench()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButton(transform.position);
            }

            if (blinkAnimator != null)
            {
                blinkAnimator.StopBlinking();
            }

            if (resistor != null) resistor.ReturnHome();
            if (led != null) led.ReturnHome();

            ClearWires();

            if (tutorial != null)
            {
                tutorial.ResetTutorial();
            }
        }

        public void RebuildCircuit()
        {
            Circuit.Clear();

            if (breadboard != null)
            {
                breadboard.ConnectBoardNets(Circuit);
            }

            Circuit.RegisterNode("UNO:D13");
            Circuit.RegisterNode("UNO:GND");

            if (resistor != null && resistor.isSnapped && breadboard != null)
            {
                resistor.AttachSnappedTerminals(breadboard, Circuit);
            }
            if (led != null && led.isSnapped && breadboard != null)
            {
                led.AttachSnappedTerminals(breadboard, Circuit);
            }

            foreach (var w in activeWires)
            {
                Circuit.Connect(w.from, w.to);
            }
        }

        public CircuitCheck ValidateCircuit()
        {
            RebuildCircuit();
            CircuitCheck check = CircuitValidator.Validate(Circuit, breadboard, resistor, led);
            if (check.isValid)
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySuccess(breadboard != null ? breadboard.transform.position : transform.position);
            }
            else
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayError(breadboard != null ? breadboard.transform.position : transform.position);
            }
            return check;
        }
    }
}
