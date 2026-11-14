using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BenchFlow.Editor
{
    /// <summary>
    /// Builds the complete VR and Desktop interactive workbench scene for BenchFlow XR.
    /// Follows the reference video design: breadboard is scaled and brought close to the foreground,
    /// 3D discrete components, VR 6-DOF hand tracking, interactive wiring, and live Arduino Blink simulation.
    /// </summary>
    public static class BenchFlowSceneBuilder
    {
        const string Models = "Assets/BenchFlow/Models/External/";
        const float DeskTopY = 0.495f;
        const float BoardScale = 1.65f; // Scaled up so breadboard is prominent, clear, and easily targetable

        [MenuItem("BenchFlow/Create VR Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Realistic workbench ambient lighting
            RenderSettings.ambientLight = new Color(0.42f, 0.46f, 0.52f);
            RenderSettings.ambientIntensity = 0.55f;

            // 1. Workbench Desk Model
            GameObject workbench = PlaceModel("Workbench Model (CC0)", Models + "Workbench_OBJ/workbench.obj",
                new Vector3(0, 0, 0), Quaternion.identity, Vector3.one * 0.55f);

            // Shaders and Materials
            Material blueGlow = MakeMaterial("Tutorial Hologram Blue", new Color(0.10f, 0.70f, 1f), true);
            Material cyanGlow = MakeMaterial("Pin Label Cyan", new Color(0.30f, 0.90f, 1f), true);
            Material panelMat = MakeMaterial("Instruction Glass", new Color(0.02f, 0.05f, 0.08f, 0.94f));
            Material trayMat = MakeMaterial("Parts Tray", new Color(0.05f, 0.07f, 0.09f));
            Material ceramicMat = MakeMaterial("Resistor Ceramic", new Color(0.85f, 0.74f, 0.58f));
            Material wireMetalMat = MakeMaterial("Metal Lead", new Color(0.75f, 0.77f, 0.80f));
            Material ledRedOff = MakeMaterial("LED Red Off", new Color(0.6f, 0.05f, 0.05f, 0.9f));
            Material ledRedLit = MakeMaterial("LED Red Lit", new Color(1f, 0.15f, 0.15f, 1f), true);
            Material runBtnMat = MakeMaterial("Run Button Mat", new Color(0.08f, 0.55f, 0.85f), true);

            // 2. Breadboard Object & Grid (Enlarged & brought close to foreground, matching reference video)
            GameObject breadboardObject = new GameObject("Breadboard | Electrical Grid");
            breadboardObject.transform.position = new Vector3(0.01f, DeskTopY, -0.065f);
            breadboardObject.transform.localScale = Vector3.one * BoardScale;

            BreadboardGrid grid = breadboardObject.AddComponent<BreadboardGrid>();
            grid.rowCount = 63;
            grid.columnsPerSide = 5;
            grid.holeSpacing = 0.00254f;
            grid.rowSpacing = 0.00254f;
            grid.centerGap = 0.00762f;
            grid.railOffset = 0.0215f;
            grid.surfaceY = 0.0135f;

            GameObject breadboardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Breadboard_OBJ/breadboard.obj");
            if (breadboardAsset != null)
            {
                GameObject boardModel = (GameObject)PrefabUtility.InstantiatePrefab(breadboardAsset);
                boardModel.name = "Breadboard Model (CC0)";
                boardModel.transform.SetParent(breadboardObject.transform, false);
                boardModel.transform.localPosition = Vector3.zero;
                boardModel.transform.localRotation = Quaternion.identity;
                boardModel.transform.localScale = Vector3.one;
            }

            BoxCollider gridCollider = breadboardObject.AddComponent<BoxCollider>();
            gridCollider.center = new Vector3(0, 0.01f, 0);
            gridCollider.size = new Vector3(0.165f, 0.025f, 0.055f);

            // 3. Arduino-Compatible Controller Board (at left of breadboard)
            GameObject controller = PlaceModel("Controller Board (Generic, CC0)", Models + "CircuitBoard_OBJ/circuit-board.obj",
                new Vector3(-0.21f, DeskTopY, -0.06f), Quaternion.Euler(0, 90, 0), Vector3.one * 1.35f);
            if (controller != null)
            {
                controller.AddComponent<BoxCollider>().size = new Vector3(0.10f, 0.025f, 0.065f);
            }

            // Arduino Pin Anchors
            GameObject pin13 = MakeAnchor("UNO Digital Pin 13", new Vector3(-0.165f, DeskTopY + 0.028f, -0.075f), cyanGlow);
            GameObject pinGnd = MakeAnchor("UNO GND", new Vector3(-0.165f, DeskTopY + 0.028f, -0.035f), cyanGlow);
            MakePinLabel("13", pin13.transform.position + Vector3.up * 0.016f, cyanGlow);
            MakePinLabel("GND", pinGnd.transform.position + Vector3.up * 0.016f, cyanGlow);

            // Arduino Built-in D13 LED Indicator
            GameObject unoLedObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            unoLedObj.name = "Arduino D13 Built-in LED";
            unoLedObj.transform.position = new Vector3(-0.185f, DeskTopY + 0.026f, -0.075f);
            unoLedObj.transform.localScale = new Vector3(0.004f, 0.002f, 0.004f);
            Object.DestroyImmediate(unoLedObj.GetComponent<Collider>());
            Renderer unoLedRenderer = unoLedObj.GetComponent<Renderer>();
            Material unoLedOff = MakeMaterial("Uno LED Off", new Color(0.3f, 0.25f, 0.05f));
            Material unoLedLit = MakeMaterial("Uno LED Lit", new Color(1f, 0.85f, 0.1f), true);
            unoLedRenderer.sharedMaterial = unoLedOff;

            // 4. Parts Tray (on the right)
            GameObject tray = Primitive("Parts Tray", PrimitiveType.Cube,
                new Vector3(0.22f, DeskTopY + 0.005f, -0.06f),
                new Vector3(0.13f, 0.008f, 0.16f), trayMat);

            // 5. Discrete 220 Ω Resistor Component (proportioned to BoardScale)
            CircuitPart resistor = CreateDiscreteResistor(
                "Resistor 220 Ohm",
                new Vector3(0.22f, DeskTopY + 0.018f, -0.10f),
                ceramicMat, wireMetalMat, BoardScale);

            // 6. Discrete Red 5mm LED Component (proportioned to BoardScale)
            CircuitPart led = CreateDiscreteLed(
                "Red LED",
                new Vector3(0.22f, DeskTopY + 0.018f, -0.02f),
                ledRedOff, wireMetalMat, BoardScale);

            // Light for when the LED lights up
            GameObject ledLightObj = new GameObject("LED Light Source");
            ledLightObj.transform.SetParent(led.transform, false);
            ledLightObj.transform.localPosition = new Vector3(0f, 0.016f, 0f);
            Light ledPointLight = ledLightObj.AddComponent<Light>();
            ledPointLight.type = LightType.Point;
            ledPointLight.color = new Color(1f, 0.2f, 0.2f);
            ledPointLight.intensity = 1.0f;
            ledPointLight.range = 0.18f;
            ledPointLight.enabled = false;

            // 7. Workbench State & Simulation Systems
            GameObject stateObj = new GameObject("Workbench Systems");
            WorkbenchState workbenchState = stateObj.AddComponent<WorkbenchState>();
            workbenchState.breadboard = grid;
            workbenchState.resistor = resistor;
            workbenchState.led = led;

            BlinkAnimator blinkAnimator = stateObj.AddComponent<BlinkAnimator>();
            blinkAnimator.ledRenderer = led.GetComponentInChildren<Renderer>();
            blinkAnimator.ledLight = ledPointLight;
            blinkAnimator.ledLitMaterial = ledRedLit;
            blinkAnimator.ledOffMaterial = ledRedOff;
            blinkAnimator.unoD13LedRenderer = unoLedRenderer;
            blinkAnimator.unoLedLitMaterial = unoLedLit;
            blinkAnimator.unoLedOffMaterial = unoLedOff;
            workbenchState.blinkAnimator = blinkAnimator;

            TutorialController tutorial = stateObj.AddComponent<TutorialController>();
            tutorial.workbench = workbenchState;
            tutorial.breadboard = grid;
            tutorial.blinkAnimator = blinkAnimator;
            workbenchState.tutorial = tutorial;

            // Audio & Holographic Ghost Systems
            SoundManager soundManager = stateObj.AddComponent<SoundManager>();
            GhostPreview ghostPreview = stateObj.AddComponent<GhostPreview>();
            ghostPreview.SetupGhosts(grid, BoardScale);

            // 8. Holographic Guided Markers & Labels (dynamically placed at scaled breadboard holes)
            // Step 1: LED (14c & 16c)
            GameObject marker14c = MakeTargetMarker("Marker 14c", grid.HolePosition(grid.GetHole("14c")), blueGlow, "14c", BoardScale);
            GameObject marker16c = MakeTargetMarker("Marker 16c", grid.HolePosition(grid.GetHole("16c")), blueGlow, "16c", BoardScale);
            tutorial.markerHole14c = marker14c;
            tutorial.markerHole16c = marker16c;

            // Step 2: Resistor (8d & 14d)
            GameObject marker8d = MakeTargetMarker("Marker 8d", grid.HolePosition(grid.GetHole("8d")), blueGlow, "8d", BoardScale);
            GameObject marker14d = MakeTargetMarker("Marker 14d", grid.HolePosition(grid.GetHole("14d")), blueGlow, "14d", BoardScale);
            tutorial.markerHole8d = marker8d;
            tutorial.markerHole14d = marker14d;

            // Step 3: Wires (Pin 13 -> 8b, GND -> 16a)
            GameObject marker8b = MakeTargetMarker("Marker 8b", grid.HolePosition(grid.GetHole("8b")), blueGlow, "8b", BoardScale);
            GameObject marker16a = MakeTargetMarker("Marker 16a", grid.HolePosition(grid.GetHole("16a")), blueGlow, "16a", BoardScale);
            tutorial.markerPin13 = pin13;
            tutorial.markerHole8b = marker8b;
            tutorial.markerPinGnd = pinGnd;
            tutorial.markerHole16a = marker16a;

            // 9. Floating 3D Guided Card (tilted towards VR player)
            GameObject cardObj = BuildGuidedCard(
                new Vector3(0.01f, DeskTopY + 0.28f, 0.12f),
                Quaternion.Euler(18f, 0f, 0f),
                panelMat, cyanGlow, tutorial);

            // 10. Interactive 3D RUN & RESET Buttons
            GameObject runBtn = BuildRunButton(
                new Vector3(0.135f, DeskTopY + 0.018f, -0.130f),
                runBtnMat, tutorial);

            Material resetBtnMat = MakeMaterial("Reset Button Mat", new Color(0.25f, 0.28f, 0.32f));
            GameObject resetBtn = BuildResetButton(
                new Vector3(0.135f, DeskTopY + 0.018f, -0.075f),
                resetBtnMat, tutorial);
            tutorial.resetButtonObject = resetBtn;

            // 11. VR Rig & Player (Positioned at comfortable viewing distance from foreground breadboard)
            GameObject vrRigObj = new GameObject("[XR Rig] Player");
            vrRigObj.transform.position = new Vector3(0.01f, DeskTopY + 0.38f, -0.42f);
            VRRig vrRig = vrRigObj.AddComponent<VRRig>();
            vrRig.standingBenchOffset = new Vector3(0.01f, DeskTopY + 0.38f, -0.42f);

            GameObject cameraObj = new GameObject("CenterEyeCamera");
            cameraObj.transform.SetParent(vrRigObj.transform, false);
            cameraObj.tag = "MainCamera";
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 52f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.09f);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.Euler(36f, 0f, 0f);

            vrRig.headTransform = cameraObj.transform;
            vrRig.centerEyeCamera = cam;

            // VR Hands
            GameObject leftHandObj = new GameObject("LeftHandController");
            leftHandObj.transform.SetParent(vrRigObj.transform, false);
            leftHandObj.transform.localPosition = new Vector3(-0.16f, -0.05f, 0.26f);
            VRHandInteractor leftHand = leftHandObj.AddComponent<VRHandInteractor>();
            leftHand.handNode = UnityEngine.XR.XRNode.LeftHand;
            leftHand.workbench = workbenchState;
            leftHand.breadboard = grid;
            leftHand.tutorial = tutorial;
            vrRig.leftHand = leftHand;

            GameObject rightHandObj = new GameObject("RightHandController");
            rightHandObj.transform.SetParent(vrRigObj.transform, false);
            rightHandObj.transform.localPosition = new Vector3(0.16f, -0.05f, 0.26f);
            VRHandInteractor rightHand = rightHandObj.AddComponent<VRHandInteractor>();
            rightHand.handNode = UnityEngine.XR.XRNode.RightHand;
            rightHand.workbench = workbenchState;
            rightHand.breadboard = grid;
            rightHand.tutorial = tutorial;
            vrRig.rightHand = rightHand;

            // Desktop Preview Fallback Interactor
            DesktopPreviewInteractor desktopInteractor = stateObj.AddComponent<DesktopPreviewInteractor>();
            desktopInteractor.previewCamera = cam;
            desktopInteractor.breadboard = grid;
            desktopInteractor.workbench = workbenchState;
            desktopInteractor.tutorial = tutorial;
            desktopInteractor.resistor = resistor;
            desktopInteractor.led = led;
            desktopInteractor.unoD13 = pin13.transform;
            desktopInteractor.unoGnd = pinGnd.transform;
            vrRig.desktopPreview = desktopInteractor;

            // 12. Directional Key and Fill Lights
            GameObject keyLight = new GameObject("Workbench Key Light");
            Light key = keyLight.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.05f;
            keyLight.transform.rotation = Quaternion.Euler(50, -30, 0);

            GameObject fillLight = new GameObject("Workbench Fill Light");
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.40f;
            fillLight.transform.rotation = Quaternion.Euler(25, 140, 0);

            // Save Scene
            EditorSceneManager.SaveScene(scene, "Assets/BenchFlow/Scenes/BenchFlowPrototype.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/BenchFlow/Scenes/BenchFlowPrototype.unity", true) };
            AssetDatabase.SaveAssets();

            Debug.Log("BenchFlow VR Workbench scene successfully generated with enlarged, foreground-centered breadboard!");
        }

        static CircuitPart CreateDiscreteResistor(string name, Vector3 pos, Material bodyMat, Material leadMat, float scale)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.identity;

            float leadSpanHalf = 0.00762f * scale; // 6 rows span at scale
            float bodyRadius = 0.0032f * scale;
            float bodyLength = 0.0055f * scale;

            // Ceramic body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Resistor Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(bodyRadius, bodyLength, bodyRadius);
            body.transform.localRotation = Quaternion.Euler(0, 0, 90);
            body.transform.localPosition = new Vector3(0, 0.004f * scale, 0);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Color bands: Red, Red, Brown, Gold (220 Ω, 5%)
            Color redBand = new Color(0.85f, 0.12f, 0.12f);
            Color brownBand = new Color(0.45f, 0.22f, 0.05f);
            Color goldBand = new Color(0.85f, 0.70f, 0.20f);

            CreateBand("Band1_Red", body.transform, -0.003f, redBand);
            CreateBand("Band2_Red", body.transform, -0.001f, redBand);
            CreateBand("Band3_Brown", body.transform, 0.001f, brownBand);
            CreateBand("Band4_Gold", body.transform, 0.003f, goldBand);

            // Wire Leads
            CreateResistorLead("LeadA", root.transform, -leadSpanHalf, -bodyLength * 0.9f, leadMat, scale);
            CreateResistorLead("LeadB", root.transform, leadSpanHalf, bodyLength * 0.9f, leadMat, scale);

            // Collider for grabbing
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.004f * scale, 0);
            col.size = new Vector3(leadSpanHalf * 2.2f, 0.016f * scale, 0.014f * scale);

            CircuitPart part = root.AddComponent<CircuitPart>();
            part.componentId = "RESISTOR";
            part.isPolarized = false;
            part.terminalOffsetA = new Vector3(-leadSpanHalf, 0f, 0f);
            part.terminalOffsetB = new Vector3(leadSpanHalf, 0f, 0f);

            return part;
        }

        static void CreateBand(string name, Transform parent, float yOffset, Color color)
        {
            GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            band.name = name;
            band.transform.SetParent(parent, false);
            band.transform.localPosition = new Vector3(0, yOffset * 200f, 0);
            band.transform.localScale = new Vector3(1.05f, 0.12f, 1.05f);
            band.transform.localRotation = Quaternion.identity;
            band.GetComponent<Renderer>().sharedMaterial = MakeMaterial(name + "_Mat", color);
            Object.DestroyImmediate(band.GetComponent<Collider>());
        }

        static void CreateResistorLead(string name, Transform parent, float xPos, float xInner, Material mat, float scale)
        {
            float len = Mathf.Abs(xPos - xInner);
            GameObject horiz = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horiz.name = name + "_Horiz";
            horiz.transform.SetParent(parent, false);
            horiz.transform.localPosition = new Vector3((xPos + xInner) * 0.5f, 0.004f * scale, 0);
            horiz.transform.localScale = new Vector3(0.0007f * scale, len * 0.5f, 0.0007f * scale);
            horiz.transform.localRotation = Quaternion.Euler(0, 0, 90);
            horiz.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(horiz.GetComponent<Collider>());

            GameObject vert = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vert.name = name + "_Pin";
            vert.transform.SetParent(parent, false);
            vert.transform.localPosition = new Vector3(xPos, 0.001f * scale, 0);
            vert.transform.localScale = new Vector3(0.0007f * scale, 0.004f * scale, 0.0007f * scale);
            vert.transform.localRotation = Quaternion.identity;
            vert.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(vert.GetComponent<Collider>());
        }

        static CircuitPart CreateDiscreteLed(string name, Vector3 pos, Material domeMat, Material leadMat, float scale)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.identity;

            float leadSpanHalf = 0.00254f * scale; // 2 rows span at scale
            float domeRadius = 0.0055f * scale;
            float domeHeight = 0.008f * scale;

            // Dome
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "LED Dome";
            dome.transform.SetParent(root.transform, false);
            dome.transform.localScale = new Vector3(domeRadius, domeHeight, domeRadius);
            dome.transform.localPosition = new Vector3(0, domeHeight, 0);
            dome.GetComponent<Renderer>().sharedMaterial = domeMat;
            Object.DestroyImmediate(dome.GetComponent<Collider>());

            // Base rim
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "LED Rim";
            rim.transform.SetParent(root.transform, false);
            rim.transform.localScale = new Vector3(domeRadius * 1.15f, 0.001f * scale, domeRadius * 1.15f);
            rim.transform.localPosition = new Vector3(0, 0.004f * scale, 0);
            rim.GetComponent<Renderer>().sharedMaterial = domeMat;
            Object.DestroyImmediate(rim.GetComponent<Collider>());

            // Leads: Long leg Anode at -leadSpanHalf, Short leg Cathode at +leadSpanHalf
            CreateLedLead("AnodeLead", root.transform, -leadSpanHalf, 0.0055f * scale, leadMat, scale);
            CreateLedLead("CathodeLead", root.transform, leadSpanHalf, 0.0040f * scale, leadMat, scale);

            // Grab collider
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.006f * scale, 0);
            col.size = new Vector3(domeRadius * 2.2f, 0.020f * scale, domeRadius * 2.2f);

            CircuitPart part = root.AddComponent<CircuitPart>();
            part.componentId = "LED";
            part.isPolarized = true;
            part.terminalOffsetA = new Vector3(-leadSpanHalf, 0f, 0f); // Anode (long leg)
            part.terminalOffsetB = new Vector3(leadSpanHalf, 0f, 0f);  // Cathode (short leg)

            return part;
        }

        static void CreateLedLead(string name, Transform parent, float xPos, float length, Material mat, float scale)
        {
            GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pin.name = name;
            pin.transform.SetParent(parent, false);
            pin.transform.localPosition = new Vector3(xPos, 0.004f * scale - length * 0.5f, 0);
            pin.transform.localScale = new Vector3(0.0007f * scale, length * 0.5f, 0.0007f * scale);
            pin.transform.localRotation = Quaternion.identity;
            pin.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(pin.GetComponent<Collider>());
        }

        static GameObject MakeTargetMarker(string name, Vector3 pos, Material mat, string label, float scale)
        {
            GameObject markerRoot = new GameObject(name);
            markerRoot.transform.position = pos;

            // Pulsing sphere
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "GlowSphere";
            sphere.transform.SetParent(markerRoot.transform, false);
            sphere.transform.localPosition = Vector3.up * (0.004f * scale);
            sphere.transform.localScale = Vector3.one * (0.006f * scale);
            sphere.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            HologramPulse pulse = sphere.AddComponent<HologramPulse>();
            pulse.baseScale = 0.006f * scale;
            pulse.pulseScaleRange = 0.002f * scale;

            // Floating Label Tag
            GameObject tag = Primitive("Label_" + label, PrimitiveType.Cube,
                pos + Vector3.up * (0.022f * scale) + Vector3.back * (0.015f * scale),
                new Vector3(0.026f * scale, 0.008f * scale, 0.014f * scale),
                MakeMaterial("TagBack", new Color(0.02f, 0.12f, 0.18f, 0.95f)));
            tag.transform.SetParent(markerRoot.transform, true);
            Object.DestroyImmediate(tag.GetComponent<Collider>());

            GameObject textObj = MakeText("LabelText_" + label, label, mat.color, 0.0022f * scale,
                pos + Vector3.up * (0.026f * scale) + Vector3.back * (0.015f * scale), markerRoot.transform);
            textObj.transform.rotation = Quaternion.Euler(40, 0, 0);

            return markerRoot;
        }

        static GameObject BuildGuidedCard(Vector3 pos, Quaternion rot, Material backerMat, Material accentMat, TutorialController tutorial)
        {
            GameObject root = new GameObject("Guided Instruction Card");
            root.transform.position = pos;
            root.transform.rotation = rot;

            // Glass backer
            GameObject backer = Primitive("Card Backer", PrimitiveType.Cube, pos, new Vector3(0.32f, 0.19f, 0.006f), backerMat);
            backer.transform.SetParent(root.transform, false);
            backer.transform.localPosition = Vector3.zero;
            backer.transform.localRotation = Quaternion.identity;
            Object.DestroyImmediate(backer.GetComponent<Collider>());

            // Title
            GameObject titleObj = MakeText("Step Title", "STEP 1 OF 4\nPLACE THE RED LED", Color.white, 0.0022f,
                new Vector3(-0.145f, 0.068f, -0.004f), root.transform);
            tutorial.stepTitleText = titleObj.GetComponent<TextMesh>();
            tutorial.stepTitleText.fontStyle = FontStyle.Bold;

            // Instruction
            GameObject instrObj = MakeText("Step Instruction", "Long leg (Anode) → 14c\nShort leg (Cathode) → 16c",
                new Color(0.85f, 0.90f, 0.95f), 0.0018f,
                new Vector3(-0.145f, 0.018f, -0.004f), root.transform);
            tutorial.stepInstructionText = instrObj.GetComponent<TextMesh>();

            // Status / Feedback
            GameObject statusObj = MakeText("Step Status", "Follow the glowing targets.",
                new Color(0.2f, 0.85f, 1f), 0.0016f,
                new Vector3(-0.145f, -0.052f, -0.004f), root.transform);
            tutorial.statusText = statusObj.GetComponent<TextMesh>();

            // Arduino Blink Code Panel (shown on right half of card)
            GameObject codeObj = MakeText("Code Display", "", new Color(0.35f, 0.95f, 0.55f), 0.0014f,
                new Vector3(0.018f, 0.055f, -0.004f), root.transform);
            tutorial.codeDisplayNode = codeObj.GetComponent<TextMesh>();

            return root;
        }

        static GameObject BuildRunButton(Vector3 pos, Material btnMat, TutorialController tutorial)
        {
            GameObject root = new GameObject("3D RUN Button");
            root.transform.position = pos;

            // Base
            GameObject baseObj = Primitive("BtnBase", PrimitiveType.Cylinder, pos, new Vector3(0.030f, 0.005f, 0.030f),
                MakeMaterial("BtnBaseMat", new Color(0.1f, 0.12f, 0.15f)));
            baseObj.transform.SetParent(root.transform, true);

            // Push surface
            GameObject cap = Primitive("BtnCap", PrimitiveType.Cylinder, pos + Vector3.up * 0.004f, new Vector3(0.025f, 0.006f, 0.025f), btnMat);
            cap.transform.SetParent(root.transform, true);

            // Text
            GameObject textObj = MakeText("BtnText", "RUN", Color.white, 0.0028f, pos + Vector3.up * 0.010f, root.transform);
            textObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            tutorial.runButtonText = textObj.GetComponent<TextMesh>();
            tutorial.runButtonText.anchor = TextAnchor.MiddleCenter;

            tutorial.runButtonObject = root;

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = Vector3.up * 0.005f;
            col.size = new Vector3(0.032f, 0.018f, 0.032f);

            return root;
        }

        static GameObject BuildResetButton(Vector3 pos, Material btnMat, TutorialController tutorial)
        {
            GameObject root = new GameObject("3D RESET Button");
            root.transform.position = pos;

            // Base
            GameObject baseObj = Primitive("ResetBase", PrimitiveType.Cylinder, pos, new Vector3(0.026f, 0.004f, 0.026f),
                MakeMaterial("ResetBaseMat", new Color(0.12f, 0.14f, 0.16f)));
            baseObj.transform.SetParent(root.transform, true);

            // Push surface
            GameObject cap = Primitive("ResetCap", PrimitiveType.Cylinder, pos + Vector3.up * 0.003f, new Vector3(0.022f, 0.005f, 0.022f), btnMat);
            cap.transform.SetParent(root.transform, true);

            // Text
            GameObject textObj = MakeText("ResetText", "RESET", new Color(0.85f, 0.88f, 0.92f), 0.0022f, pos + Vector3.up * 0.008f, root.transform);
            textObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            textObj.GetComponent<TextMesh>().anchor = TextAnchor.MiddleCenter;

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = Vector3.up * 0.004f;
            col.size = new Vector3(0.028f, 0.016f, 0.028f);

            return root;
        }

        static GameObject PlaceModel(string name, string path, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("Missing or unimported model at " + path);
                return null;
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;
            return instance;
        }

        static GameObject MakeAnchor(string name, Vector3 position, Material material)
        {
            GameObject anchor = Primitive(name, PrimitiveType.Sphere, position, Vector3.one * 0.008f, material);
            return anchor;
        }

        static GameObject MakePinLabel(string label, Vector3 position, Material material)
        {
            GameObject tag = Primitive("Pin Label " + label, PrimitiveType.Cube, position,
                new Vector3(0.032f, 0.009f, 0.016f),
                MakeMaterial("PinLabelBack", new Color(0.02f, 0.12f, 0.18f, 0.95f)));
            Object.DestroyImmediate(tag.GetComponent<Collider>());

            GameObject textObj = MakeText("Text " + label, label, material.color, 0.0026f, position + Vector3.up * 0.007f, null);
            textObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            return tag;
        }

        static GameObject MakeText(string name, string value, Color color, float size, Vector3 localPosition, Transform parent)
        {
            GameObject obj = new GameObject(name);
            if (parent != null) obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = Quaternion.identity;

            TextMesh text = obj.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleLeft;
            text.alignment = TextAlignment.Left;
            text.fontSize = 64;
            text.characterSize = size;
            text.color = color;
            return obj;
        }

        static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            if (material != null) obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        static Material MakeMaterial(string name, Color color, bool emission = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            Material mat = new Material(shader) { name = name, color = color };
            if (emission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2.0f);
            }
            return mat;
        }
    }
}
