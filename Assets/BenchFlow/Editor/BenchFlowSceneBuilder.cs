using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BenchFlow.Editor
{
    /// <summary>Builds the authored virtual workbench from licensed mesh assets and reusable scene systems.</summary>
    public static class BenchFlowSceneBuilder
    {
        const string Models = "Assets/BenchFlow/Models/External/";
        const float DeskTopY = 0.495f;

        [MenuItem("BenchFlow/Create VR Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.39f, 0.43f, 0.49f);
            RenderSettings.ambientIntensity = 0.45f;

            GameObject workbench = PlaceModel("Workbench Model (CC0)", Models + "Workbench_OBJ/workbench.obj",
                new Vector3(0, 0, 0), Quaternion.identity, Vector3.one * 0.55f);
            if (workbench == null) return;

            Material blueGlow = MakeMaterial("Tutorial Hologram Blue", new Color(0.10f, 0.70f, 1f), true);
            Material cyanGlow = MakeMaterial("Pin Label Cyan", new Color(0.35f, 0.92f, 1f), true);
            Material panelMat = MakeMaterial("Instruction Glass", new Color(0.018f, 0.055f, 0.09f, 0.92f));
            Material trayMat = MakeMaterial("Parts Tray", new Color(0.045f, 0.065f, 0.085f));

            // The breadboard mesh retains the source model's texture; the grid remains an independent electrical system.
            GameObject breadboardObject = new GameObject("Breadboard | electrical grid");
            breadboardObject.transform.position = new Vector3(0.015f, DeskTopY, 0.0f);
            BreadboardGrid grid = breadboardObject.AddComponent<BreadboardGrid>();
            grid.rowCount = 63;
            grid.columnsPerSide = 5;
            grid.holeSpacing = 0.00254f;
            grid.rowSpacing = 0.00254f;
            grid.centerGap = 0.00762f;
            grid.railOffset = 0.0215f;

            GameObject breadboardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Breadboard_OBJ/breadboard.obj");
            if (breadboardAsset == null)
            {
                Debug.LogError("Breadboard OBJ is missing or still importing. Refresh Assets, then run BenchFlow/Create VR Prototype Scene again.");
                return;
            }
            GameObject boardModel = (GameObject)PrefabUtility.InstantiatePrefab(breadboardAsset);
            boardModel.name = "Breadboard Model (CC0)";
            boardModel.transform.SetParent(breadboardObject.transform, false);
            boardModel.transform.localPosition = Vector3.zero;
            boardModel.transform.localRotation = Quaternion.identity;
            boardModel.transform.localScale = Vector3.one;
            BoxCollider gridCollider = breadboardObject.AddComponent<BoxCollider>();
            gridCollider.center = new Vector3(0, 0.01f, 0);
            gridCollider.size = new Vector3(0.054f, 0.02f, 0.165f);

            // This is deliberately unbranded: it is a generic controller-board visual, not a genuine Arduino Uno model.
            GameObject controller = PlaceModel("Controller Board (generic, CC0)", Models + "CircuitBoard_OBJ/circuit-board.obj",
                new Vector3(-0.145f, DeskTopY, 0.015f), Quaternion.Euler(0, 90, 0), Vector3.one);
            if (controller == null) return;
            controller.AddComponent<BoxCollider>().size = new Vector3(0.10f, 0.025f, 0.065f);

            // Real downloaded part assemblies provide the loose component staging area at the right side of the desk.
            GameObject tray = Primitive("Parts tray", PrimitiveType.Cube, new Vector3(0.165f, DeskTopY + 0.004f, 0.015f),
                new Vector3(0.115f, 0.008f, 0.15f), trayMat);
            GameObject ledSet = PlaceModel("LED component assortment (CC0)", Models + "LED_OBJ/led.obj",
                new Vector3(0.165f, DeskTopY + 0.009f, 0.015f), Quaternion.identity, Vector3.one * 0.60f);
            GameObject resistorSet = PlaceModel("Resistor component assortment (CC0)", Models + "Resistor_OBJ/resistor.obj",
                new Vector3(0.165f, DeskTopY + 0.009f, -0.040f), Quaternion.Euler(0, 0, 90), Vector3.one * 0.48f);
            if (ledSet == null || resistorSet == null) return;
            // Keep imported component assemblies out of the scaled tray hierarchy. A child of the
            // non-uniformly scaled tray inherits a compensating local scale when reparented, which
            // makes its later interactive root unexpectedly enormous.

            // Interactive tutorial targets remain small world-space markers; electrical state is not inferred from meshes.
            GameObject marker = Primitive("Tutorial Hologram Marker", PrimitiveType.Sphere,
                grid.HolePosition(new HoleAddress(14, 2, BreadboardSide.Left, BreadboardRail.None)) + Vector3.up * 0.009f,
                Vector3.one * 0.006f, blueGlow);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.AddComponent<HologramPulse>();

            GameObject state = new GameObject("Workbench State");
            WorkbenchState workbenchState = state.AddComponent<WorkbenchState>();
            workbenchState.breadboard = grid;
            TutorialController tutorial = state.AddComponent<TutorialController>();
            tutorial.graph = workbenchState.Circuit;
            workbenchState.tutorial = tutorial;

            GameObject pin13 = MakeAnchor("UNO-compatible digital pin 13", new Vector3(-0.102f, DeskTopY + 0.028f, -0.005f), cyanGlow);
            GameObject pinGround = MakeAnchor("UNO-compatible GND", new Vector3(-0.102f, DeskTopY + 0.028f, 0.025f), cyanGlow);
            MakePinLabel("13", pin13.transform.position + new Vector3(0.0f, 0.018f, 0.0f), cyanGlow);
            MakePinLabel("GND", pinGround.transform.position + new Vector3(-0.002f, 0.018f, 0.0f), cyanGlow);

            // Tutorial pickup targets use the real licensed assemblies as visual mesh children.
            CircuitPart resistor = CreateModelPart("Resistor", "RESISTOR", resistorSet,
                new Vector3(0.16f, DeskTopY + 0.035f, -0.02f), new Vector3(0.0f, 0.0f, -0.004f), new Vector3(0.0f, 0.0f, 0.004f));
            CircuitPart led = CreateModelPart("LED", "LED", ledSet,
                new Vector3(0.16f, DeskTopY + 0.035f, 0.04f), new Vector3(-0.004f, 0.0f, 0.0f), new Vector3(0.004f, 0.0f, 0.0f));

            GameObject cameraObject = new GameObject("Workbench Preview Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0.03f, 0.90f, -0.40f);
            cameraObject.transform.LookAt(new Vector3(0.01f, 0.50f, 0.005f));
            camera.fieldOfView = 44;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.065f, 0.085f);

            BuildInstructionCard(cameraObject.transform, panelMat, cyanGlow, tutorial);
            GameObject target14 = MakePinLabel("14c", grid.HolePosition(new HoleAddress(14, 2, BreadboardSide.Left, BreadboardRail.None)) + Vector3.up * 0.012f + Vector3.left * 0.035f, cyanGlow);
            GameObject target16 = MakePinLabel("16c", grid.HolePosition(new HoleAddress(16, 2, BreadboardSide.Left, BreadboardRail.None)) + Vector3.up * 0.012f + Vector3.right * 0.035f, cyanGlow);
            tutorial.stepTargets = new Transform[5];
            for (int i = 0; i < tutorial.stepTargets.Length; i++)
            {
                GameObject target = new GameObject("Tutorial Target " + (i + 1));
                target.transform.position = grid.HolePosition(new HoleAddress(5 + i * 4, 1, BreadboardSide.Left, BreadboardRail.None));
                tutorial.stepTargets[i] = target.transform;
            }
            tutorial.viewCamera = camera;
            tutorial.marker = marker.transform;

            DesktopPreviewInteractor preview = state.AddComponent<DesktopPreviewInteractor>();
            preview.previewCamera = camera;
            preview.breadboard = grid;
            preview.workbench = workbenchState;
            preview.tutorial = tutorial;
            preview.resistor = resistor;
            preview.led = led;
            preview.unoD13 = pin13.transform;
            preview.unoGnd = pinGround.transform;

            GameObject keyLight = new GameObject("Workbench Key Light");
            Light light = keyLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.90f;
            keyLight.transform.rotation = Quaternion.Euler(48, -28, 0);

            GameObject fillLight = new GameObject("Workbench Fill Light");
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.22f;
            fill.color = new Color(0.45f, 0.74f, 1f);
            fillLight.transform.rotation = Quaternion.Euler(22, 145, 0);

            EditorSceneManager.SaveScene(scene, "Assets/BenchFlow/Scenes/BenchFlowPrototype.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/BenchFlow/Scenes/BenchFlowPrototype.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("BenchFlow virtual workbench scene created with licensed meshes, desktop preview interaction, and an XR hand interaction adapter.");
        }

        static GameObject PlaceModel(string name, string path, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogError("Missing or unimported model at " + path + ". Refresh the Project window and run the scene builder again.");
                return null;
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;
            return instance;
        }

        static CircuitPart CreateModelPart(string name, string componentId, GameObject model,
            Vector3 position, Vector3 terminalA, Vector3 terminalB)
        {
            GameObject root = new GameObject(name + " | interactive component");
            root.transform.position = position;
            CircuitPart part = root.AddComponent<CircuitPart>();
            part.componentId = componentId;
            part.terminalOffsetA = terminalA;
            part.terminalOffsetB = terminalB;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 0.008f, 0);
            collider.size = new Vector3(0.018f, 0.018f, 0.02f);
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            return part;
        }

        static void BuildInstructionCard(Transform camera, Material backerMaterial, Material accent, TutorialController tutorial)
        {
            GameObject card = Primitive("Guided Step Card", PrimitiveType.Cube, Vector3.zero, Vector3.one, backerMaterial);
            card.transform.SetParent(camera, false);
            card.transform.localPosition = new Vector3(-0.145f, 0.16f, 0.62f);
            card.transform.localRotation = Quaternion.identity;
            card.transform.localScale = new Vector3(0.34f, 0.18f, 0.006f);
            Object.DestroyImmediate(card.GetComponent<Collider>());

            GameObject prompt = MakeText("Step instruction", "Place the resistor\nFollow the glowing target", Color.white, 0.00045f,
                new Vector3(-0.298f, 0.16f, 0.617f), camera);
            TextMesh promptMesh = prompt.GetComponent<TextMesh>();
            tutorial.promptRoot = prompt.transform;
            tutorial.promptText = promptMesh;
        }

        static GameObject MakeText(string name, string value, Color color, float size, Vector3 localPosition, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = Quaternion.identity;
            TextMesh text = obj.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleLeft;
            text.alignment = TextAlignment.Left;
            text.fontSize = 40;
            text.characterSize = size;
            text.color = color;
            return obj;
        }

        static GameObject MakePinLabel(string label, Vector3 position, Material material)
        {
            GameObject tag = Primitive("Pin label " + label, PrimitiveType.Cube, position, new Vector3(0.027f, 0.008f, 0.014f),
                MakeMaterial("Pin label back", new Color(0.02f, 0.12f, 0.17f, 0.92f)));
            Object.DestroyImmediate(tag.GetComponent<Collider>());
            GameObject textObject = MakeText("Pin text " + label, label, material.color, 0.00035f, position + Vector3.up * 0.006f, null);
            textObject.transform.rotation = Quaternion.Euler(90, 0, 0);
            return tag;
        }

        static GameObject MakeAnchor(string name, Vector3 position, Material material)
        {
            GameObject anchor = Primitive(name, PrimitiveType.Sphere, position, Vector3.one * 0.005f, material);
            Object.DestroyImmediate(anchor.GetComponent<SphereCollider>());
            return anchor;
        }

        static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        static Material MakeMaterial(string name, Color color, bool emission = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader) { name = name, color = color };
            if (emission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2.25f);
            }
            return mat;
        }
    }
}
