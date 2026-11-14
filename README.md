# BenchFlow XR — VR & Desktop Interactive Electronics Workbench

An immersive virtual electronics lab designed for VR headsets (Meta Quest 3/3S, OpenXR, PCVR) with seamless Desktop Preview support.

## Overview

BenchFlow XR guides learners through building and verifying electronic circuits step-by-step on a solderless breadboard with an Arduino-compatible controller:

1. **Step 1: Place the Red LED** (Long leg Anode → `14c`, Short leg Cathode → `16c`).
2. **Step 2: Add the 220 Ω Resistor** (Bridges row `8d` to `14d`).
3. **Step 3: Connect Jumper Wires** (Pin 13 → `8b`, GND → `16a`).
4. **Step 4: Run the Blink Program** (Tap the 3D `RUN` button → verifies circuit and runs live Arduino Blink loop).

---

## Features

- **VR 6-DOF Hand & Controller Tracking:**
  - Built on standard `UnityEngine.XR.InputDevices` (zero external bloat, compatible with Meta Quest Link, AirLink, Standalone APK, OpenXR, and SteamVR).
  - Grab, lift, move, and rotate components using Grip or Trigger.
  - Thumbstick horizontal axis rotates held parts in 90° increments.
  - Laser pointer raycast with reticle for distant selection and button tapping.
  - Subtle haptic feedback impulses on grab, snap, and button press.

- **Desktop Preview Harness:**
  - Left-click drag to grab and place components.
  - Press `R` or scroll mouse wheel to rotate held parts 90°.
  - Left-click on Pin 13 / GND or breadboard holes to draw wires.
  - Keyboard shortcuts:
    - `1`: Connect Pin 13 to row `8b` (Red jumper wire)
    - `2`: Connect GND to row `16a` (Cyan jumper wire)
    - `Space`: Tap the RUN button / verify circuit.

- **Physically Aligned Breadboard & Discrete Components:**
  - Standard 2.54 mm pitch grid aligned with the 3D model (rows 1–63 along X, columns a–j along Z).
  - Discrete 220 Ω Resistor with ceramic body, 4 color code bands (Red-Red-Brown-Gold), and 15.24 mm lead span.
  - Discrete 5mm Red LED with dome lens, cathode flat rim, long anode wire, short cathode wire, and dynamic point light.

- **Deterministic Circuit Netlist & Diagnostic Validator:**
  - Evaluates connectivity through real breadboard terminal strips (`strip:A:8`, `strip:A:14`, `strip:A:16`).
  - Supports non-polarized resistor in either orientation.
  - Detects and diagnoses specific user mistakes:
    - Same-strip short circuits (e.g. both LED legs in row 14).
    - Direct D13-to-GND shorts without load.
    - Reversed LED polarity (helpful message instructing how to orient anode and cathode).
    - Missing wires or bridged rows.

- **Live Arduino Blink Simulation:**
  - Displays Arduino C++ sketch on the floating instruction card.
  - When verified, status turns to `RUNNING`.
  - Breadboard Red LED and Arduino D13 indicator blink synchronously at 1 Hz (500 ms ON / 500 ms OFF).
  - Wires pulse with animated current flow.

---

## Getting Started

1. Open the project in Unity (Unity 6 / 6000.x).
2. To rebuild or customize the scene: run **BenchFlow → Create VR Prototype Scene** in the Unity menu bar.
3. Open `Assets/BenchFlow/Scenes/BenchFlowPrototype.unity`.
4. Press **Play**:
   - In VR: put on your headset and use Touch controllers / hands.
   - On Desktop: use mouse drag, `R` to rotate, `1`/`2` to wire, and click `RUN`.
