# Phobia Relief Therapy

A Unity-based **Virtual Reality Exposure Therapy (VRET)** application that guides users through a structured, clinician-inspired session flow to gradually confront common phobias in safe, controlled virtual environments.

**Repository:** [github.com/ahmedtayyab/PhobiaReliefTherapy](https://github.com/ahmedtayyab/PhobiaReliefTherapy)

---

## What This Project Is

Phobia Relief Therapy is an immersive therapeutic tool built in Unity. It combines proven exposure-therapy principles with VR-ready architecture so users can face fears — heights, darkness, and crowds — at their own pace, without real-world risk.

The app walks users through authentication, phobia selection, physiological baseline measurement, a calming safe room, and (planned) graduated exposure environments. A consistent medical-grade UI theme runs across every screen, and session state is tracked in memory as the user moves between scenes.

---

## Supported Phobias (Phase 1)

| Phobia | Internal key | Exposure scene |
|--------|--------------|----------------|
| Fear of Heights (Acrophobia) | `Height` | `HeightScene` |
| Fear of Darkness (Nyctophobia) | `Darkness` | `DarknessScene` |
| Fear of Crowds (Enochlophobia) | `Crowd` | `CrowdScene` |

---

## Therapy Flow

```
WelcomeScene
  → LoginScene / RegisterScene        (Supabase cloud auth)
    → DashboardScene                  (select phobia: Height, Darkness, or Crowd)
      → BaselineScene                 (10s mock heart-rate + calm 360° panorama)
        → SafeRoomScene               (session summary + Start Exposure)
          → HeightScene | DarknessScene | CrowdScene
            → (exposure content — placeholder)
```

> **Note:** Difficulty selection (Low / Medium / High) is implemented in code but **bypassed in Phase 1**. Selecting a phobia currently sets difficulty to `Low` and stage to `1`, then jumps straight to baseline measurement. AI-driven level selection is planned for Phase 2.

---

## What's Done So Far

### Fully implemented

- **Welcome, Login, and Register** — polished medical-themed UI with email/password validation
- **Supabase authentication** — cloud register/login, user profile storage, auth token in `PlayerPrefs`
- **Dashboard phobia selection** — three phobia tracks with session state stored in `UserData`
- **Baseline measurement scene**
  - 10-second countdown with progress bar
  - Mock heart-rate sensor (70–90 BPM via `SensorManager`)
  - 360° panoramic calm environment (VR skybox + 2D flat-screen fallback)
  - Editor preview: hold **right mouse button** to look around the skybox without a headset
- **Safe Room hub** — displays username, phobia, baseline HR, difficulty; shows safe-room preview image; routes to the correct exposure scene
- **Global medical theme** — `MedicalTheme` ScriptableObject auto-applied on every scene load
- **Scene transitions** — fade-to-black via `SceneLoader` singleton
- **Editor tooling** — menu tools to rebuild auth scenes, UI prefabs, and apply themes across all build scenes

### Partially implemented

| Feature | Status |
|---------|--------|
| **VR (Meta Quest 2)** | XR packages and Oculus loader configured for Android; `VRManager` is mocked; baseline skybox is VR-aware |
| **Heart-rate sensor** | `SensorManager` mock only; Polar H10 integration stubbed for a later phase |
| **Difficulty / staging** | UI code exists (`ShowLevelSelection`) but is bypassed — always Stage 1 / Low |
| **Session persistence** | `DatabaseManager.SaveTherapySession()` exists but is not yet called from gameplay |
| **SQLite plugin** | Bundled under `Assets/SQLite4Unity3d/` but unused — backend is Supabase |

### Placeholder (not yet built)

- `HeightScene`, `DarknessScene`, `CrowdScene` — empty shells (camera, light, canvas only)
- `FeedbackScene` — in build settings but unreachable from code
- `PhobiaSelectionScene` — unused; selection happens on `DashboardScene`
- Real VR controllers, XR Origin, and grab/teleport interactions
- Post-session feedback and session-complete flow

---

## Requirements

| Requirement | Details |
|-------------|---------|
| **Unity** | 2022.3.62f3 LTS (see `ProjectSettings/ProjectVersion.txt`) |
| **Internet** | Required for Supabase authentication |
| **VR (optional)** | Meta Quest 2 + Android SDK/NDK for headset builds |

---

## How to Run (Editor)

1. **Clone the repository**
   ```bash
   git clone https://github.com/ahmedtayyab/PhobiaReliefTherapy.git
   cd PhobiaReliefTherapy
   ```

2. **Open in Unity Hub** with version **2022.3.62f3** (or a compatible 2022.3 LTS release).

3. **Let packages resolve** on first open (XR, TextMeshPro, UGUI, etc.).

4. **Open** `Assets/Scenes/WelcomeScene.unity` (build index 0).

5. **Press Play** and follow the flow:
   - Welcome → Get Started
   - Register or Login (requires internet)
   - Dashboard → pick a phobia
   - Baseline → wait 10 seconds, then Continue
   - Safe Room → Start Exposure (lands in placeholder exposure scene)

### Editor tips

- In **BaselineScene**, hold **right mouse button** to preview the 360° skybox without VR.
- Heart-rate readings are always simulated in Phase 1 (`SensorManager.UseMockSensor = true`).

---

## How to Build

1. **File → Build Settings**
2. Confirm all 11 scenes are enabled in this order:

   | # | Scene |
   |---|-------|
   | 0 | `WelcomeScene` |
   | 1 | `LoginScene` |
   | 2 | `RegisterScene` |
   | 3 | `DashboardScene` |
   | 4 | `PhobiaSelectionScene` |
   | 5 | `BaselineScene` |
   | 6 | `SafeRoomScene` |
   | 7 | `HeightScene` |
   | 8 | `DarknessScene` |
   | 9 | `CrowdScene` |
   | 10 | `FeedbackScene` |

3. Select your target platform:
   - **Windows / Mac / Linux Standalone** — flat-screen testing
   - **Android** — primary VR target (Meta Quest 2 via Oculus XR loader)

4. **Switch Platform** if needed, then **Build** or **Build And Run**.

**App version:** `0.1`

---

## Project Structure

```
Assets/
├── Scenes/              # All therapy and auth scenes
├── Scripts/
│   ├── Managers/        # SceneLoader, AuthManager, VRManager, SensorManager
│   ├── Therapy/         # BaselineManager, SafeRoomManager, PhobiaSelectionManager
│   ├── Data/            # UserData, DatabaseManager (Supabase)
│   ├── Theme/           # MedicalTheme system (ThemePreset, ThemeableUI)
│   ├── UI/              # Style helpers, fade effects, hover states
│   └── Utils/           # AutoBindHelper
├── Editor/              # AuthSceneBuilder, PhobiaUIBuilder, UIStyleManager
├── Resources/           # MedicalTheme, baseline_image, safe_room_preview
├── Materials/           # BaselineSkybox.mat (panoramic skybox)
├── Prefabs/UI/          # Reusable card, button, input prefabs
├── XR/                  # XR Management loaders (Oculus for Android)
└── XRI/                 # XR Interaction Toolkit settings
Packages/manifest.json   # Unity package dependencies
ProjectSettings/         # Build order, XR, input settings
```

---

## Key Scripts

| Script | Role |
|--------|------|
| `SceneLoader` | DontDestroyOnLoad singleton; fade transitions between scenes |
| `AuthManager` | Login/register with validation; calls Supabase via `DatabaseManager` |
| `DatabaseManager` | Supabase REST API — auth, `users` table, `therapy_sessions` table |
| `PhobiaSelectionManager` | Phobia button handlers; stores selection in `UserData`; loads baseline |
| `BaselineManager` | 10s measurement, panoramic skybox, 2D/VR hybrid layout, editor look-around |
| `SafeRoomManager` | Session summary UI, safe-room preview image, routes to exposure scenes |
| `SensorManager` | Mock heart-rate (Polar H10 planned) |
| `VRManager` | Mock VR abstraction (Quest 2 planned) |
| `UserData` | Static in-session state: username, phobia, difficulty, HR, stage |
| `UIThemeAutoApply` | Auto-applies `MedicalTheme` on every scene load |

Several managers (`SceneLoader`, `DatabaseManager`, `VRManager`, `SensorManager`) auto-instantiate on first access — they do not need to be placed manually in scenes.

---

## Backend (Supabase)

Authentication and user data are stored in [Supabase](https://supabase.com):

- **Auth** — email/password signup and login (`/auth/v1`)
- **Users table** — profile records (`/rest/v1/users`)
- **Therapy sessions table** — session history schema ready (`/rest/v1/therapy_sessions`)

> **For contributors:** Supabase URL and anon key are configured in `Assets/Scripts/Data/DatabaseManager.cs`. For production deployments, move credentials to environment-based configuration rather than hardcoding them.

---

## VR / XR Setup

| Platform | XR loader |
|----------|-----------|
| Android (Quest) | Oculus Loader |
| Standalone / WebGL | None (flat-screen) |

Packages installed: XR Management, OpenXR 1.14.3, Oculus XR 4.5.4, XR Interaction Toolkit 2.6.5.

**Current runtime behavior:** `VRManager` reports mock mode. `BaselineManager` is the most VR-aware script — it applies a panoramic skybox when a headset is active and falls back to a 2D image on flat screens. No XR Origin, controllers, or interaction systems are wired into scenes yet.

**Target device:** Meta Quest 2

---

## Editor Tools

Available from the Unity **Tools** menu:

| Menu item | Purpose |
|-----------|---------|
| **Build Auth Scenes** | Regenerate Welcome, Login, and Register scenes |
| **Phobia Relief → Create Style Guide & Prefabs** | Generate UI style guide and reusable prefabs |
| **Phobia Relief → Apply / Build UI** | Apply medical theme and build UI across all scenes |
| **Apply Global Theme** | Apply `MedicalTheme` to current or all build scenes |

---

## Technologies

| Category | Technology |
|----------|------------|
| Engine | Unity 2022.3.62f3 LTS |
| Language | C# |
| UI | Unity UGUI, TextMeshPro |
| Theming | ScriptableObjects + runtime auto-apply |
| Backend | Supabase REST API |
| Local storage | `PlayerPrefs` (auth token) |
| XR / VR | XR Management, OpenXR, Oculus XR, XR Interaction Toolkit |
| Render pipeline | Built-in (panoramic skybox shader) |
| Fonts | Montserrat, Inter (TMP SDF) |

---

## Roadmap (Phase 2+)

- [ ] Build immersive exposure environments (heights, darkness, crowds)
- [ ] Wire up `FeedbackScene` and post-session flow
- [ ] Call `SaveTherapySession()` to persist sessions to Supabase
- [ ] Enable AI-driven difficulty selection (`ShowLevelSelection`)
- [ ] Integrate Polar H10 heart-rate sensor
- [ ] Replace `VRManager` mock with live Quest 2 / OpenXR initialization
- [ ] Add XR Origin, controllers, and in-VR interaction
- [ ] Move Supabase credentials to secure configuration

---

## Session State (`UserData`)

Held in memory for the duration of a play session:

| Field | Example values |
|-------|----------------|
| `Username` | From Supabase profile |
| `SelectedPhobia` | `"Height"`, `"Darkness"`, `"Crowd"` |
| `SelectedDifficulty` | `"Low"`, `"Medium"`, `"High"` (currently always `"Low"`) |
| `BaselineHeartRate` | Simulated 70–90 BPM |
| `CurrentStage` | `1`–`3` (currently always `1`) |

---

## License

See repository license file if present. Otherwise, contact the repository owner for usage terms.
