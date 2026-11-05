using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BenchFlow.Editor
{
    public static class BenchFlowSceneBuilder
    {
        [MenuItem("BenchFlow/Create VR Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Material tabletop = MakeMaterial("Workbench", new Color(0.20f, 0.23f, 0.26f));
            Material green = MakeMaterial("Arduino Green", new Color(0.08f, 0.38f, 0.25f));
            Material blue = MakeMaterial("Hologram Blue", new Color(0.05f, 0.62f, 1f), true);
            Material resistorColor = MakeMaterial("Resistor Body", new Color(0.68f, 0.43f, 0.20f));
            Material ledColor = MakeMaterial("LED Red", new Color(0.85f, 0.06f, 0.04f), true);
            Material metal = MakeMaterial("Component Leads", new Color(0.68f, 0.70f, 0.72f));

            GameObject table = Primitive("Workbench Surface", PrimitiveType.Cube, new Vector3(0, 0.72f, 0), new Vector3(1.45f, 0.08f, 0.95f), tabletop);
            table.name = "Workbench Surface";
            GameObject breadboardObject = new GameObject("Breadboard");
            breadboardObject.transform.position = new Vector3(0.06f, 0.765f, 0.02f);
            BreadboardGrid grid = breadboardObject.AddComponent<BreadboardGrid>();
            grid.rowCount = 63;
            grid.columnsPerSide = 5;
            grid.holeSpacing = 0.00254f;
            grid.rowSpacing = 0.00254f;
            grid.centerGap = 0.00762f;
            grid.railOffset = 0.0215f;

            GameObject boardModelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BenchFlow/Models/External/Breadboard_OBJ/breadboard.obj");
            if (boardModelAsset == null)
            {
                Debug.LogError("Missing imported breadboard model at Assets/BenchFlow/Models/External/Breadboard_OBJ/breadboard.obj. Refresh the Project window, then run this menu again.");
                return;
            }
            GameObject boardModel = (GameObject)PrefabUtility.InstantiatePrefab(boardModelAsset);
            boardModel.name = "Breadboard Model (CC0)";
            boardModel.transform.SetParent(breadboardObject.transform, false);
            boardModel.transform.localPosition = Vector3.zero;
            boardModel.transform.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.right);
            boardModel.transform.localScale = Vector3.one;
            var gridCollider = breadboardObject.AddComponent<BoxCollider>();
            gridCollider.size = new Vector3(0.054f, 0.012f, 0.165f);

            GameObject uno = Primitive("Arduino Uno", PrimitiveType.Cube, new Vector3(-0.02f, 0.768f, 0.02f), new Vector3(0.069f, 0.016f, 0.053f), green);
            uno.transform.rotation = Quaternion.Euler(0, 90, 0);

            GameObject marker = Primitive("Tutorial Hologram Marker", PrimitiveType.Sphere, grid.HolePosition(new HoleAddress(14, 2, BreadboardSide.Left, BreadboardRail.None)) + Vector3.up * 0.009f, Vector3.one * 0.015f, blue);
            marker.name = "Tutorial Hologram Marker";
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.AddComponent<HologramPulse>();

            GameObject state = new GameObject("Workbench State");
            WorkbenchState workbenchState = state.AddComponent<WorkbenchState>();
            workbenchState.breadboard = grid;
            TutorialController tutorial = state.AddComponent<TutorialController>();
            tutorial.marker = marker.transform;
            tutorial.graph = workbenchState.Circuit;
            workbenchState.tutorial = tutorial;
            GameObject promptBacker = Primitive("Tutorial Prompt Backer", PrimitiveType.Cube, new Vector3(0.06f, 1.00f, 0.10f), new Vector3(0.43f, 0.12f, 0.012f), MakeMaterial("Prompt Backer", new Color(0.035f, 0.10f, 0.16f)));
            GameObject promptObject = new GameObject("Tutorial Prompt");
            promptObject.transform.position = new Vector3(0.06f, 1.00f, 0.108f);
            TextMesh promptText = promptObject.AddComponent<TextMesh>();
            promptText.text = "STEP 1 / 5\nPick up a resistor and place it across two breadboard strips.";
            promptText.anchor = TextAnchor.MiddleCenter;
            promptText.alignment = TextAlignment.Center;
            promptText.fontSize = 36;
            promptText.characterSize = 0.004f;
            promptText.color = new Color(0.65f, 0.90f, 1f);
            tutorial.promptRoot = promptObject.transform;
            tutorial.promptText = promptText;
            tutorial.stepTargets = new Transform[5];
            for (int i = 0; i < tutorial.stepTargets.Length; i++)
            {
                GameObject target = new GameObject("Tutorial Target " + (i + 1));
                target.transform.position = grid.HolePosition(new HoleAddress(5 + i * 4, 1, BreadboardSide.Left, BreadboardRail.None));
                tutorial.stepTargets[i] = target.transform;
            }

            GameObject cameraObject = new GameObject("Prototype Preview Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0.02f, 0.98f, -0.29f);
            cameraObject.transform.LookAt(new Vector3(0.04f, 0.77f, 0.02f));
            camera.fieldOfView = 50;
            tutorial.viewCamera = camera;

            CircuitPart resistor = CreatePreviewPart("Resistor", "RESISTOR", PrimitiveType.Cube,
                new Vector3(0.00f, 0.76f, -0.14f), new Vector3(0.007f, 0.006f, 0.014f), resistorColor, metal);
            CircuitPart led = CreatePreviewPart("LED", "LED", PrimitiveType.Sphere,
                new Vector3(0.08f, 0.76f, -0.14f), new Vector3(0.008f, 0.008f, 0.008f), ledColor, metal);
            resistor.terminalOffsetA = new Vector3(0f, 0f, -0.00508f);
            resistor.terminalOffsetB = new Vector3(0f, 0f, 0.00508f);
            led.terminalOffsetA = new Vector3(0f, 0f, -0.00508f);
            led.terminalOffsetB = new Vector3(0f, 0f, 0.00508f);

            Transform d13Anchor = MakeAnchor("Arduino D13", new Vector3(0.012f, 0.778f, -0.005f), new Color(0.92f, 0.55f, 0.10f));
            Transform groundAnchor = MakeAnchor("Arduino GND", new Vector3(0.012f, 0.778f, 0.045f), new Color(0.12f, 0.16f, 0.20f));
            DesktopPreviewInteractor preview = state.AddComponent<DesktopPreviewInteractor>();
            preview.previewCamera = camera;
            preview.breadboard = grid;
            preview.workbench = workbenchState;
            preview.tutorial = tutorial;
            preview.resistor = resistor;
            preview.led = led;
            preview.unoD13 = d13Anchor;
            preview.unoGnd = groundAnchor;
            tutorial.viewCamera = camera;

            GameObject lightObject = new GameObject("Workbench Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(48, -25, 0);

            EditorSceneManager.SaveScene(scene, "Assets/BenchFlow/Scenes/BenchFlowPrototype.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/BenchFlow/Scenes/BenchFlowPrototype.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("BenchFlow prototype scene created. XR rig and Meta Interaction SDK input must be connected after package version is confirmed.");
        }

        static CircuitPart CreatePreviewPart(string name, string componentId, PrimitiveType bodyType,
            Vector3 stagingPosition, Vector3 bodyScale, Material bodyMaterial, Material leadMaterial)
        {
            GameObject root = new GameObject(name);
            root.transform.position = stagingPosition;
            CircuitPart part = root.AddComponent<CircuitPart>();
            part.componentId = componentId;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 0.008f, 0);
            collider.size = new Vector3(0.016f, 0.018f, 0.018f);

            GameObject body = Primitive(name + " Body", bodyType, stagingPosition + Vector3.up * 0.004f, bodyScale, bodyMaterial);
            body.transform.SetParent(root.transform, true);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            for (int sign = -1; sign <= 1; sign += 2)
            {
                GameObject lead = Primitive(name + " Lead", PrimitiveType.Cylinder,
                    stagingPosition + new Vector3(0, 0.001f, sign * 0.0035f), new Vector3(0.001f, 0.0025f, 0.001f), leadMaterial);
                lead.transform.rotation = Quaternion.Euler(90, 0, 0);
                lead.transform.SetParent(root.transform, true);
                Object.DestroyImmediate(lead.GetComponent<Collider>());
            }
            return part;
        }

        static Transform MakeAnchor(string name, Vector3 position, Color color)
        {
            Material mat = MakeMaterial(name + " Material", color, true);
            GameObject anchor = Primitive(name, PrimitiveType.Sphere, position, Vector3.one * 0.015f, mat);
            return anchor.transform;
        }

        static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name; obj.transform.position = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        static Material MakeMaterial(string name, Color color, bool emission = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader) { name = name, color = color };
            if (emission) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 2.5f); }
            return mat;
        }
    }
}
