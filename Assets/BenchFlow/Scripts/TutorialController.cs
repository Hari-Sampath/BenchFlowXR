using System;
using UnityEngine;

namespace BenchFlow
{
    /// <summary>
    /// Controls the 4-step interactive electronics tutorial:
    /// 1. Place red LED (14c to 16c)
    /// 2. Add 220 Ω resistor (8d to 14d)
    /// 3. Connect jumper wires (Pin 13 -> 8b, GND -> 16a)
    /// 4. Tap RUN to execute the Arduino blink program!
    /// </summary>
    public sealed class TutorialController : MonoBehaviour
    {
        [Header("Scene References")]
        public WorkbenchState workbench;
        public BreadboardGrid breadboard;
        public BlinkAnimator blinkAnimator;

        [Header("Step Markers & Guides")]
        public GameObject markerHole14c;
        public GameObject markerHole16c;
        public GameObject markerHole8d;
        public GameObject markerHole14d;
        public GameObject markerPin13;
        public GameObject markerPinGnd;
        public GameObject markerHole8b;
        public GameObject markerHole16a;

        [Header("UI Panels & Text")]
        public TextMesh stepTitleText;
        public TextMesh stepInstructionText;
        public TextMesh statusText;
        public TextMesh codeDisplayNode;
        public GameObject runButtonObject;
        public TextMesh runButtonText;
        public GameObject resetButtonObject;

        [Header("Progress State")]
        [Range(0, 3)] public int currentStep = 0; // 0=LED, 1=Resistor, 2=Wires, 3=Run
        public bool isProgramRunning = false;
        public string latestFeedback = "";

        const string ArduinoBlinkCode =
@"void loop() {
  digitalWrite(13, HIGH);
  delay(500);
  digitalWrite(13, LOW);
  delay(500);
}";

        void Awake()
        {
            AutoWireReferences();
        }

        void Start()
        {
            UpdateStepUI();
        }

        public void AutoWireReferences()
        {
            if (workbench == null) workbench = FindAnyObjectByType<WorkbenchState>();
            if (breadboard == null) breadboard = FindAnyObjectByType<BreadboardGrid>();
            if (blinkAnimator == null) blinkAnimator = FindAnyObjectByType<BlinkAnimator>();

            if (stepInstructionText == null)
            {
                TextMesh[] texts = FindObjectsByType<TextMesh>();
                foreach (var t in texts)
                {
                    if (t.name.Contains("Instruction") || t.name.Contains("Step instruction"))
                    {
                        stepInstructionText = t;
                        break;
                    }
                }
            }

            if (markerPin13 == null)
            {
                GameObject p13 = GameObject.Find("UNO-compatible digital pin 13") ?? GameObject.Find("UNO Digital Pin 13");
                if (p13 != null) markerPin13 = p13;
            }

            if (markerPinGnd == null)
            {
                GameObject pgnd = GameObject.Find("UNO-compatible GND") ?? GameObject.Find("UNO GND");
                if (pgnd != null) markerPinGnd = pgnd;
            }

            if (runButtonObject == null)
            {
                GameObject btn = GameObject.Find("3D RUN Button") ?? GameObject.Find("RUN Button") ?? GameObject.Find("Run");
                if (btn != null) runButtonObject = btn;
            }
        }

        public void OnComponentStateChanged()
        {
            CheckStepProgress();
            UpdateStepUI();
        }

        public void CheckStepProgress()
        {
            if (workbench == null || breadboard == null) return;

            CircuitPart led = workbench.led;
            CircuitPart res = workbench.resistor;

            // Check Step 0: LED placed
            if (currentStep == 0)
            {
                if (led != null && led.isSnapped)
                {
                    // Check if placed in rows 14 and 16
                    bool correctRows = (led.terminalHoleA.row == 14 && led.terminalHoleB.row == 16) ||
                                       (led.terminalHoleA.row == 16 && led.terminalHoleB.row == 14);
                    if (correctRows)
                    {
                        if (led.terminalHoleA.row == 14 && led.terminalHoleB.row == 16)
                        {
                            latestFeedback = "LED placed correctly (Anode at 14c, Cathode at 16c).";
                            currentStep = 1;
                        }
                        else
                        {
                            latestFeedback = "LED is reversed! Long leg (anode) should be at 14c.";
                        }
                    }
                    else
                    {
                        latestFeedback = $"LED placed in rows {led.terminalHoleA.row} and {led.terminalHoleB.row}. Target: 14c & 16c.";
                    }
                }
            }

            // Check Step 1: Resistor placed
            if (currentStep == 1)
            {
                if (res != null && res.isSnapped)
                {
                    bool correctRows = (res.terminalHoleA.row == 8 && res.terminalHoleB.row == 14) ||
                                       (res.terminalHoleA.row == 14 && res.terminalHoleB.row == 8);
                    if (correctRows)
                    {
                        latestFeedback = "220 Ω resistor placed between rows 8 and 14.";
                        currentStep = 2;
                    }
                    else
                    {
                        latestFeedback = $"Resistor placed in rows {res.terminalHoleA.row} and {res.terminalHoleB.row}. Target: 8d & 14d.";
                    }
                }
            }

            // Check Step 2: Wires connected
            if (currentStep == 2)
            {
                bool d13Connected = workbench.Circuit.AreConnected("UNO:D13", "strip:A:8") ||
                                    workbench.Circuit.AreConnected("UNO:D13", "RESISTOR:A") ||
                                    workbench.Circuit.AreConnected("UNO:D13", "RESISTOR:B");

                bool gndConnected = workbench.Circuit.AreConnected("UNO:GND", "strip:A:16") ||
                                    workbench.Circuit.AreConnected("UNO:GND", "LED:CATHODE");

                if (d13Connected && gndConnected)
                {
                    latestFeedback = "Wires connected: Pin 13 → 8b, GND → 16a.";
                    currentStep = 3;
                }
            }
        }

