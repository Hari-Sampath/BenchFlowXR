namespace BenchFlow
{
    public sealed class CircuitCheck
    {
        public readonly bool valid;
        public readonly string message;
        public CircuitCheck(bool valid, string message) { this.valid = valid; this.message = message; }
    }

    /// <summary>Checks the expected D13 -> resistor -> LED -> GND path by net membership.</summary>
    public static class CircuitValidator
    {
        public static CircuitCheck ValidateLedCircuit(CircuitGraph graph, bool resistorPlaced, bool ledPlaced)
        {
            if (graph == null) return new CircuitCheck(false, "Circuit state is unavailable. Rebuild the workbench circuit and try again.");
            if (!resistorPlaced) return new CircuitCheck(false, "Place the resistor across two separate breadboard strips.");
            if (!ledPlaced) return new CircuitCheck(false, "Place the LED with its legs in separate strips.");

            if (graph.AreConnected("RESISTOR:A", "RESISTOR:B"))
                return new CircuitCheck(false, "Move the resistor legs to separate electrical strips.");
            if (graph.AreConnected("LED:ANODE", "LED:CATHODE"))
                return new CircuitCheck(false, "Move the LED legs to separate electrical strips.");

            bool d13AtA = graph.AreConnected("UNO:D13", "RESISTOR:A");
            bool d13AtB = graph.AreConnected("UNO:D13", "RESISTOR:B");
            if (!d13AtA && !d13AtB)
                return new CircuitCheck(false, "Connect Arduino D13 to either end of the resistor.");

            string freeResistorEnd = d13AtA ? "RESISTOR:B" : "RESISTOR:A";
            if (!graph.AreConnected(freeResistorEnd, "LED:ANODE"))
                return new CircuitCheck(false, "Connect the resistor end opposite D13 to the LED's long leg (anode).");
            if (!graph.AreConnected("LED:CATHODE", "UNO:GND"))
                return new CircuitCheck(false, "Connect the LED's short leg (cathode) to Arduino GND.");
            return new CircuitCheck(true, "Circuit verified: D13 → resistor → LED → GND.");
        }
    }
}
