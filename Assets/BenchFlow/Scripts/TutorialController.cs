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
        public GameObject undoButtonObject;

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
            EnsureVisualPolish();
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

            EnsureVisualPolish();
        }

        /// <summary>
        /// Guarantees that the instruction card backer uses a proper translucent blend mode at runtime,
        /// adds holographic borders and badges, and labels the workbench console buttons with angled nameplates.
        /// </summary>
        public void EnsureVisualPolish()
        {
            EnsureSignboardTranslucent();
            EnsureSignboardDecorations();
            EnsureButtonNameplates();
        }

        public void EnsureSignboardTranslucent()
        {
            GameObject card = GameObject.Find("Guided Instruction Card");
            if (card == null) return;

            Transform backer = card.transform.Find("Card Backer");
            if (backer == null) return;

            if (backer.localScale.x < 0.36f)
            {
                backer.localScale = new Vector3(0.38f, 0.22f, 0.004f);
            }

            Renderer rend = backer.GetComponent<Renderer>();
            if (rend == null || rend.material == null) return;

            Material mat = rend.material;
            mat.color = new Color(0.03f, 0.07f, 0.13f, 0.52f);

            // Standard shader transparency
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.SetFloat("_Glossiness", 0.75f);
            mat.SetFloat("_Metallic", 0.05f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.SetOverrideTag("RenderType", "Transparent");

            // URP Lit shader transparency if present
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1.0f);
                mat.SetFloat("_Blend", 0.0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }

        public void EnsureSignboardDecorations()
        {
            GameObject card = GameObject.Find("Guided Instruction Card");
            if (card == null) return;

            Transform root = card.transform;

            // Add Glowing Holographic Borders if missing
            if (root.Find("BorderTop") == null)
            {
                Material borderMat = CreateRuntimeGlowMat("Instruction Glow Border", new Color(0.18f, 0.78f, 1.0f, 0.85f));
                float w = 0.38f;
                float h = 0.22f;
                float t = 0.0025f;
                float halfW = w * 0.5f;
                float halfH = h * 0.5f;

                SpawnFrameEdge(root, "BorderTop", new Vector3(0, halfH, -0.0025f), new Vector3(w + t, t, 0.003f), borderMat);
                SpawnFrameEdge(root, "BorderBottom", new Vector3(0, -halfH, -0.0025f), new Vector3(w + t, t, 0.003f), borderMat);
                SpawnFrameEdge(root, "BorderLeft", new Vector3(-halfW, 0, -0.0025f), new Vector3(t, h, 0.003f), borderMat);
                SpawnFrameEdge(root, "BorderRight", new Vector3(halfW, 0, -0.0025f), new Vector3(t, h, 0.003f), borderMat);

                // Divider line between title and instructions
                SpawnFrameEdge(root, "DividerLine", new Vector3(0f, 0.046f, -0.003f), new Vector3(0.34f, 0.0012f, 0.002f), borderMat);

                // Card badge
                if (root.Find("Card Badge") == null)
                {
                    GameObject badgeObj = new GameObject("Card Badge");
                    badgeObj.transform.SetParent(root, false);
                    badgeObj.transform.localPosition = new Vector3(-0.170f, 0.088f, -0.004f);
                    TextMesh tm = badgeObj.AddComponent<TextMesh>();
                    tm.text = "[ BENCHFLOW XR  •  LESSON 1 ]";
                    tm.color = new Color(0.22f, 0.75f, 1.0f);
                    tm.fontSize = 64;
                    tm.characterSize = 0.0012f;
                    tm.fontStyle = FontStyle.Bold;
                    tm.anchor = TextAnchor.MiddleLeft;
                }
            }
        }

        public void EnsureButtonNameplates()
        {
            // 1. RUN Button
            GameObject runBtn = runButtonObject ?? GameObject.Find("3D RUN Button") ?? GameObject.Find("RUN Button");
            if (runBtn != null && runBtn.transform.Find("RUN_Nameplate") == null)
            {
                AddRuntimeNameplate(runBtn.transform, "RUN", "SIMULATE", new Color(0.15f, 0.90f, 1.0f), new Vector3(0f, 0.004f, -0.022f));
            }

            // 2. RESET Button
            GameObject resetBtn = resetButtonObject ?? GameObject.Find("3D RESET Button") ?? GameObject.Find("RESET Button");
            if (resetBtn != null && resetBtn.transform.Find("RESET_Nameplate") == null)
            {
                AddRuntimeNameplate(resetBtn.transform, "RESET", "RESTART", new Color(0.95f, 0.45f, 0.35f), new Vector3(0f, 0.004f, -0.020f));
            }

            // 3. UNDO Button
            GameObject undoBtn = undoButtonObject ?? GameObject.Find("3D UNDO Button") ?? GameObject.Find("UNDO Button");
            if (undoBtn != null && undoBtn.transform.Find("UNDO_Nameplate") == null)
            {
                AddRuntimeNameplate(undoBtn.transform, "UNDO", "LAST WIRE", new Color(0.35f, 0.75f, 1.0f), new Vector3(0f, 0.004f, -0.020f));
            }

            // 4. Workbench Control Console Dock
            if (GameObject.Find("Workbench Control Console") == null && runBtn != null)
            {
                GameObject dock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dock.name = "Workbench Control Console";
                dock.transform.position = new Vector3(0.01f, 0.495f + 0.003f, -0.185f);
                dock.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
                dock.transform.localScale = new Vector3(0.24f, 0.006f, 0.075f);
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (sh != null)
                {
                    Material m = new Material(sh) { color = new Color(0.06f, 0.08f, 0.11f) };
                    dock.GetComponent<Renderer>().sharedMaterial = m;
                }
                Destroy(dock.GetComponent<Collider>());
            }
        }

        void SpawnFrameEdge(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = name;
            edge.transform.SetParent(parent, false);
            edge.transform.localPosition = localPos;
            edge.transform.localRotation = Quaternion.identity;
            edge.transform.localScale = localScale;
            if (mat != null) edge.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(edge.GetComponent<Collider>());
        }

        void AddRuntimeNameplate(Transform parent, string label, string subtitle, Color labelColor, Vector3 localOffset)
        {
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = label + "_Nameplate";
            plate.transform.SetParent(parent, false);
            plate.transform.localPosition = localOffset;
            plate.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);
            plate.transform.localScale = new Vector3(0.038f, 0.012f, 0.002f);
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (sh != null)
            {
                Material m = new Material(sh) { color = new Color(0.05f, 0.07f, 0.10f) };
                plate.GetComponent<Renderer>().sharedMaterial = m;
            }
            Destroy(plate.GetComponent<Collider>());

            // Main Label Text
            GameObject textObj = new GameObject(label + "_Label");
            textObj.transform.SetParent(parent, false);
            textObj.transform.localPosition = localOffset + new Vector3(0f, 0.0016f, -0.0018f);
            textObj.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);
            TextMesh tm = textObj.AddComponent<TextMesh>();
            tm.text = label;
            tm.color = labelColor;
            tm.fontSize = 64;
            tm.characterSize = 0.00155f;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;

            // Subtitle Description
            if (!string.IsNullOrEmpty(subtitle))
            {
                GameObject subObj = new GameObject(label + "_Sub");
                subObj.transform.SetParent(parent, false);
                subObj.transform.localPosition = localOffset + new Vector3(0f, -0.0030f, -0.0018f);
                subObj.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);
                TextMesh subTm = subObj.AddComponent<TextMesh>();
                subTm.text = subtitle;
                subTm.color = new Color(0.65f, 0.70f, 0.78f);
                subTm.fontSize = 64;
                subTm.characterSize = 0.00090f;
                subTm.anchor = TextAnchor.MiddleCenter;
                subTm.alignment = TextAlignment.Center;
            }
        }

        Material CreateRuntimeGlowMat(string name, Color color)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            Material mat = new Material(sh) { name = name, color = color };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.0f);
            return mat;
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
                    bool anodeAt14c = led.terminalHoleA.row == 14 && led.terminalHoleA.column == 2 && led.terminalHoleA.side == BreadboardSide.SideA;
                    bool cathodeAt16c = led.terminalHoleB.row == 16 && led.terminalHoleB.column == 2 && led.terminalHoleB.side == BreadboardSide.SideA;
                    bool anodeAt16c = led.terminalHoleA.row == 16 && led.terminalHoleA.column == 2 && led.terminalHoleA.side == BreadboardSide.SideA;
                    bool cathodeAt14c = led.terminalHoleB.row == 14 && led.terminalHoleB.column == 2 && led.terminalHoleB.side == BreadboardSide.SideA;
                    if ((anodeAt14c && cathodeAt16c) || (anodeAt16c && cathodeAt14c))
                    {
                        if (anodeAt14c && cathodeAt16c)
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
                        latestFeedback = "Move the two LED legs into holes 14c and 16c.";
                    }
                }
            }

            // Check Step 1: Resistor placed
            if (currentStep == 1)
            {
                if (res != null && res.isSnapped)
                {
                    bool correct = (res.terminalHoleA.row == 8 && res.terminalHoleA.column == 3 && res.terminalHoleA.side == BreadboardSide.SideA &&
                                    res.terminalHoleB.row == 14 && res.terminalHoleB.column == 3 && res.terminalHoleB.side == BreadboardSide.SideA) ||
                                   (res.terminalHoleB.row == 8 && res.terminalHoleB.column == 3 && res.terminalHoleB.side == BreadboardSide.SideA &&
                                    res.terminalHoleA.row == 14 && res.terminalHoleA.column == 3 && res.terminalHoleA.side == BreadboardSide.SideA);
                    if (correct)
                    {
                        latestFeedback = "220 Ω resistor placed between rows 8 and 14.";
                        currentStep = 2;
                    }
                    else
                    {
                        latestFeedback = "Place the resistor legs in holes 8d and 14d.";
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
                else if (!d13Connected)
                {
                    latestFeedback = "Connect Arduino Pin 13 to the glowing 8b target.";
                }
                else
                {
                    latestFeedback = "Connect Arduino GND to the glowing 16a target.";
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

            // Keep control console buttons visible throughout the entire lesson
            if (runButtonObject != null)
            {
                runButtonObject.SetActive(true);
            }

            // Code panel visible in step 3
            if (codeDisplayNode != null)
            {
                codeDisplayNode.gameObject.SetActive(currentStep == 3 || isProgramRunning);
                if (codeDisplayNode.gameObject.activeSelf)
                    codeDisplayNode.text = ArduinoBlinkCode;
            }

            // Beautifully formatted titles and instructions
            switch (currentStep)
            {
                case 0:
                    SetText(stepTitleText, "STEP 1 OF 4: PLACE THE RED LED");
                    SetText(stepInstructionText,
                        "• Long leg  (Anode +)   ➜  Hole 14c\n" +
                        "• Short leg (Cathode -) ➜  Hole 16c\n" +
                        "• Follow the glowing blue target markers.");
                    break;
                case 1:
                    SetText(stepTitleText, "STEP 2 OF 4: ADD 220 Ω RESISTOR");
                    SetText(stepInstructionText,
                        "• Lead A (either end)   ➜  Hole 8d\n" +
                        "• Lead B (other end)    ➜  Hole 14d\n" +
                        "• Limits current to protect the LED.");
                    break;
                case 2:
                    SetText(stepTitleText, "STEP 3 OF 4: CONNECT JUMPER WIRES");
                    SetText(stepInstructionText,
                        "• Wire 1: Arduino Pin 13  ➜  Hole 8b\n" +
                        "• Wire 2: Arduino GND     ➜  Hole 16a\n" +
                        "• Click or touch endpoints to route wire.");
                    break;
                case 3:
                    if (isProgramRunning)
                    {
                        SetText(stepTitleText, "CIRCUIT SIMULATION: RUNNING");
                        SetText(stepInstructionText,
                            "✓ Arduino sketch uploaded & executing!\n" +
                            "• Current path: Pin 13 → Resistor → LED → GND\n" +
                            "• Tap STOP on console to pause.");
                    }
                    else
                    {
                        SetText(stepTitleText, "STEP 4 OF 4: RUN SIMULATION");
                        SetText(stepInstructionText,
                            "• Circuit is complete and verified!\n" +
                            "• Tap [RUN] on the control console\n" +
                            "  to compile and execute Arduino sketch.");
                    }
                    break;
            }

            if (statusText != null)
            {
                if (string.IsNullOrEmpty(latestFeedback))
                {
                    latestFeedback = "Follow the glowing targets on the breadboard.";
                }
                statusText.text = latestFeedback;
            }

            if (runButtonText != null)
            {
                runButtonText.text = isProgramRunning ? "STOP" : "RUN";
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

        public void OnUndoButtonPressed()
        {
            if (workbench != null) workbench.UndoLastWire();
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
