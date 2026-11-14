using System;
using UnityEngine;

namespace BenchFlow
{
    public sealed class CircuitCheck
    {
        public readonly bool isValid;
        public readonly string message;
        public readonly string detailedHint;

        public CircuitCheck(bool isValid, string message, string detailedHint = "")
        {
            this.isValid = isValid;
            this.message = message;
            this.detailedHint = detailedHint;
        }
    }

    /// <summary>
    /// Evaluates circuit connectivity, polarities, and shorts.
    /// Provides clear diagnostic correction messages when a learner makes a mistake.
    /// </summary>
    public static class CircuitValidator
    {
        public static CircuitCheck Validate(
            CircuitGraph graph,
            BreadboardGrid board,
            CircuitPart resistor,
            CircuitPart led)
        {
            if (graph == null) return new CircuitCheck(false, "Circuit graph is not initialized.");

            // 1. Direct Short Circuit Check
            if (graph.AreConnected("UNO:D13", "UNO:GND"))
            {
                return new CircuitCheck(false,
                    "Short circuit detected!",
                    "Pin 13 is connected directly to GND without a current-limiting resistor or load.");
            }

            // 2. LED Placement Check
            if (led == null || !led.isSnapped)
            {
                return new CircuitCheck(false,
                    "Place the red LED.",
                    "Insert the LED into the breadboard (e.g. long leg at 14c, short leg at 16c).");
            }

            if (board != null && led.IsShortedSameStrip(board))
            {
                return new CircuitCheck(false,
                    "LED is shorted across the same strip!",
                    $"Both legs of the LED are in {led.electricalNodeA}. Each leg must be in a different row.");
            }

            // 3. Resistor Placement Check
            if (resistor == null || !resistor.isSnapped)
            {
                return new CircuitCheck(false,
                    "Add the 220 Ω resistor.",
                    "Bridge the resistor between row 8 (input) and row 14 (LED anode).");
            }

            if (board != null && resistor.IsShortedSameStrip(board))
            {
                return new CircuitCheck(false,
                    "Resistor is shorted across the same strip!",
                    $"Both leads are in {resistor.electricalNodeA}. Span the resistor across two separate rows.");
            }

            // 4. Pin 13 Connection Check
            bool d13ToResA = graph.AreConnected("UNO:D13", "RESISTOR:A");
            bool d13ToResB = graph.AreConnected("UNO:D13", "RESISTOR:B");

            if (!d13ToResA && !d13ToResB)
            {
                return new CircuitCheck(false,
                    "Missing connection from Arduino Pin 13.",
                    "Connect a jumper wire from Arduino Pin 13 to row 8 (where the resistor starts).");
            }

            // Resistor is non-polarized: determine which terminal receives D13 and which feeds the LED
            string resOutput = d13ToResA ? "RESISTOR:B" : "RESISTOR:A";

            // 5. Reversed LED Polarity Check
            bool reversedLed = graph.AreConnected(resOutput, "LED:CATHODE") || graph.AreConnected("UNO:GND", "LED:ANODE");
            if (reversedLed)
            {
                return new CircuitCheck(false,
                    "LED polarity is reversed!",
                    "The LED's long leg (anode) must connect to the resistor, and the short leg (cathode) to GND. Rotate or flip the LED.");
            }

            // 6. Resistor to LED Anode Connection Check
            bool resToAnode = graph.AreConnected(resOutput, "LED:ANODE");
            if (!resToAnode)
            {
                return new CircuitCheck(false,
                    "Resistor is not connected to the LED.",
                    "Make sure the resistor's second lead and the LED's long leg share the same row strip (row 14).");
            }

            // 7. Ground Connection Check
            bool cathodeToGnd = graph.AreConnected("LED:CATHODE", "UNO:GND");
            if (!cathodeToGnd)
            {
                return new CircuitCheck(false,
                    "Missing connection to Arduino GND.",
                    "Connect a jumper wire from Arduino GND to row 16 (where the LED cathode connects).");
            }

            // 8. Full Complete Circuit Verified!
            return new CircuitCheck(true,
                "Circuit verified!",
                "Current flows: D13 → 220 Ω Resistor → Red LED (Anode → Cathode) → GND.");
        }
    }
}
