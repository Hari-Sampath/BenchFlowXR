# BenchFlow XR — Phase 1 prototype slice

## Audit

The provided workspace was empty: it contained no Unity project, scenes, package manifest, project settings, or existing assets. Unity **6000.3.6f1** is installed at `/Applications/Unity/Hub/Editor/6000.3.6f1`. With no project package manifest, there is no project-specific Meta XR SDK version to inspect. This scaffold records the installed editor version and leaves the package manifest empty; it does not upgrade or pin XR packages.

## First vertical slice

The visible breadboard uses a real-scale external 3D model. Its CC0 provenance is recorded beside the imported OBJ in `Assets/BenchFlow/Models/External/ATTRIBUTION.txt`; the board's hole grid and electrical mapping remain separate data in `BreadboardGrid`. The slice also includes snap coordinates, independent union-find circuit nets, deterministic D13 → resistor → LED → GND checks, a compact pulsing marker, a tutorial prompt, and a desktop-only preview interaction harness. `IWorkbenchInteractionPort` is the boundary for the Meta hand adapter and keeps input out of the circuit graph and validator.

## Open and generate the scene

1. Open this folder as a Unity project with Unity 6000.3.6f1.
2. Wait for the asset import, then run **BenchFlow → Create VR Prototype Scene**.
3. Open `Assets/BenchFlow/Scenes/BenchFlowPrototype.unity` and press Play.
4. In Play Mode, drag the resistor and LED onto the breadboard. Press `Space` to verify each tutorial step. Preview wire shortcuts: `1` connects D13 to the resistor, `2` connects the resistor to the LED anode, and `3` connects the LED cathode to GND.

The mouse and number-key controls are a desktop preview harness; wire shortcuts create preview wires. The project does not yet contain an XR rig, Meta Interaction SDK adapter, hand grabbing, hand-drawn wire endpoints, Android player configuration, or a Quest build. Those require selecting and validating compatible Meta SDK packages first; the original project had no Meta package configuration to inspect.

## Implementation plan

1. **Audit and baseline:** retain Unity 6000.3.6f1; identify the intended Meta XR SDK / Interaction SDK version before changing package or Android settings.
2. **Quest scene:** add the compatible OpenXR/Meta rig and hand tracking adapter, then verify launch and hand tracking on Quest 3/3S.
3. **Physical interactions:** add grab/rotate/release to reusable component prefabs and map each placement to board-hole coordinates.
4. **Circuit model:** bind terminals and wire endpoints to breadboard strips and rails; make graph rebuild deterministic after moves.
5. **Tutorial and verification:** expose step-specific terminal targets, update the marker, run expected-net checks, and present concrete corrections.
6. **Build and polish:** profile standalone performance, configure Android only after package validation, and build/install on device.

## Scope notes

This phase contains no passthrough, camera access, AR/MR, object detection, or vision processing. The graph checks connectivity only; it is not an electrical simulator.
