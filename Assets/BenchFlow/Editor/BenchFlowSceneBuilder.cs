using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BenchFlow.Editor
{
    /// <summary>
    /// Builds the complete VR and Desktop interactive workbench scene for BenchFlow XR.
    /// Follows the reference video design: 4-step guided lesson with real breadboard alignment,
    /// 3D discrete components, VR 6-DOF hand tracking, interactive wiring, and live Arduino Blink simulation.
    /// </summary>
    public static class BenchFlowSceneBuilder
    {
        const string Models = "Assets/BenchFlow/Models/External/";
        const float DeskTopY = 0.495f;

        [MenuItem("BenchFlow/Create VR Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Realistic workbench ambient lighting
            RenderSettings.ambientLight = new Color(0.42f, 0.46f, 0.52f);
            RenderSettings.ambientIntensity = 0.5f;

            // 1. Workbench Desk Model
            GameObject workbench = PlaceModel("Workbench Model (CC0)", Models + "Workbench_OBJ/workbench.obj",
                new Vector3(0, 0, 0), Quaternion.identity, Vector3.one * 0.55f);

            // Shaders and Materials
            Material blueGlow = MakeMaterial("Tutorial Hologram Blue", new Color(0.10f, 0.70f, 1f), true);
            Material cyanGlow = MakeMaterial("Pin Label Cyan", new Color(0.30f, 0.90f, 1f), true);
            Material greenGlow = MakeMaterial("Verified Green", new Color(0.2f, 0.95f, 0.4f), true);
            Material panelMat = MakeMaterial("Instruction Glass", new Color(0.02f, 0.05f, 0.08f, 0.94f));
            Material trayMat = MakeMaterial("Parts Tray", new Color(0.05f, 0.07f, 0.09f));
            Material ceramicMat = MakeMaterial("Resistor Ceramic", new Color(0.85f, 0.74f, 0.58f));
            Material wireMetalMat = MakeMaterial("Metal Lead", new Color(0.75f, 0.77f, 0.80f));
            Material ledRedOff = MakeMaterial("LED Red Off", new Color(0.6f, 0.05f, 0.05f, 0.9f));
            Material ledRedLit = MakeMaterial("LED Red Lit", new Color(1f, 0.15f, 0.15f, 1f), true);
            Material runBtnMat = MakeMaterial("Run Button Mat", new Color(0.08f, 0.55f, 0.85f), true);

            // 2. Breadboard Object & Grid (Rows 1-63 along X, Columns a-j along Z)
            GameObject breadboardObject = new GameObject("Breadboard | Electrical Grid");
            breadboardObject.transform.position = new Vector3(0.015f, DeskTopY, 0.0f);
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
                new Vector3(-0.145f, DeskTopY, 0.015f), Quaternion.Euler(0, 90, 0), Vector3.one);
            if (controller != null)
            {
                controller.AddComponent<BoxCollider>().size = new Vector3(0.10f, 0.025f, 0.065f);
            }

            // Arduino Pin Anchors
            GameObject pin13 = MakeAnchor("UNO Digital Pin 13", new Vector3(-0.102f, DeskTopY + 0.025f, -0.005f), cyanGlow);
            GameObject pinGnd = MakeAnchor("UNO GND", new Vector3(-0.102f, DeskTopY + 0.025f, 0.025f), cyanGlow);
            MakePinLabel("13", pin13.transform.position + Vector3.up * 0.015f, cyanGlow);
            MakePinLabel("GND", pinGnd.transform.position + Vector3.up * 0.015f, cyanGlow);

            // Arduino Built-in D13 LED Indicator
            GameObject unoLedObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            unoLedObj.name = "Arduino D13 Built-in LED";
            unoLedObj.transform.position = new Vector3(-0.118f, DeskTopY + 0.023f, -0.005f);
            unoLedObj.transform.localScale = new Vector3(0.003f, 0.002f, 0.003f);
            Object.DestroyImmediate(unoLedObj.GetComponent<Collider>());
            Renderer unoLedRenderer = unoLedObj.GetComponent<Renderer>();
            Material unoLedOff = MakeMaterial("Uno LED Off", new Color(0.3f, 0.25f, 0.05f));
            Material unoLedLit = MakeMaterial("Uno LED Lit", new Color(1f, 0.85f, 0.1f), true);
            unoLedRenderer.sharedMaterial = unoLedOff;

            // 4. Parts Tray (on the right)
            GameObject tray = Primitive("Parts Tray", PrimitiveType.Cube,
                new Vector3(0.165f, DeskTopY + 0.004f, 0.015f),
                new Vector3(0.115f, 0.008f, 0.15f), trayMat);

            // 5. Discrete 220 Ω Resistor Component
            CircuitPart resistor = CreateDiscreteResistor(
                "Resistor 220 Ohm",
                new Vector3(0.165f, DeskTopY + 0.015f, -0.025f),
                ceramicMat, wireMetalMat);

            // 6. Discrete Red 5mm LED Component
            CircuitPart led = CreateDiscreteLed(
                "Red LED",
                new Vector3(0.165f, DeskTopY + 0.015f, 0.035f),
                ledRedOff, wireMetalMat);

            // Light for when the LED lights up
            GameObject ledLightObj = new GameObject("LED Light Source");
            ledLightObj.transform.SetParent(led.transform, false);
            ledLightObj.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            Light ledPointLight = ledLightObj.AddComponent<Light>();
            ledPointLight.type = LightType.Point;
            ledPointLight.color = new Color(1f, 0.2f, 0.2f);
            ledPointLight.intensity = 0.8f;
            ledPointLight.range = 0.12f;
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

            // 8. Holographic Guided Markers & Labels
            // Step 1: LED (14c & 16c)
            GameObject marker14c = MakeTargetMarker("Marker 14c", grid.HolePosition(grid.GetHole("14c")), blueGlow, "14c");
            GameObject marker16c = MakeTargetMarker("Marker 16c", grid.HolePosition(grid.GetHole("16c")), blueGlow, "16c");
            tutorial.markerHole14c = marker14c;
            tutorial.markerHole16c = marker16c;

            // Step 2: Resistor (8d & 14d)
            GameObject marker8d = MakeTargetMarker("Marker 8d", grid.HolePosition(grid.GetHole("8d")), blueGlow, "8d");
            GameObject marker14d = MakeTargetMarker("Marker 14d", grid.HolePosition(grid.GetHole("14d")), blueGlow, "14d");
            tutorial.markerHole8d = marker8d;
            tutorial.markerHole14d = marker14d;

            // Step 3: Wires (Pin 13 -> 8b, GND -> 16a)
            GameObject marker8b = MakeTargetMarker("Marker 8b", grid.HolePosition(grid.GetHole("8b")), blueGlow, "8b");
            GameObject marker16a = MakeTargetMarker("Marker 16a", grid.HolePosition(grid.GetHole("16a")), blueGlow, "16a");
            tutorial.markerPin13 = pin13;
            tutorial.markerHole8b = marker8b;
            tutorial.markerPinGnd = pinGnd;
            tutorial.markerHole16a = marker16a;

            // 9. Floating 3D Guided Card (tilted towards VR player)
            GameObject cardObj = BuildGuidedCard(
                new Vector3(0.015f, DeskTopY + 0.22f, 0.16f),
                Quaternion.Euler(18f, 0f, 0f),
                panelMat, cyanGlow, tutorial);

            // 10. Interactive 3D RUN Button
            GameObject runBtn = BuildRunButton(
                new Vector3(0.115f, DeskTopY + 0.018f, -0.045f),
                runBtnMat, tutorial);

            // 11. VR Rig & Player (Positioned in front of the bench)
            GameObject vrRigObj = new GameObject("[XR Rig] Player");
            vrRigObj.transform.position = new Vector3(0.015f, DeskTopY + 0.40f, -0.45f);
            VRRig vrRig = vrRigObj.AddComponent<VRRig>();
            vrRig.standingBenchOffset = new Vector3(0.015f, DeskTopY + 0.40f, -0.45f);

            GameObject cameraObj = new GameObject("CenterEyeCamera");
            cameraObj.transform.SetParent(vrRigObj.transform, false);
            cameraObj.tag = "MainCamera";
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.09f);
            cameraObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            cameraObj.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);

            vrRig.headTransform = cameraObj.transform;
            vrRig.centerEyeCamera = cam;

            // VR Hands
            GameObject leftHandObj = new GameObject("LeftHandController");
            leftHandObj.transform.SetParent(vrRigObj.transform, false);
            leftHandObj.transform.localPosition = new Vector3(-0.15f, 0.05f, 0.25f);
            VRHandInteractor leftHand = leftHandObj.AddComponent<VRHandInteractor>();
            leftHand.handNode = UnityEngine.XR.XRNode.LeftHand;
            leftHand.workbench = workbenchState;
            leftHand.breadboard = grid;
            leftHand.tutorial = tutorial;
            vrRig.leftHand = leftHand;

            GameObject rightHandObj = new GameObject("RightHandController");
            rightHandObj.transform.SetParent(vrRigObj.transform, false);
            rightHandObj.transform.localPosition = new Vector3(0.15f, 0.05f, 0.25f);
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
            key.intensity = 1.0f;
            keyLight.transform.rotation = Quaternion.Euler(50, -30, 0);

            GameObject fillLight = new GameObject("Workbench Fill Light");
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.35f;
            fill.color = new Color(0.6f, 0.8f, 1f);
            fillLight.transform.rotation = Quaternion.Euler(25, 140, 0);

            // Save Scene
            EditorSceneManager.SaveScene(scene, "Assets/BenchFlow/Scenes/BenchFlowPrototype.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/BenchFlow/Scenes/BenchFlowPrototype.unity", true) };
            AssetDatabase.SaveAssets();

            Debug.Log("BenchFlow VR Workbench scene successfully generated! Compatible with Meta Quest, OpenXR, and Desktop Preview.");
        }

        static CircuitPart CreateDiscreteResistor(string name, Vector3 pos, Material bodyMat, Material leadMat)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.identity;

            // Main cylindrical ceramic body (10mm long, 3mm diameter)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Resistor Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.0032f, 0.005f, 0.0032f);
            body.transform.localRotation = Quaternion.Euler(0, 0, 90);
            body.transform.localPosition = new Vector3(0, 0.004f, 0);
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

            // Wire Leads (6 rows span = 15.24 mm along X = +/-0.00762m)
            CreateResistorLead("LeadA", root.transform, -0.00762f, -0.005f, leadMat);
            CreateResistorLead("LeadB", root.transform, 0.00762f, 0.005f, leadMat);

            // Collider for grabbing
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.004f, 0);
            col.size = new Vector3(0.020f, 0.015f, 0.012f);

            CircuitPart part = root.AddComponent<CircuitPart>();
            part.componentId = "RESISTOR";
            part.isPolarized = false;
            part.terminalOffsetA = new Vector3(-0.00762f, 0f, 0f);
            part.terminalOffsetB = new Vector3(0.00762f, 0f, 0f);

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

        static void CreateResistorLead(string name, Transform parent, float xPos, float xInner, Material mat)
        {
            // Horizontal lead segment
            GameObject horiz = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horiz.name = name + "_Horiz";
            horiz.transform.SetParent(parent, false);
            float len = Mathf.Abs(xPos - xInner);
            horiz.transform.localPosition = new Vector3((xPos + xInner) * 0.5f, 0.004f, 0);
            horiz.transform.localScale = new Vector3(0.0006f, len * 0.5f, 0.0006f);
            horiz.transform.localRotation = Quaternion.Euler(0, 0, 90);
            horiz.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(horiz.GetComponent<Collider>());

            // Vertical lead pin (inserts into breadboard hole)
            GameObject vert = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vert.name = name + "_Pin";
            vert.transform.SetParent(parent, false);
            vert.transform.localPosition = new Vector3(xPos, 0.001f, 0);
            vert.transform.localScale = new Vector3(0.0006f, 0.0035f, 0.0006f);
            vert.transform.localRotation = Quaternion.identity;
            vert.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(vert.GetComponent<Collider>());
        }

        static CircuitPart CreateDiscreteLed(string name, Vector3 pos, Material domeMat, Material leadMat)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.identity;

            // 5mm Dome
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "LED Dome";
            dome.transform.SetParent(root.transform, false);
            dome.transform.localScale = new Vector3(0.005f, 0.0075f, 0.005f);
            dome.transform.localPosition = new Vector3(0, 0.0075f, 0);
            dome.GetComponent<Renderer>().sharedMaterial = domeMat;
            Object.DestroyImmediate(dome.GetComponent<Collider>());

            // Base rim with flat side indicating cathode
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "LED Rim";
            rim.transform.SetParent(root.transform, false);
            rim.transform.localScale = new Vector3(0.0058f, 0.001f, 0.0058f);
            rim.transform.localPosition = new Vector3(0, 0.004f, 0);
            rim.GetComponent<Renderer>().sharedMaterial = domeMat;
            Object.DestroyImmediate(rim.GetComponent<Collider>());

            // Leads: 2 rows span = 5.08 mm = +/-0.00254m
            // Long leg = Anode (terminal A at -0.00254f), Short leg = Cathode (terminal B at +0.00254f)
            CreateLedLead("AnodeLead", root.transform, -0.00254f, 0.0045f, leadMat);
            CreateLedLead("CathodeLead", root.transform, 0.00254f, 0.0035f, leadMat);

            // Grab collider
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.006f, 0);
            col.size = new Vector3(0.015f, 0.018f, 0.012f);

            CircuitPart part = root.AddComponent<CircuitPart>();
            part.componentId = "LED";
            part.isPolarized = true;
            part.terminalOffsetA = new Vector3(-0.00254f, 0f, 0f); // Anode (long leg)
            part.terminalOffsetB = new Vector3(0.00254f, 0f, 0f);  // Cathode (short leg)

            return part;
        }

        static void CreateLedLead(string name, Transform parent, float xPos, float length, Material mat)
        {
            GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pin.name = name;
            pin.transform.SetParent(parent, false);
            pin.transform.localPosition = new Vector3(xPos, 0.004f - length * 0.5f, 0);
            pin.transform.localScale = new Vector3(0.0006f, length * 0.5f, 0.0006f);
            pin.transform.localRotation = Quaternion.identity;
            pin.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(pin.GetComponent<Collider>());
        }

        static GameObject MakeTargetMarker(string name, Vector3 pos, Material mat, string label)
        {
            GameObject markerRoot = new GameObject(name);
            markerRoot.transform.position = pos;

            // Pulsing sphere
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "GlowSphere";
            sphere.transform.SetParent(markerRoot.transform, false);
            sphere.transform.localPosition = Vector3.up * 0.004f;
            sphere.transform.localScale = Vector3.one * 0.005f;
            sphere.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            sphere.AddComponent<HologramPulse>();

            // Floating Label Tag
            GameObject tag = Primitive("Label_" + label, PrimitiveType.Cube,
                pos + Vector3.up * 0.016f + Vector3.back * 0.012f,
                new Vector3(0.022f, 0.007f, 0.012f),
                MakeMaterial("TagBack", new Color(0.02f, 0.12f, 0.18f, 0.95f)));
            tag.transform.SetParent(markerRoot.transform, true);
            Object.DestroyImmediate(tag.GetComponent<Collider>());

            GameObject textObj = MakeText("LabelText_" + label, label, mat.color, 0.0018f,
                pos + Vector3.up * 0.020f + Vector3.back * 0.012f, markerRoot.transform);
            textObj.transform.rotation = Quaternion.Euler(45, 0, 0);

            return markerRoot;
        }

        static GameObject BuildGuidedCard(Vector3 pos, Quaternion rot, Material backerMat, Material accentMat, TutorialController tutorial)
        {
            GameObject root = new GameObject("Guided Instruction Card");
            root.transform.position = pos;
            root.transform.rotation = rot;

            // Glass backer
            GameObject backer = Primitive("Card Backer", PrimitiveType.Cube, pos, new Vector3(0.26f, 0.16f, 0.005f), backerMat);
            backer.transform.SetParent(root.transform, false);
            backer.transform.localPosition = Vector3.zero;
            backer.transform.localRotation = Quaternion.identity;
            Object.DestroyImmediate(backer.GetComponent<Collider>());

            // Title
            GameObject titleObj = MakeText("Step Title", "STEP 1 OF 4\nPLACE THE RED LED", Color.white, 0.0019f,
                new Vector3(-0.118f, 0.058f, -0.004f), root.transform);
            tutorial.stepTitleText = titleObj.GetComponent<TextMesh>();
            tutorial.stepTitleText.fontStyle = FontStyle.Bold;

            // Instruction
            GameObject instrObj = MakeText("Step Instruction", "Long leg (Anode) → 14c\nShort leg (Cathode) → 16c",
                new Color(0.85f, 0.90f, 0.95f), 0.0016f,
                new Vector3(-0.118f, 0.015f, -0.004f), root.transform);
            tutorial.stepInstructionText = instrObj.GetComponent<TextMesh>();

            // Status / Feedback
            GameObject statusObj = MakeText("Step Status", "Follow the glowing targets.",
                new Color(0.2f, 0.85f, 1f), 0.0015f,
                new Vector3(-0.118f, -0.045f, -0.004f), root.transform);
            tutorial.statusText = statusObj.GetComponent<TextMesh>();

            // Arduino Blink Code Panel (shown on right half or step 4)
            GameObject codeObj = MakeText("Code Display", "", new Color(0.35f, 0.95f, 0.55f), 0.0013f,
                new Vector3(0.015f, 0.045f, -0.004f), root.transform);
            tutorial.codeDisplayNode = codeObj.GetComponent<TextMesh>();

            return root;
        }

        static GameObject BuildRunButton(Vector3 pos, Material btnMat, TutorialController tutorial)
        {
            GameObject root = new GameObject("3D RUN Button");
            root.transform.position = pos;

            // Base
            GameObject baseObj = Primitive("BtnBase", PrimitiveType.Cylinder, pos, new Vector3(0.024f, 0.004f, 0.024f),
                MakeMaterial("BtnBaseMat", new Color(0.1f, 0.12f, 0.15f)));
            baseObj.transform.SetParent(root.transform, true);

            // Push surface
            GameObject cap = Primitive("BtnCap", PrimitiveType.Cylinder, pos + Vector3.up * 0.003f, new Vector3(0.020f, 0.005f, 0.020f), btnMat);
            cap.transform.SetParent(root.transform, true);

            // Text
            GameObject textObj = MakeText("BtnText", "RUN", Color.white, 0.0022f, pos + Vector3.up * 0.008f, root.transform);
            textObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            tutorial.runButtonText = textObj.GetComponent<TextMesh>();
            tutorial.runButtonText.anchor = TextAnchor.MiddleCenter;

            tutorial.runButtonObject = root;

            // BoxCollider for VR ray / direct trigger hit
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = Vector3.up * 0.004f;
            col.size = new Vector3(0.026f, 0.015f, 0.026f);

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
            GameObject anchor = Primitive(name, PrimitiveType.Sphere, position, Vector3.one * 0.006f, material);
            return anchor;
        }

        static GameObject MakePinLabel(string label, Vector3 position, Material material)
        {
            GameObject tag = Primitive("Pin Label " + label, PrimitiveType.Cube, position,
                new Vector3(0.028f, 0.008f, 0.014f),
                MakeMaterial("PinLabelBack", new Color(0.02f, 0.12f, 0.18f, 0.95f)));
            Object.DestroyImmediate(tag.GetComponent<Collider>());

            GameObject textObj = MakeText("Text " + label, label, material.color, 0.0022f, position + Vector3.up * 0.006f, null);
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