        public void UpdateStepUI()
        {
            // Activate markers for active step
            SetMarkerActive(markerHole14c, currentStep == 0);
            SetMarkerActive(markerHole16c, currentStep == 0);

            SetMarkerActive(markerHole8d, currentStep == 1);
            SetMarkerActive(markerHole14d, currentStep == 1);

            SetMarkerActive(markerPin13, currentStep == 2);
            SetMarkerActive(markerHole8b, currentStep == 2);
            SetMarkerActive(markerPinGnd, currentStep == 2);
            SetMarkerActive(markerHole16a, currentStep == 2);

            // RUN button visible in Step 3 or when program running
            if (runButtonObject != null)
            {
                runButtonObject.SetActive(currentStep >= 2 || isProgramRunning);
            }

            // Code panel visible in step 3
            if (codeDisplayNode != null)
            {
                codeDisplayNode.gameObject.SetActive(currentStep == 3 || isProgramRunning);
                if (codeDisplayNode.gameObject.activeSelf)
                    codeDisplayNode.text = ArduinoBlinkCode;
            }

            // Titles and instructions
            switch (currentStep)
            {
                case 0:
                    SetText(stepTitleText, "STEP 1 OF 4\nPLACE THE RED LED");
                    SetText(stepInstructionText, "Long leg (Anode) → 14c\nShort leg (Cathode) → 16c\nFollow the glowing targets.");
                    break;
                case 1:
                    SetText(stepTitleText, "STEP 2 OF 4\nADD THE 220 Ω RESISTOR");
                    SetText(stepInstructionText, "From 8d to 14d\nBridges power to the LED anode.");
                    break;
                case 2:
                    SetText(stepTitleText, "STEP 3 OF 4\nCONNECT TWO JUMPER WIRES");
                    SetText(stepInstructionText, "Pin 13 → 8b (Red)\nArduino GND → 16a (Cyan)\nFollow the glowing paths.");
                    break;
                case 3:
                    if (isProgramRunning)
                    {
                        SetText(stepTitleText, "BLINK — UPLOADED & RUNNING");
                        SetText(stepInstructionText, "Lesson 1 complete! Current flows:\nPin 13 → Resistor → LED → GND");
                    }
                    else
                    {
                        SetText(stepTitleText, "STEP 4 OF 4\nRUN THE BLINK PROGRAM");
                        SetText(stepInstructionText, "Tap the RUN button to compile\nand upload the sketch to the board.");
                    }
                    break;
            }

            if (statusText != null)
            {
                statusText.text = latestFeedback;
            }

            if (runButtonText != null)
            {
                runButtonText.text = isProgramRunning ? "RUNNING" : "RUN";
            }
        }

        /// <summary>Called when the user clicks or points & triggers the 3D RUN button.</summary>
        public void OnRunButtonPressed()
        {
            if (workbench == null) return;

            if (isProgramRunning)
            {
                // Toggle off
                isProgramRunning = false;
                if (blinkAnimator != null) blinkAnimator.StopBlinking();
                latestFeedback = "Program stopped.";
                UpdateStepUI();
                return;
            }

            // Validate circuit
            CircuitCheck check = workbench.ValidateCircuit();
            if (check.isValid)
            {
                isProgramRunning = true;
                currentStep = 3;
                latestFeedback = check.message + " " + check.detailedHint;
                if (blinkAnimator != null) blinkAnimator.StartBlinking();
            }
            else
            {
                isProgramRunning = false;
                latestFeedback = check.message + "\n" + check.detailedHint;
                if (blinkAnimator != null) blinkAnimator.StopBlinking();
            }

            UpdateStepUI();
        }

        /// <summary>Resets tutorial progress back to Step 1.</summary>
        public void ResetTutorial()
        {
            currentStep = 0;
            isProgramRunning = false;
            latestFeedback = "Workbench reset. Start with Step 1: Place the red LED.";
            UpdateStepUI();
        }

        public void OnResetButtonPressed()
        {
            if (workbench != null) workbench.ResetWorkbench();
            else ResetTutorial();
        }

        void SetMarkerActive(GameObject obj, bool active)
        {
            if (obj != null) obj.SetActive(active);
        }

        void SetText(TextMesh tm, string val)
        {
            if (tm != null) tm.text = val;
        }
    }
}
