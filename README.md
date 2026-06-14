# Phobia Relief Therapy

A Unity-based **Virtual Reality Exposure Therapy (VRET)** application that guides users through a structured, clinician-inspired session flow to gradually confront common phobias in safe, controlled virtual environments.

**Repository:** [github.com/ahmedtayyab/PhobiaReliefTherapy](https://github.com/ahmedtayyab/PhobiaReliefTherapy)

---

## Table of Contents
1. [Overview & Core Philosophy](#overview--core-philosophy)
2. [Supported Phobias (Phase 1)](#supported-phobias-phase-1)
3. [VR & Interactive Mechanics (Deep Dive)](#vr--interactive-mechanics-deep-dive)
    - [Dynamic VR Rig Generation (`VRRigBuilder`)](#dynamic-vr-rig-generation-vrrigbuilder)
    - [Physical Head Tracking & Panoramic Exposure](#physical-head-tracking--panoramic-exposure)
    - [Ergonomic VR Controllers & Comfort Pitch](#ergonomic-vr-controllers--comfort-pitch)
    - [High-Visibility Laser Pointers](#high-visibility-laser-pointers)
    - [Draggable & Adjustable VR Keyboard](#draggable--adjustable-vr-keyboard)
4. [UI Positioning & Adaptive Design System](#ui-positioning--adaptive-design-system)
    - [Adaptive Canvas Mounting](#adaptive-canvas-mounting)
    - [Medical Theme Engine](#medical-theme-engine)
5. [Cloud Backend & Password Recovery (Supabase)](#cloud-backend--password-recovery-supabase)
    - [Authentication Scheme](#authentication-scheme)
    - [Hosted Web Recovery Redirect Loop Fix](#hosted-web-recovery-redirect-loop-fix)
6. [Therapy Session Flow](#therapy-session-flow)
7. [Technical Requirements](#technical-requirements)
8. [Setup & Editor Run Guide](#setup--editor-run-guide)
9. [Build & Platform Configuration](#build--platform-configuration)
10. [Editor Tools Reference](#editor-tools-reference)
11. [Project Directory Structure](#project-directory-structure)
12. [Roadmap (Phase 2+)](#roadmap-phase-2)

---

## Overview & Core Philosophy

**Phobia Relief Therapy** is an immersive therapeutic tool built in Unity. It leverages the power of Virtual Reality to implement clinician-approved VRET flows, allowing users to confront common anxiety-inducing triggers at their own pace. By placing the user in high-fidelity 360-degree panoramic environments, the system triggers real psychological responses while ensuring safety, predictability, and full user control.

The system features an automated, runtime-constructed VR rig, code-driven world-space keyboards, and a cloud-based authentication system backed by Supabase. To ease developer workflows, the system is designed to run seamlessly in the Unity Editor with flat-screen fallback options (including look-around capabilities via right-mouse holding) and deploy directly to mobile VR platforms (such as the Meta Quest 2).

---

## Supported Phobias (Phase 1)

The application models exposure categories using internal lookup keys and maps them to specialized exposure scenes:

| Phobia | Key | Scene Name | Description |
| :--- | :--- | :--- | :--- |
| **Fear of Heights (Acrophobia)** | `Height` | `HeightScene` | Controlled height-exposure environment featuring panoramic height skyboxes and 2D fallback background support. |
| **Fear of Darkness (Nyctophobia)** | `Darkness` | `DarknessScene` | Controlled darkness-exposure environment featuring progressive illumination stages and panoramic dark skyboxes. |
| **Fear of Crowds (Enochlophobia)** | `Crowd` | `CrowdScene` | Controlled crowd-exposure environment featuring high-density crowd panoramic skyboxes and 2D fallback background support. |

---

## VR & Interactive Mechanics (Deep Dive)

### Dynamic VR Rig Generation (`VRRigBuilder`)

The application avoids hardcoded VR cameras or static rig prefabs. Instead, the static helper class [VRRigBuilder.cs](file:///c:/Users/ahmad/PhobiaReliefTherapy/Assets/Scripts/VR/VRRigBuilder.cs) dynamically constructs a native **XR Interaction Toolkit** rig at runtime.

- **Double-Height Camera Fix**: VR devices in `Floor` tracking mode automatically add the player's physical height to the camera. If the root origin is also placed at a default eye level (e.g., `1.7m`), the virtual camera and hands float at an unrealistic `3.4m` altitude. `VRRigBuilder` fixes this by initializing the `XR Origin` root coordinates strictly on the floor level (`Y = 0f`) and offset-reparenting the camera.
- **Graphic Raycasters & Event Handlers**: On setup, the runtime removes flat-screen `GraphicRaycaster` components from canvases and replaces them with `TrackedDeviceGraphicRaycaster`. Similarly, standard input modules are swapped for `XRUIInputModule` to route trigger actions seamlessly.

### Physical Head Tracking & Panoramic Exposure

In therapy scenes (like [BaselineScene](file:///c:/Users/ahmad/PhobiaReliefTherapy/Assets/Scenes/BaselineScene.unity) and [DarknessScene](file:///c:/Users/ahmad/PhobiaReliefTherapy/Assets/Scenes/DarknessScene.unity)), the user rotates their head physically in 360 degrees to view panoramic skyboxes.
- **Headset Mapping**: The `VRRigBuilder` mounts a `TrackedPoseDriver` targeting the headset's `Center` eye to track spatial rotation and translation.
- **Flat-Screen Fallback**: When testing in the Unity Editor without a VR headset connected, the `BaselineManager` and `DarknessManager` allow developers/users to hold down the **Right Mouse Button** and drag to rotate the camera, simulating headset panning.

### Ergonomic VR Controllers & Comfort Pitch

Controllers are mapped to the user's physical hands via `XRController` tracking nodes.
- **Comfort Pitch Angle**: Aiming standard straight laser beams at virtual UI panels often forces users to hold their hands at unnatural angles, causing wrist strain. `VRRigBuilder` configures a child object named `Ray Origin` parented to the controller, applying a **15-degree pitch down** rotation. This allows users to aim comfortably with a natural, relaxed wrist posture.

### High-Visibility Laser Pointers

Interactable lines are rendered from each hand controller using custom styling.
- Cyan laser pointers are established dynamically using an `XRInteractorLineVisual` and a `LineRenderer` with custom gradients.
- Valid and invalid raycasts use a consistent cyan visual gradient, scaling from high alpha (`0.8`) near the hand to low alpha (`0.1`) at the beam's tip, ensuring clear guidance without obstructing the user's view.

### Draggable & Adjustable VR Keyboard

To eliminate dependency on OS-level keyboards, the system includes a custom, code-driven world-space VR keyboard ([VRKeyboard.cs](file:///c:/Users/ahmad/PhobiaReliefTherapy/Assets/Scripts/VR/VRKeyboard.cs)). It automatically instantiates in front of the camera when a user clicks on input fields in login or registration scenes.

```
+-------------------------------------------------------------+
|  [ Type here... ]                 ✥ Grip+Stick to move      |
+-------------------------------------------------------------+
|  [1]  [2]  [3]  [4]  [5]  [6]  [7]  [8]  [9]  [0]  [-]      |
|  [q]  [w]  [e]  [r]  [t]  [y]  [u]  [i]  [o]  [p]           |
|  [a]  [s]  [d]  [f]  [g]  [h]  [j]  [k]  [l]  [;]           |
|  [z]  [x]  [c]  [v]  [b]  [n]  [m]  [,]  [.]                |
|                                                             |
|  [ CAPS ]        [       SPACE       ]        [ ⌫ ] [✓ Done]|
+-------------------------------------------------------------+
```

- **Interactive Drag & Positioning**:
  - Users can press and hold the **Grip** button on either controller to grab the keyboard.
  - While gripping, moving the **Thumbstick up/down** (Y-axis) increases or decreases the floating distance of the keyboard (clamped between `0.3m` and `2.0m`).
  - Moving the **Thumbstick left/right** (X-axis) rotates/yaws the keyboard, allowing the user to angle it comfortably.
- **Feedback & Password Masking**: The top display bar mirrors typed input dynamically. When targeting password input fields, characters are masked with security dots (`●`).

---

## UI Positioning & Adaptive Design System

### Adaptive Canvas Mounting

In standard flat-screen mode, canvases render as `ScreenSpaceOverlay`. When VR is detected at scene initialization, [UIThemeAutoApply.cs](file:///c:/Users/ahmad/PhobiaReliefTherapy/Assets/Scripts/Theme/UIThemeAutoApply.cs) dynamically adapts canvas parameters:
- **Render Mode**: Swapped to `ScreenSpaceCamera` pointing to the VR camera.
- **Plane Distance**: Positioned at a comfortable distance of `1.5` meters. Distances closer than `1.0` meter can cause eye strain and visual clipping.
- **Field of View Safe Zone**: Canvas anchors are constrained slightly inward toward the center to ensure UI buttons, status messages, and timer indicators are not cut off by the headset's physical lens borders.
- **Return Button Placement**: The critical "Return to Phobia Selection" button is centered and positioned lower in the physical viewport `(0.5, 0.25)` to remain highly visible without requiring users to look upward excessively.

### Medical Theme Engine

The application defines visual guidelines using a global `MedicalTheme` ScriptableObject:
- **Primary Color**: Dark Slate Blue / Navy (HSL: `225, 25%, 15%`) for primary frames.
- **Accent Color**: Clinical Teal / Cyan (HSL: `180, 70%, 45%`) representing active state, indicators, and buttons.
- **Alert Colors**: Clean Green (HSL: `140, 50%, 45%`) for registration successes, and Warning Red (HSL: `0, 70%, 45%`) for input validation errors.

---

## Cloud Backend & Password Recovery (Supabase)

### Authentication Scheme

The authentication subsystem is implemented in [AuthManager.cs](file:///c:/Users/ahmad/PhobiaReliefTherapy/Assets/Scripts/Managers/authManager.cs) and uses Supabase's REST endpoints:
- **Endpoint Routings**:
  - `/auth/v1/signup` (Signup processing)
  - `/auth/v1/token?grant_type=password` (User login verification)
- **Token Storage**: Successful logins retrieve an access token which is persisted securely in local `PlayerPrefs`.

### Hosted Web Recovery Redirect Loop Fix

When users request a password recovery email, Supabase sends a transaction link that redirects to the default Site URL. By default, this can lead to a loop where recovery parameters are lost, resulting in a "no recovery session found" error on reset forms.

This is solved via a dual-page hosting configuration under the `docs/` folder:

```
[Supabase Recovery Link]
          │
          ▼
docs/registration-success/index.html   (Intercepts parameter hash)
          │
      (Redirects via window.location.replace)
          ▼
docs/password-reset/index.html         (Loads Supabase JS client and resets password)
```

1. **docs/registration-success/index.html**: Serves as the landing redirect target. A lightweight script checks the URL search query or fragment hash:
   ```javascript
   const url = new URL(window.location.href);
   const hash = window.location.hash;
   const isRecovery = url.searchParams.get("type") === "recovery" ||
                      url.searchParams.get("token_hash") ||
                      url.searchParams.get("code") ||
                      hash.includes("type=recovery");
   
   if (isRecovery) {
     window.location.replace("../password-reset/" + url.search + hash);
   }
   ```
2. **docs/password-reset/index.html**: Captures the forwarded session keys, establishes an active session with the Supabase JS library, prompts the user with a clinical UI to input their new password, and updates it securely using `supabase.auth.updateUser`.

---

## Therapy Session Flow

```
+----------------------+
|     WelcomeScene     | --> Intro and "Get Started" buttons
+----------------------+
           │
           ▼
+----------------------+
|  Login/RegisterScene | --> Cloud Auth (Supabase validation)
+----------------------+
           │
           ▼
+----------------------+
|    DashboardScene    | --> Phobia Selection Grid (Height, Darkness, Crowd)
+----------------------+
           │
           ▼
+----------------------+
|    BaselineScene     | --> 10-second calibration & simulated BPM tracking
+----------------------+
           │
           ▼
+----------------------+
|    SafeRoomScene     | --> Confirms baseline statistics and launches exposure
+----------------------+
           │
           ▼
+----------------------+
|    Exposure Scene    | --> Guided virtual environment (e.g. DarknessScene)
+----------------------+
```

*Note: In Phase 1, difficulty selections default to **Low (Stage 1)**. AI-driven staging is scheduled for Phase 2.*

---

## Technical Requirements

* **Unity Engine**: `2022.3.62f3 LTS` (recommended configuration).
* **XR Framework**: XR Plugin Management with OpenXR and Oculus XR Plug-ins enabled.
* **Input System**: Unity Input System package.
* **Text Rendering**: TextMesh Pro with customized Montserrat/Inter SDF font atlases.
* **Target Platforms**: Meta Quest 2 / Quest 3, Android VR, and Windows Standalone fallback.

---

## Setup & Editor Run Guide

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/ahmedtayyab/PhobiaReliefTherapy.git
   cd PhobiaReliefTherapy
   ```
2. **Open in Unity**: Launch Unity Hub, click **Add project from disk**, and target the cloned folder using Unity version `2022.3.62f3`.
3. **Resolve Package Dependencies**: Allow Unity to download packages (XR Core Utilities, XR Interaction Toolkit, TextMesh Pro).
4. **Configure Scene Entry Point**:
   - Open [WelcomeScene](file:///c:/Users/ahmad/PhobiaReliefTherapy/Assets/Scenes/WelcomeScene.unity) inside `Assets/Scenes/`.
   - Press the **Play** button in the Editor to test the flat-screen flow.
   - Hold the **Right Mouse Button** during the baseline or darkness calibration to pan the skybox camera.

---

## Build & Platform Configuration

1. Open **File** > **Build Settings**.
2. Verify that the build scenes are configured in this exact order:
   - `0`: `WelcomeScene`
   - `1`: `LoginScene`
   - `2`: `RegisterScene`
   - `3`: `DashboardScene`
   - `4`: `PhobiaSelectionScene`
   - `5`: `BaselineScene`
   - `6`: `SafeRoomScene`
   - `7`: `HeightScene`
   - `8`: `DarknessScene`
   - `9`: `CrowdScene`
   - `10`: `FeedbackScene`
3. **Select Target Platform**:
   - Choose **Android** for VR deployment (Meta Quest devices).
   - Ensure the active XR loader (Oculus/OpenXR) is enabled under **Project Settings** > **XR Plug-in Management**.
4. Click **Build And Run**.

---

## Editor Tools Reference

The codebase features customized productivity extensions accessible from the top toolbar:

| Tools Path | Function |
| :--- | :--- |
| **Tools** > **Build Auth Scenes** | Automatically regenerates the UI layout structures for the login, welcome, and signup scenes. |
| **Tools** > **Phobia Relief** > **Create Style Guide & Prefabs** | Exports default UI templates, layouts, input containers, and button configurations to `Assets/Prefabs/UI/`. |
| **Tools** > **Phobia Relief** > **Apply / Build UI** | Traverses build scenes and updates canvases with standard UI themes and font templates. |
| **Tools** > **Apply Global Theme** | Forces application of the `MedicalTheme` properties to the current active scene hierarchy. |

---

## Project Directory Structure

```
Assets/
├── Editor/              # Developer menu scripts (AuthSceneBuilder, DarknessSceneBuilder)
├── Materials/           # Exposure skybox materials (BaselineSkybox.mat, DarknessSkybox.mat)
├── Prefabs/UI/          # Prefab templates for UI cards, inputs, and buttons
├── Resources/           # 360 panoramic assets, skyboxes, and the MedicalTheme asset
├── Scenes/              # Unified scene flow sequence files (Welcome, Login, Darkness, etc.)
├── Scripts/
│   ├── Data/            # DatabaseManager (Supabase connection) and UserData states
│   ├── Managers/        # Core singletons: SceneLoader, AuthManager, VRManager, SensorManager
│   ├── Theme/           # Stylers: MedicalTheme asset configurations and UIThemeAutoApply rules
│   ├── Therapy/         # Scenario managers: BaselineManager, SafeRoomManager, DarknessManager
│   ├── UI/              # Transition shaders, UI transitions, button hover scripts
│   ├── Utils/           # Auto-bindings and common reflection code
│   └── VR/              # Dynamic XR systems: VRRigBuilder, VRKeyboard, VRKeyboardTrigger
├── XR/                  # XR Settings and plugin loader configurations
└── XRI/                 # Controller profile mappings and interaction definitions
docs/                    # GitHub Pages hosted folder containing signup and reset pages
```

---

## Roadmap (Phase 2+)

* [ ] **Immersive 3D Exposure**: Replace Skybox overlays in `HeightScene` and `CrowdScene` with detailed 3D models and crowds.
* [ ] **Polar H10 Integration**: Connect Bluetooth Low Energy APIs to read real-time heart rate via `SensorManager.cs`.
* [ ] **AI-driven Exposure Staging**: Activate the difficulty loader in dashboard menus to adjust triggers dynamically.
* [ ] **Backend Logging**: Call `DatabaseManager.SaveTherapySession()` at the end of sessions to log statistics (BPM variations, exposure duration) back to Supabase.
