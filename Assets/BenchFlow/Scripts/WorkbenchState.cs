using System.Collections.Generic;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>Owns placed parts and rebuilds the electrical graph after each interaction.</summary>
    public sealed class WorkbenchState : MonoBehaviour, IWorkbenchInteractionPort
    {
        public BreadboardGrid breadboard;
        public TutorialController tutorial;
        public CircuitGraph Circuit { get; } = new CircuitGraph();
        readonly List<CircuitPart> parts = new List<CircuitPart>();
        readonly List<(string a, string b)> wires = new List<(string a, string b)>();

        void Awake() => RebuildCircuit();

        public void BeginGrab(CircuitPart part, Pose handPose)
        {
            if (part == null) return;
            part.isSnapped = false;
            RebuildCircuit();
        }

        public void UpdateGrab(CircuitPart part, Pose handPose) => part.transform.SetPositionAndRotation(handPose.position, handPose.rotation);

        public void EndGrab(CircuitPart part, Vector3 releasePosition)
        {
            if (part == null) return;
            if (breadboard == null)
            {
                part.isSnapped = false;
                if (!parts.Contains(part)) parts.Add(part);
                RebuildCircuit();
                return;
            }
            part.SnapTo(breadboard, releasePosition);
            part.isSnapped = HasValidPlacement(part);
            if (!parts.Contains(part)) parts.Add(part);
            RebuildCircuit();
        }

        public void ConnectWire(string terminalA, string terminalB)
        {
            if (string.IsNullOrWhiteSpace(terminalA) || string.IsNullOrWhiteSpace(terminalB)) return;
            terminalA = terminalA.Trim();
            terminalB = terminalB.Trim();
            foreach (var wire in wires)
                if ((wire.a == terminalA && wire.b == terminalB) || (wire.a == terminalB && wire.b == terminalA)) return;
            wires.Add((terminalA, terminalB));
            RebuildCircuit();
        }

        public bool DisconnectWire(string terminalA, string terminalB)
        {
            if (string.IsNullOrWhiteSpace(terminalA) || string.IsNullOrWhiteSpace(terminalB)) return false;
            terminalA = terminalA.Trim();
            terminalB = terminalB.Trim();
            int index = wires.FindIndex(w => (w.a == terminalA && w.b == terminalB) || (w.a == terminalB && w.b == terminalA));
            if (index < 0) return false;
            wires.RemoveAt(index);
            RebuildCircuit();
            return true;
        }

        public void ClearWires()
        {
            if (wires.Count == 0) return;
            wires.Clear();
            RebuildCircuit();
        }

        void UpdateTutorialPlacementState()
        {
            if (tutorial == null) return;
            tutorial.graph = Circuit;
            tutorial.resistorPlaced = HasValidPlacement("RESISTOR");
            tutorial.ledPlaced = HasValidPlacement("LED");
        }

        bool HasValidPlacement(string componentId)
        {
            if (breadboard == null) return false;
            return parts.Exists(part => part != null && part.componentId == componentId && HasValidPlacement(part));
        }

        bool HasValidPlacement(CircuitPart part)
        {
            return breadboard != null && part != null && part.isSnapped &&
                IsTerminalOnBoard(part, part.terminalOffsetA, part.firstTerminalHole) &&
                IsTerminalOnBoard(part, part.terminalOffsetB, part.secondTerminalHole);
        }

        bool IsTerminalOnBoard(CircuitPart part, Vector3 localOffset, HoleAddress savedHole)
        {
            if (!breadboard.TryGetElectricalNode(savedHole, out _)) return false;
            return breadboard.TrySnap(part.transform.TransformPoint(localOffset), out HoleAddress actualHole, out _) && actualHole.Equals(savedHole);
        }

        void RebuildCircuit()
        {
            Circuit.Clear();
            if (breadboard != null) breadboard.ConnectBoardNets(Circuit);
            Circuit.RegisterNode("UNO:D13");
            Circuit.RegisterNode("UNO:GND");
            foreach (CircuitPart part in parts)
                if (part != null && HasValidPlacement(part) && !string.IsNullOrWhiteSpace(part.componentId))
                    part.AttachSnappedTerminals(breadboard, Circuit);
            foreach (var wire in wires) Circuit.Connect(wire.a, wire.b);
            UpdateTutorialPlacementState();
        }
    }
}
