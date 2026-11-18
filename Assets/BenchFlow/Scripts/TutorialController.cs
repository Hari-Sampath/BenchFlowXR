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
        public GameObject landingPanel;
        public GameObject guidedCard;
        GameObject arduinoPinLabelsRoot;

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
            RemoveLandingPanelBlockingColliders();
            if (landingPanel != null) landingPanel.SetActive(true);
            if (guidedCard != null) guidedCard.SetActive(false);
            UpdateStepUI();
        }

        void RemoveLandingPanelBlockingColliders()
        {
            if (landingPanel == null) return;

            // The backplate is decorative. Its collider sits in front of the start
            // button and otherwise consumes desktop/VR pointer hits before the button.
            foreach (Collider col in landingPanel.GetComponentsInChildren<Collider>(true))
            {
                WorkbenchButton action = col.GetComponentInParent<WorkbenchButton>();
                if (action == null || action.actionType != WorkbenchButtonAction.StartLesson)
                    Destroy(col);
            }
        }

        public void BeginLesson()
        {
            if (landingPanel != null) landingPanel.SetActive(false);
            if (guidedCard != null) guidedCard.SetActive(true);
            currentStep = 0;
            latestFeedback = "Follow the glowing targets on the breadboard.";
            UpdateStepUI();
        }

        public void AutoWireReferences()
        {
            if (workbench == null) workbench = FindAnyObjectByType<WorkbenchState>();
            if (breadboard == null) breadboard = FindAnyObjectByType<BreadboardGrid>();
            if (blinkAnimator == null) blinkAnimator = FindAnyObjectByType<BlinkAnimator>();

            if (stepInstructionText == null)
            {
                TextMesh[] texts = FindObjectsByType<TextMesh>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
        /// adds holographic borders and badges, labels workbench console buttons, and styles hole guidance markers.
        /// </summary>
        public void EnsureVisualPolish()
        {
            EnsureSignboardTranslucent();
            EnsureSignboardDecorations();
            EnsureButtonNameplates();
            EnsureArduinoPinLabels();
            EnsureHoleMarkersDark();
        }

        public void EnsureHoleMarkersDark()
        {
            if (breadboard != null)
            {
                if (markerHole14c != null && breadboard.TryParseHole("14c", out var h14c)) markerHole14c.transform.position = breadboard.HolePosition(h14c);
                if (markerHole16c != null && breadboard.TryParseHole("16c", out var h16c)) markerHole16c.transform.position = breadboard.HolePosition(h16c);
                if (markerHole8d != null && breadboard.TryParseHole("8d", out var h8d)) markerHole8d.transform.position = breadboard.HolePosition(h8d);
                if (markerHole14d != null && breadboard.TryParseHole("14d", out var h14d)) markerHole14d.transform.position = breadboard.HolePosition(h14d);
                if (markerHole8b != null && breadboard.TryParseHole("8b", out var h8b)) markerHole8b.transform.position = breadboard.HolePosition(h8b);
                if (markerHole16a != null && breadboard.TryParseHole("16a", out var h16a)) markerHole16a.transform.position = breadboard.HolePosition(h16a);
            }

            GameObject[] markers = new GameObject[]
            {
                markerHole14c, markerHole16c,
                markerHole8d, markerHole14d,
                markerHole8b, markerHole16a,
                markerPin13, markerPinGnd
            };

            Color darkBlue = new Color(0.04f, 0.22f, 0.85f, 1f);
            Material darkBlueMat = CreateRuntimeGlowMat("DarkBlueHoleGlow", darkBlue);

            foreach (var m in markers)
            {
                if (m == null) continue;

                // Remove any colliders from guidance markers so they never block raycasts or part placements
                foreach (var col in m.GetComponentsInChildren<Collider>())
                {
                    Destroy(col);
                }

                // Configure or convert marker children to flat hole socket discs
                Transform glowChild = m.transform.Find("GlowSphere") ?? m.transform.Find("HoleGlowDisc");
                if (glowChild != null)
                {
                    glowChild.name = "HoleGlowDisc";
                    // Flatten into the hole socket opening so only the hole glows
                    glowChild.localPosition = new Vector3(0f, 0.0004f, 0f);
                    glowChild.localScale = new Vector3(0.0022f, 0.0003f, 0.0022f);

                    Renderer r = glowChild.GetComponent<Renderer>();
                    if (r != null) r.sharedMaterial = darkBlueMat;

                    HologramPulse pulse = glowChild.GetComponent<HologramPulse>();
                    if (pulse == null) pulse = glowChild.gameObject.AddComponent<HologramPulse>();
                    pulse.glowColor = darkBlue;
                    pulse.baseScale = 0.0022f;
                    pulse.pulseScaleRange = 0.0001f;
                    pulse.pulseSpeed = 2.2f;
                }
                else
                {
                    // Direct anchor (e.g. Arduino Pin 13 / GND)
                    Renderer r = m.GetComponent<Renderer>();
                    if (r != null) r.sharedMaterial = darkBlueMat;

                    HologramPulse pulse = m.GetComponent<HologramPulse>();
                    if (pulse == null && r != null) pulse = m.AddComponent<HologramPulse>();
                    if (pulse != null)
                    {
                        pulse.glowColor = darkBlue;
                        pulse.baseScale = 0.003f;
                        pulse.pulseScaleRange = 0.0002f;
                        pulse.pulseSpeed = 2.2f;
                    }
                }
            }
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
            if (runBtn != null)
            {
                RemoveButtonTextAndNameplates(runBtn);
                // Remove root colliders so clicking the button housing, dock, or text plate in front does NOT actuate
                foreach (var col in runBtn.GetComponents<Collider>()) Destroy(col);

                Transform cap = runBtn.transform.Find("RunCap") ?? runBtn.transform.Find("BtnCap");
                if (cap != null)
                {
                    cap.name = "RunCap";
                    // Only the 3D moving cylinder cap is the physical actuator
                    if (cap.GetComponent<Collider>() == null) cap.gameObject.AddComponent<CapsuleCollider>();

                    WorkbenchButton wb = cap.GetComponent<WorkbenchButton>() ?? cap.gameObject.AddComponent<WorkbenchButton>();
                    wb.actionType = WorkbenchButtonAction.Run;
                    wb.movingCap = cap;
                    wb.tutorial = this;
                    wb.workbench = workbench;
                }

                // Attach button controller to root as well for reference
                WorkbenchButton rootWb = runBtn.GetComponent<WorkbenchButton>() ?? runBtn.AddComponent<WorkbenchButton>();
                rootWb.actionType = WorkbenchButtonAction.Run;
                rootWb.movingCap = cap;
                rootWb.tutorial = this;
                rootWb.workbench = workbench;

                // Strip any colliders from children other than the moving cap
                foreach (var col in runBtn.GetComponentsInChildren<Collider>())
                {
                    if (cap != null && col.transform != cap) Destroy(col);
                }

                AddRuntimeNameplate(runBtn.transform, "RUN", "", new Color(0.25f, 0.88f, 1f),
                    new Vector3(0f, -0.002f, -0.032f));
            }

            // 2. RESET Button
            GameObject resetBtn = resetButtonObject ?? GameObject.Find("3D RESET Button") ?? GameObject.Find("RESET Button");
            if (resetBtn != null)
            {
                RemoveButtonTextAndNameplates(resetBtn);
                // Remove root colliders so clicking the text plate in front does NOT actuate
                foreach (var col in resetBtn.GetComponents<Collider>()) Destroy(col);

                Transform cap = resetBtn.transform.Find("ResetCap");
                if (cap != null)
                {
                    // Only the 3D moving cylinder cap is the physical actuator
                    if (cap.GetComponent<Collider>() == null) cap.gameObject.AddComponent<CapsuleCollider>();

                    WorkbenchButton wb = cap.GetComponent<WorkbenchButton>() ?? cap.gameObject.AddComponent<WorkbenchButton>();
                    wb.actionType = WorkbenchButtonAction.Reset;
                    wb.movingCap = cap;
                    wb.tutorial = this;
                    wb.workbench = workbench;
                }

                WorkbenchButton rootWb = resetBtn.GetComponent<WorkbenchButton>() ?? resetBtn.AddComponent<WorkbenchButton>();
                rootWb.actionType = WorkbenchButtonAction.Reset;
                rootWb.movingCap = cap;
                rootWb.tutorial = this;
                rootWb.workbench = workbench;

                foreach (var col in resetBtn.GetComponentsInChildren<Collider>())
                {
                    if (cap != null && col.transform != cap) Destroy(col);
                }

                AddRuntimeNameplate(resetBtn.transform, "RESET", "", new Color(1f, 0.55f, 0.48f),
                    new Vector3(0f, -0.002f, -0.030f));
            }

            // 3. UNDO Button
            GameObject undoBtn = undoButtonObject ?? GameObject.Find("3D UNDO Button") ?? GameObject.Find("UNDO Button");
            if (undoBtn != null)
            {
                RemoveButtonTextAndNameplates(undoBtn);
                // Remove root colliders so clicking the text plate in front does NOT actuate
                foreach (var col in undoBtn.GetComponents<Collider>()) Destroy(col);

                Transform cap = undoBtn.transform.Find("UndoCap");
                if (cap != null)
                {
                    // Only the 3D moving cylinder cap is the physical actuator
                    if (cap.GetComponent<Collider>() == null) cap.gameObject.AddComponent<CapsuleCollider>();

                    WorkbenchButton wb = cap.GetComponent<WorkbenchButton>() ?? cap.gameObject.AddComponent<WorkbenchButton>();
                    wb.actionType = WorkbenchButtonAction.Undo;
                    wb.movingCap = cap;
                    wb.tutorial = this;
                    wb.workbench = workbench;
                }

                WorkbenchButton rootWb = undoBtn.GetComponent<WorkbenchButton>() ?? undoBtn.AddComponent<WorkbenchButton>();
                rootWb.actionType = WorkbenchButtonAction.Undo;
                rootWb.movingCap = cap;
                rootWb.tutorial = this;
                rootWb.workbench = workbench;

                foreach (var col in undoBtn.GetComponentsInChildren<Collider>())
                {
                    if (cap != null && col.transform != cap) Destroy(col);
                }

                AddRuntimeNameplate(undoBtn.transform, "UNDO", "", new Color(0.48f, 0.72f, 1f),
                    new Vector3(0f, -0.002f, -0.030f));
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
            Quaternion facing = GetConsoleLabelFacing(plate.transform.position);
            plate.transform.rotation = facing;
            plate.transform.localScale = new Vector3(label == "RESET" ? 0.046f : 0.040f, 0.010f, 0.0016f);
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
            textObj.transform.rotation = GetConsoleLabelFacing(textObj.transform.position);
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
                subObj.transform.rotation = GetConsoleLabelFacing(subObj.transform.position);
                TextMesh subTm = subObj.AddComponent<TextMesh>();
                subTm.text = subtitle;
                subTm.color = new Color(0.65f, 0.70f, 0.78f);
                subTm.fontSize = 64;
                subTm.characterSize = 0.00090f;
                subTm.anchor = TextAnchor.MiddleCenter;
                subTm.alignment = TextAlignment.Center;
            }
        }

        Quaternion GetConsoleLabelFacing(Vector3 labelPosition)
        {
            Camera viewer = Camera.main;
            if (viewer == null) return Quaternion.Euler(38f, 0f, 0f);
            Vector3 towardViewer = viewer.transform.position - labelPosition;
            // TextMesh's front face points opposite the workbench camera-facing
            // direction here. Turn the plane toward the viewer while retaining
            // the viewer's up axis so the glyphs read left-to-right and upright.
            Quaternion cameraFacing = Quaternion.LookRotation(towardViewer.normalized, viewer.transform.up);
            return cameraFacing * Quaternion.Euler(0f, 180f, 0f);
        }

        void EnsureArduinoPinLabels()
        {
            if (markerPin13 == null || markerPinGnd == null) return;

            // The original labels were standalone objects, so marker visibility did not
            // hide them on the landing screen. Replace them with a grouped, step-gated pair.
            GameObject oldPin13Label = GameObject.Find("Text 13");
            GameObject oldGndLabel = GameObject.Find("Text GND");
            if (oldPin13Label != null) Destroy(oldPin13Label);
            if (oldGndLabel != null) Destroy(oldGndLabel);

            if (arduinoPinLabelsRoot == null)
            {
                arduinoPinLabelsRoot = GameObject.Find("Arduino Terminal Labels");
                if (arduinoPinLabelsRoot == null)
                {
                    arduinoPinLabelsRoot = new GameObject("Arduino Terminal Labels");
                    arduinoPinLabelsRoot.SetActive(false);
                    CreateArduinoTerminalLabel(arduinoPinLabelsRoot.transform, "D13", markerPin13.transform.position,
                        new Color(0.28f, 0.88f, 1f));
                    CreateArduinoTerminalLabel(arduinoPinLabelsRoot.transform, "GND", markerPinGnd.transform.position,
                        new Color(0.62f, 0.82f, 1f));
                }
            }
        }

        void CreateArduinoTerminalLabel(Transform parent, string label, Vector3 terminalPosition, Color textColor)
        {
            Vector3 tagPosition = terminalPosition + Vector3.right * 0.019f + Vector3.up * 0.0014f;

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = label + " Terminal Plate";
            plate.transform.SetParent(parent, false);
            plate.transform.position = tagPosition;
            plate.transform.rotation = Quaternion.identity;
            plate.transform.localScale = new Vector3(0.025f, 0.0012f, 0.008f);
            Renderer plateRenderer = plate.GetComponent<Renderer>();
            plateRenderer.sharedMaterial = CreateRuntimeGlowMat(label + " Terminal Plate Material",
                new Color(0.025f, 0.065f, 0.10f));
            Destroy(plate.GetComponent<Collider>());

            GameObject accent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            accent.name = label + " Terminal Accent";
            accent.transform.SetParent(parent, false);
            accent.transform.position = tagPosition + Vector3.left * 0.0114f + Vector3.up * 0.0008f;
            accent.transform.rotation = Quaternion.identity;
            accent.transform.localScale = new Vector3(0.0013f, 0.0005f, 0.006f);
            accent.GetComponent<Renderer>().sharedMaterial = CreateRuntimeGlowMat(label + " Terminal Accent Material", textColor);
            Destroy(accent.GetComponent<Collider>());

            GameObject textObject = new GameObject(label + " Terminal Label");
            textObject.transform.SetParent(parent, false);
            textObject.transform.position = tagPosition + Vector3.up * 0.00075f;
            textObject.transform.rotation = GetConsoleLabelFacing(textObject.transform.position);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = label;
            text.color = textColor;
            text.fontSize = 64;
            text.characterSize = 0.00082f;
            text.fontStyle = FontStyle.Bold;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
        }

        void RemoveButtonTextAndNameplates(GameObject button)
        {
            foreach (Transform child in button.GetComponentsInChildren<Transform>(true))
            {
                if (child == button.transform) continue;
                if (child.name.EndsWith("_Nameplate", System.StringComparison.Ordinal) || child.GetComponent<TextMesh>() != null)
                    Destroy(child.gameObject);
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
                            latestFeedback = "LED placed correctly (Anode at 14c, Cathode at 16c). Locked in place.";
                            currentStep = 1;
                            led.isLocked = true;
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
                        latestFeedback = "220 Ω resistor placed between rows 8 and 14. Locked in place.";
                        currentStep = 2;
                        res.isLocked = true;
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
            if (arduinoPinLabelsRoot != null)
                arduinoPinLabelsRoot.SetActive(currentStep == 2);

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

            SetButtonNameplatesVisible(landingPanel == null || !landingPanel.activeInHierarchy);
        }

        void SetButtonNameplatesVisible(bool visible)
        {
            GameObject[] buttons =
            {
                runButtonObject,
                resetButtonObject ?? GameObject.Find("3D RESET Button") ?? GameObject.Find("RESET Button"),
                undoButtonObject ?? GameObject.Find("3D UNDO Button") ?? GameObject.Find("UNDO Button")
            };

            foreach (GameObject button in buttons)
            {
                if (button == null) continue;
                foreach (Transform child in button.GetComponentsInChildren<Transform>(true))
                {
                    if (child == button.transform) continue;
                    if (child.name.EndsWith("_Nameplate", StringComparison.Ordinal) ||
                        child.name.EndsWith("_Label", StringComparison.Ordinal) ||
                        child.name.EndsWith("_Sub", StringComparison.Ordinal))
                    {
                        child.gameObject.SetActive(visible);
                    }
                }
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
            if (workbench != null)
            {
                if (workbench.led != null) workbench.led.isLocked = false;
                if (workbench.resistor != null) workbench.resistor.isLocked = false;
            }
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
