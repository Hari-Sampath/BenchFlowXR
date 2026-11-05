using System.Collections.Generic;

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
            if (!resistorPlaced) return new CircuitCheck(false, "Place the resistor across two separate breadboard strips.");
            if (!ledPlaced) return new CircuitCheck(false, "Place the LED with its legs in separate strips.");
            if (!graph.AreConnected("UNO:D13", "RESISTOR:A"))
                return new CircuitCheck(false, "Connect Arduino D13 to one end of the resistor.");
            if (!graph.AreConnected("RESISTOR:B", "LED:ANODE"))
                return new CircuitCheck(false, "Connect the resistor's free end to the LED's long leg (anode).");
            if (!graph.AreConnected("LED:CATHODE", "UNO:GND"))
                return new CircuitCheck(false, "Connect the LED's short leg (cathode) to Arduino GND.");
            return new CircuitCheck(true, "Circuit verified: D13 → resistor → LED → GND.");
        }
    }
}
