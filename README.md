# Phobia Relief Therapy (VR-Based Exposure Therapy System)

A VR-based therapeutic system designed to help users gradually overcome common phobias using controlled exposure therapy in immersive 3D/VR environments.

## Supported Phobias (First Phase)
* Fear of Heights (Acrophobia)
* Fear of Darkness (Nyctophobia)
* Fear of Crowds (Enochlophobia / Social Anxiety)

## Project Flow
Login → Register → Dashboard → Select Phobia → Select Difficulty → Baseline Measurement → Safe Room → Exposure Scene → Feedback → Session Complete.

## Unity Version
* Unity 2022.3 LTS

---

## Commit History & Milestones

### Commit 1: Initial Unity project setup with proper folder structure
* Initialized Unity project using Unity 2022.3 LTS.
* Created proper FYP folder architecture (`Scenes`, `Scripts/Managers`, `Scripts/UI`, `Scripts/Player`, `Scripts/Therapy`, `Scripts/Data`, `Scripts/VR`, `Prefabs`, etc.).
* Set up standard `.gitignore` for Unity to exclude `Library/`, `Temp/`, `Logs/`, `Build/`, `Obj/`.

### Commit 2: Added Login and Register scenes with UI layout
* Created `LoginScene` with UI (Username, Password, Login Button).
* Created `RegisterScene` with UI (Name, Email, Password, Register Button).
* Designed clean, functional UI structures using Unity UI (TextMeshPro).

### Commit 3: Implemented SceneLoader and AuthManager scripts
* Created `SceneLoader.cs` in `Scripts/Managers` (Singleton pattern for robust scene navigation).
* Created `AuthManager.cs` in `Scripts/Managers` to handle simulated authentication without a database.
* Added `UserData.cs` in `Scripts/Data` to persist state (Username, Selected Phobia, Baseline HR) across scenes.

### Commit 4: Added Phobia Selection and Baseline system
* Created `DashboardScene` / `PhobiaSelectionScene`.
* Created `PhobiaSelectionManager.cs` to capture user choices (Height, Darkness, Crowd).
* Created `BaselineScene` and `BaselineManager.cs` to simulate a 10-second vitals baseline check before exposure begins.

### Commit 5: Created Safe Room and initial exposure scenes
* Created `SafeRoomScene` where the user prepares before entering the exposure environment.
* Created `SafeRoomManager.cs` to dynamically route the user to their chosen exposure level.
* Created initial block-out exposure scenes: `HeightScene`, `DarknessScene`, `CrowdScene`.

### Commit 6: Updated README and project documentation
* Added detailed `README.md` containing project details, architectural guidelines, and milestone documentation.

### Commit 7: Developed Global Medical Theme Architecture
* Shifted from manual styling to a strict `ScriptableObject`-driven Global Medical Theme System.
* Created `ThemePreset.cs` to centrally manage all aesthetic values (Medical Blue, Slate Gray, sharp typography sizes).
* Created `ThemeableUI.cs` base component to enforce a crisp, non-blurry, medical-grade UI.
* Developed `UIStyleManager.cs`, a powerful Editor tool to instantly apply the `MedicalTheme` to every scene in the Build Settings.
* Purged old procedural generator scripts to maintain a clean, solid, professional aesthetic.

---

## Architecture Overview

**Core Scripts & Responsibilities:**
- `SceneLoader.cs`: Global scene management.
- `AuthManager.cs`: Local simulated authentication logic for early iterations.
- `UserData.cs`: Static class for holding session-specific data.
- `BaselineManager.cs`: Countdown timer and simulated vitals collection.
- `PhobiaSelectionManager.cs`: User selection persistence.
- `SafeRoomManager.cs`: Dynamic navigation to the final exposure scene.
- `ThemeManager.cs` & `ThemeableUI.cs`: Global styling framework that automatically loads the `MedicalTheme` ScriptableObject from the `Resources` folder to enforce medical-grade UI consistency.

## Note for Contributors / Evaluators
This project relies initially on *simulated* advanced systems (like heart-rate and AI) to prioritize establishing a functional 3D/VR application flow. Real hardware and backend integrations are planned for a later phase.
