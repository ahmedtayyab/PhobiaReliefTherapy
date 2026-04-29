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

## Project Overview

Phobia Relief Therapy is an innovative Virtual Reality (VR) application designed to assist users in overcoming specific phobias through Virtual Reality Exposure Therapy (VRET). By immersing users in controlled, safe, and gradually intensifying virtual environments, the system allows individuals to confront their fears at their own pace without real-world risks. 

Exposure therapy is a proven psychological treatment, and by combining it with modern VR technology, this system provides an accessible, immersive, and highly effective therapeutic tool.

### Key Features
* **Secure Session Management:** Local authentication and user data persistence to track therapy progress across sessions.
* **Customizable Therapy Modules:** Users can select their target phobia (Heights, Darkness, Crowds) and scale the intensity/difficulty of the exposure.
* **Physiological Baseline Simulation:** A pre-exposure module that simulates gathering a user's resting heart rate to establish a baseline before the stressful environment is introduced.
* **Immersive Preparation (Safe Room):** A neutral "Safe Room" environment where users can mentally prepare before dynamically transitioning into the exposure scene.
* **Global Medical-Grade Architecture:** A robust `ScriptableObject`-driven UI framework that strictly enforces a clean, sharp, and highly professional medical aesthetic across all screens.

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
