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
        public string previewStatus = "Drag R/LED onto the board • 1/2/3 add preview wires • Space verifies";
        public CircuitGraph graph = new CircuitGraph();
        public bool resistorPlaced;
        public bool ledPlaced;
        public int stepIndex;

        readonly string[] instructions = {
            "Pick up a resistor and place it across two breadboard strips.",
            "Connect Arduino D13 to the resistor.",
            "Place the LED with its legs in separate strips.",
            "Connect the resistor to the LED's long leg (anode).",
            "Connect the LED's short leg (cathode) to Arduino GND."
        };

        public string CurrentInstruction => instructions[Mathf.Clamp(stepIndex, 0, instructions.Length - 1)];

        void Update()
        {
            if (promptText != null) promptText.text = (stepIndex >= instructions.Length
                ? "CIRCUIT VERIFIED\nD13 → resistor → LED → GND"
                : $"STEP {stepIndex + 1} / {instructions.Length}\n{CurrentInstruction}") + "\n" + previewStatus;
            if (promptRoot != null && viewCamera != null)
                promptRoot.rotation = Quaternion.LookRotation(promptRoot.position - viewCamera.transform.position);

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
