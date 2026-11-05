using UnityEngine;

namespace BenchFlow
{
    [CreateAssetMenu(menuName = "BenchFlow/Component Definition")]
    public sealed class ComponentDefinition : ScriptableObject
    {
        public string componentId;
        public int terminalCount = 2;
        public Color displayColor = Color.white;
    }

    /// <summary>Electrical identity and snap state for a visual prefab.</summary>
    public sealed class CircuitPart : MonoBehaviour
    {
        public string componentId;
        public string[] terminalNodes = new string[2];
        public HoleAddress snappedHole;
        public HoleAddress firstTerminalHole;
        public HoleAddress secondTerminalHole;
        public Vector3 terminalOffsetA = new Vector3(-0.06f, 0f, 0f);
        public Vector3 terminalOffsetB = new Vector3(0.06f, 0f, 0f);
        public bool isSnapped;

        public void SnapTo(BreadboardGrid board, Vector3 target)
        {
            isSnapped = false;
            if (board.TrySnap(target, out snappedHole, out Vector3 point))
            {
                transform.position = point;
                board.TrySnap(transform.TransformPoint(terminalOffsetA), out firstTerminalHole, out _);
                board.TrySnap(transform.TransformPoint(terminalOffsetB), out secondTerminalHole, out _);
                isSnapped = true;
            }
        }

        public string Terminal(int index) => terminalNodes[index];

        public void AttachTerminalsToHole(BreadboardGrid board, CircuitGraph graph, HoleAddress first, HoleAddress second)
        {
            if (terminalNodes == null || terminalNodes.Length < 2) terminalNodes = new string[2];
            terminalNodes[0] = componentId == "LED" ? "LED:ANODE" : componentId + ":A";
            terminalNodes[1] = componentId == "LED" ? "LED:CATHODE" : componentId + ":B";
            graph.Connect(terminalNodes[0], board.ElectricalNode(first));
            graph.Connect(terminalNodes[1], board.ElectricalNode(second));
        }

        public void AttachSnappedTerminals(BreadboardGrid board, CircuitGraph graph)
        {
            AttachTerminalsToHole(board, graph, firstTerminalHole, secondTerminalHole);
        }
    }
}
