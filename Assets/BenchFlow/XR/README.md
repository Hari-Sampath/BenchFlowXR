# Quest XR foundation

The project targets Unity 6000.3.6f1 with Unity OpenXR, XR Hands, XR Plug-in Management, and Input System. On first editor import, the XR editor setup creates the Android OpenXR loader configuration, enables the built-in Meta Quest Support and Hand Tracking Subsystem features, and creates the reusable rig under `Resources/BenchFlow`.

The runtime rig adds a stereo head camera only to the active XR view. With tracked hands, thumb-index pinch selects a nearby `CircuitPart` collider and routes grab, move, and release through `IWorkbenchInteractionPort`. It does not contain hand meshes or alter the workbench scene. Add the visual hand assets separately when they are ready.

The Unity 6000.3.6f1 installation used for this foundation currently has only the Web build module. Android build support is required to create and deploy a Quest build.
