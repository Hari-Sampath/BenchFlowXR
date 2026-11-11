using UnityEngine;

namespace BenchFlow
{
    public sealed class TutorialController : MonoBehaviour
    {
        public Transform[] stepTargets;
        public Transform marker;
        public Vector3 markerOffset = new Vector3(0f, 0.006f, 0f);
        public Transform promptRoot;
        public TextMesh promptText;
        public Camera viewCamera;
        public string previewStatus = "";
        public CircuitGraph graph = new CircuitGraph();
        public bool resistorPlaced;
        public bool ledPlaced;
        public int stepIndex;
        string verificationFeedback = "";

        readonly string[] instructions = {
            "Place the resistor so its legs sit in separate breadboard strips.",
            "Connect Arduino D13 to one resistor terminal.",
            "Place the LED with its two legs in separate strips.",
            "Connect the resistor's free end to the LED's long leg (anode).",
            "Connect the LED's short leg (cathode) to Arduino GND."
        };

        readonly string[] corrections = {
            "Move the resistor until each leg is in a different strip.",
            "Add a wire from D13 to either resistor terminal.",
            "Move the LED until its legs are in different strips.",
            "Wire the open resistor end to the LED's long leg (anode).",
            "Wire the LED's short leg (cathode) to GND."
        };

        public string CurrentInstruction => instructions[Mathf.Clamp(stepIndex, 0, instructions.Length - 1)];

        void Update()
        {
            if (promptText != null)
            {
                string instruction = stepIndex >= instructions.Length
                    ? "CIRCUIT VERIFIED\nD13 → resistor → LED → GND"
                    : $"STEP {stepIndex + 1} / {instructions.Length}\n{CurrentInstruction}";
                string feedback = string.IsNullOrEmpty(previewStatus) ? verificationFeedback : previewStatus;
                promptText.text = string.IsNullOrEmpty(feedback) ? instruction : instruction + "\n\n" + feedback;
            }
            if (promptRoot != null) promptRoot.localRotation = Quaternion.identity;

            if (marker == null || stepTargets == null || stepTargets.Length == 0) return;
            Transform target = stepTargets[Mathf.Clamp(stepIndex, 0, stepTargets.Length - 1)];
            marker.gameObject.SetActive(target != null);
            if (target != null)
            {
                marker.position = target.position + markerOffset;
            }
        }

        public CircuitCheck VerifyStep()
        {
            if (stepIndex >= instructions.Length)
                return RecordResult(CircuitValidator.ValidateLedCircuit(graph, resistorPlaced, ledPlaced));

            bool complete;
            switch (stepIndex)
            {
                case 0: complete = resistorPlaced; break;
                case 1: complete = graph.AreConnected("UNO:D13", "RESISTOR:A") ||
                                    graph.AreConnected("UNO:D13", "RESISTOR:B"); break;
                case 2: complete = ledPlaced; break;
                case 3:
                    bool d13AtA = graph.AreConnected("UNO:D13", "RESISTOR:A");
                    bool d13AtB = graph.AreConnected("UNO:D13", "RESISTOR:B");
                    complete = d13AtA
                        ? graph.AreConnected("RESISTOR:B", "LED:ANODE")
                        : d13AtB && graph.AreConnected("RESISTOR:A", "LED:ANODE");
                    break;
                default: complete = graph.AreConnected("LED:CATHODE", "UNO:GND"); break;
            }

            if (!complete)
                return RecordResult(new CircuitCheck(false, "Needs correction: " + corrections[stepIndex]));
            stepIndex++;
            if (stepIndex == instructions.Length)
                return RecordResult(CircuitValidator.ValidateLedCircuit(graph, resistorPlaced, ledPlaced));
            return RecordResult(new CircuitCheck(true, "Step complete. Next: " + CurrentInstruction));
        }

        CircuitCheck RecordResult(CircuitCheck result)
        {
            verificationFeedback = result.message;
            return result;
        }
    }
}
