using UnityEngine;
using System.Collections.Generic;

namespace BenchFlow
{
    /// <summary>
    /// Input boundary for Meta Interaction SDK, desktop preview, or future MR adapters.
    /// Adapters call these methods; circuit and tutorial state do not depend on a headset SDK.
    /// </summary>
    public interface IWorkbenchInteractionPort
    {
        void BeginGrab(CircuitPart part, Pose handPose);
        void UpdateGrab(CircuitPart part, Pose handPose);
        void EndGrab(CircuitPart part, Vector3 releasePosition);
        void ConnectWire(string terminalA, string terminalB);
    }

    public sealed class WorkbenchState : MonoBehaviour, IWorkbenchInteractionPort
    {
        public BreadboardGrid breadboard;
        public TutorialController tutorial;
        public CircuitGraph Circuit { get; } = new CircuitGraph();
        readonly List<CircuitPart> parts = new List<CircuitPart>();
        readonly List<(string a, string b)> wires = new List<(string a, string b)>();

        void Awake()
        {
            RebuildCircuit();
        }

        public void BeginGrab(CircuitPart part, Pose handPose) { }
        public void UpdateGrab(CircuitPart part, Pose handPose) => part.transform.SetPositionAndRotation(handPose.position, handPose.rotation);
        public void EndGrab(CircuitPart part, Vector3 releasePosition)
        {
            if (breadboard == null) return;
            part.SnapTo(breadboard, releasePosition);
            if (!parts.Contains(part)) parts.Add(part);
            RebuildCircuit();
            if (tutorial != null)
            {
                tutorial.graph = Circuit;
                tutorial.resistorPlaced = parts.Exists(p => p != null && p.isSnapped && p.componentId == "RESISTOR");
                tutorial.ledPlaced = parts.Exists(p => p != null && p.isSnapped && p.componentId == "LED");
            }
        }
        public void ConnectWire(string terminalA, string terminalB)
        {
            wires.Add((terminalA, terminalB));
            RebuildCircuit();
        }

        void RebuildCircuit()
        {
            Circuit.Clear();
            if (breadboard != null) breadboard.ConnectBoardNets(Circuit);
            Circuit.RegisterNode("UNO:D13");
            Circuit.RegisterNode("UNO:GND");
            foreach (CircuitPart part in parts)
                if (part != null && part.isSnapped && breadboard != null)
                    part.AttachSnappedTerminals(breadboard, Circuit);
            foreach (var wire in wires) Circuit.Connect(wire.a, wire.b);
            if (tutorial != null) tutorial.graph = Circuit;
        }
    }
}
