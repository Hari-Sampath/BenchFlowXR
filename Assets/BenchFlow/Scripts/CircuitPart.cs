using System;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Represents a 2-terminal physical circuit component (Resistor, LED, etc.)
    /// that can be picked up, rotated, and snapped onto the breadboard.
    /// </summary>
    public sealed class CircuitPart : MonoBehaviour
    {
        [Header("Electrical Identity")]
        public string componentId = "RESISTOR";
        public bool isPolarized = false; // true for LED (terminal 0 = Anode, terminal 1 = Cathode)

        [Header("Physical Leads")]
        [Tooltip("Local offset for Terminal A (Anode for LED)")]
        public Vector3 terminalOffsetA = new Vector3(-0.00762f, 0f, 0f);
        [Tooltip("Local offset for Terminal B (Cathode for LED)")]
        public Vector3 terminalOffsetB = new Vector3(0.00762f, 0f, 0f);

        [Header("Runtime Snap State")]
        public bool isSnapped = false;
        public bool isLocked = false; // When placed in correct position, locked so it cannot be moved
        public HoleAddress terminalHoleA;
        public HoleAddress terminalHoleB;
        public string electricalNodeA = "";
        public string electricalNodeB = "";

        [Header("Visual Feedback")]
        public Renderer[] partRenderers;
        public Color hoverColor = new Color(0.2f, 0.8f, 1f, 1f);

        // Saved home transform on parts tray
        [HideInInspector] public Vector3 homePosition;
        [HideInInspector] public Quaternion homeRotation;

        void Awake()
        {
            if (partRenderers == null || partRenderers.Length == 0)
                partRenderers = GetComponentsInChildren<Renderer>();

            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        public Vector3 WorldLeadPositionA => transform.TransformPoint(terminalOffsetA);
        public Vector3 WorldLeadPositionB => transform.TransformPoint(terminalOffsetB);

        /// <summary>Returns true if both legs are in the same electrical strip (shorted!).</summary>
        public bool IsShortedSameStrip(BreadboardGrid board)
        {
            if (!isSnapped || board == null) return false;
            return board.ElectricalNode(terminalHoleA) == board.ElectricalNode(terminalHoleB);
        }

        /// <summary>Tests snapping onto the breadboard from a release or drag position.</summary>
        public bool TrySnapToBoard(BreadboardGrid board, Vector3 targetPosition, Quaternion targetRotation)
        {
            isSnapped = false;
            if (board == null) return false;

            // Apply candidate transform
            transform.SetPositionAndRotation(targetPosition, targetRotation);

            // Test snap for both leads
            Vector3 leadA = WorldLeadPositionA;
            Vector3 leadB = WorldLeadPositionB;

            bool snapA = board.TrySnap(leadA, out HoleAddress holeA, out Vector3 snappedPosA);
            bool snapB = board.TrySnap(leadB, out HoleAddress holeB, out Vector3 snappedPosB);

            if (snapA && snapB)
            {
                // Align component position to the midpoint of the two snapped holes
                Vector3 center = (snappedPosA + snappedPosB) * 0.5f;
                // Keep the part resting on the breadboard surface
                transform.position = center - (terminalOffsetA + terminalOffsetB) * 0.5f;

                terminalHoleA = holeA;
                terminalHoleB = holeB;
                electricalNodeA = board.ElectricalNode(holeA);
                electricalNodeB = board.ElectricalNode(holeB);
                isSnapped = true;
                return true;
            }

            return false;
        }

        /// <summary>Rotates the component by 90 degrees around its local Y axis.</summary>
        public void Rotate90()
        {
            transform.Rotate(0f, 90f, 0f, Space.Self);
        }

        /// <summary>Returns to home tray position if released in invalid space.</summary>
        public void ReturnHome()
        {
            isSnapped = false;
            isLocked = false;
            terminalHoleA = default;
            terminalHoleB = default;
            electricalNodeA = "";
            electricalNodeB = "";
            transform.SetPositionAndRotation(homePosition, homeRotation);
        }

        /// <summary>Attaches terminals to the circuit graph.</summary>
        public void AttachSnappedTerminals(BreadboardGrid board, CircuitGraph graph)
        {
            if (!isSnapped || board == null || graph == null) return;

            string idA = isPolarized ? $"{componentId}:ANODE" : $"{componentId}:A";
            string idB = isPolarized ? $"{componentId}:CATHODE" : $"{componentId}:B";

            electricalNodeA = board.ElectricalNode(terminalHoleA);
            electricalNodeB = board.ElectricalNode(terminalHoleB);

            graph.RegisterNode(idA);
            graph.RegisterNode(idB);
            graph.Connect(idA, electricalNodeA);
            graph.Connect(idB, electricalNodeB);
        }
    }
}
