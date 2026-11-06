using UnityEngine;

namespace BenchFlow
{
    public sealed class TutorialController : MonoBehaviour
    {
        public Transform[] stepTargets;
        public Transform marker;
        public Transform promptRoot;
        public TextMesh promptText;
        public Camera viewCamera;
        public string previewStatus = "";
        public CircuitGraph graph = new CircuitGraph();
        public bool resistorPlaced;
        public bool ledPlaced;
        public int stepIndex;

        readonly string[] instructions = {
            "Place a resistor across two strips.",
            "Connect D13 to the resistor.",
            "Place the LED across two strips.",
            "Connect the resistor to the LED anode.",
            "Connect the LED cathode to GND."
        };

        public string CurrentInstruction => instructions[Mathf.Clamp(stepIndex, 0, instructions.Length - 1)];

        void Update()
        {
            if (promptText != null) promptText.text = (stepIndex >= instructions.Length
                ? "CIRCUIT VERIFIED\nD13 → resistor → LED → GND"
                : $"STEP {stepIndex + 1} / {instructions.Length}\n{CurrentInstruction}");
            if (promptRoot != null) promptRoot.localRotation = Quaternion.identity;

            if (marker == null || stepTargets == null || stepTargets.Length == 0) return;
            Transform target = stepTargets[Mathf.Clamp(stepIndex, 0, stepTargets.Length - 1)];
            if (target != null)
            {
                marker.position = target.position;
            }
        }

        public CircuitCheck VerifyStep()
        {
            if (stepIndex >= instructions.Length)
                return CircuitValidator.ValidateLedCircuit(graph, resistorPlaced, ledPlaced);

            bool complete;
            switch (stepIndex)
            {
                case 0: complete = resistorPlaced; break;
                case 1: complete = graph.AreConnected("UNO:D13", "RESISTOR:A"); break;
                case 2: complete = ledPlaced; break;
                case 3: complete = graph.AreConnected("RESISTOR:B", "LED:ANODE"); break;
                default: complete = graph.AreConnected("LED:CATHODE", "UNO:GND"); break;
            }

            if (!complete) return new CircuitCheck(false, CurrentInstruction);
            stepIndex++;
            if (stepIndex == instructions.Length)
                return CircuitValidator.ValidateLedCircuit(graph, resistorPlaced, ledPlaced);
            return new CircuitCheck(true, "Step complete. " + CurrentInstruction);
        }
    }
}
