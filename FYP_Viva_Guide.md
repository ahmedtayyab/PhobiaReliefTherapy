# Phobia Relief Therapy in VR

## Final Year Project - Viva Guide

Use this document for the oral defense. Values match the current Unity code.

**Do not quote older docs** (`Project_Explanation.md`, parts of `README.md`) if they still say 20 seconds / 30 seconds. The live timers are **60 seconds baseline** and **120 seconds exposure**.

---

## How to turn this file into a PDF

**Easiest:** open `FYP_Viva_Guide.html` in Chrome or Edge, press **Ctrl+P**, choose **Save as PDF**, enable **Background graphics**.

**Online Markdown:** copy this `.md` file into [md2pdf.net](https://md2pdf.net), [dillinger.io](https://dillinger.io), or [markdown-pdf](https://www.markdowntopdf.com).

---

# 1. One-minute pitch

This is a **Virtual Reality Exposure Therapy (VRET)** application built in Unity. The patient logs in, sits in a calm baseline room while Polar H10 heart rate and headset movement are averaged for **60 seconds**, then an **AI decision tree** selects Beginner, Intermediate, or Advanced. They pass through a **Safe Room**, then a **2-minute** 360-degree exposure for heights, darkness, or crowds. Live **panic scoring** can lower intensity or return them to the Safe Room. Session data is stored locally and synced to **Supabase**.

**Engine:** Unity 2022.3.62f3  
**VR:** OpenXR + XR Interaction Toolkit  
**Headset:** Meta Quest (Quest Link for the live Polar demo)  
**Sensor:** Polar H10 via Python BLE bridge  
**Cloud:** Supabase (PostgreSQL + Auth)

---

# 2. Polar H10 - commands (memorize this)

Live heart rate does **not** come from Unity BLE. A Python script on the **same Windows PC** reads the strap over Bluetooth and sends BPM to Unity on **UDP port 7878**.

### One-time install

```bash
pip install bleak
```

If `pip` is not found:

```bash
python -m pip install bleak
```

### Every demo (keep this window open)

Open **PowerShell** or Command Prompt. Go to the project folder, then run:

```bash
cd C:\Users\ahmad\PhobiaReliefTherapy
python tools\polar_hr_bridge.py
```

You should see:

```text
Scanning for Polar heart rate sensor...
Found Polar H10 ... Connecting...
Streaming to Unity at 127.0.0.1:7878
BPM: 78
BPM: 79
```

Unity must already be in **Play** mode (or start Play right after). Wear the strap, **wet the electrodes**, keep the strap snug.

### Demo hardware setup

1. Quest connected with **Quest Link (USB)**. USB Link does **not** need Wi-Fi. Air Link **does**.
2. Polar H10 on the chest, electrodes wet.
3. Laptop Bluetooth **on**.
4. `python tools\polar_hr_bridge.py` running.
5. Unity Editor **Play**, `forceMockSensor` left **false**.

### What not to promise

A standalone Quest **APK cannot** use this live Polar path. The bridge talks to `127.0.0.1` on the PC. Do not claim a wireless headset-only Polar demo.

---

# 3. Sentences to memorize

**What is the AI?**  
A **rule-based decision tree**, not a trained neural network. Same inputs the SRS names: change in heart rate, head movement, and time spent in stress.

**Why not Random Forest?**  
Random Forest needs labelled clinical data and a training pipeline. A decision tree is **deterministic, inspectable, and matches the SRS formula**. We kept the word AI because the SRS uses it for this adaptive engine.

**How is panic detected?**  
`PanicScore = 0.5 * deltaHR + 0.3 * HeadMovement + 0.2 * TimeSpent`  
Below 10: continue. 10 to 18: lower stage. 18 and above: stop / Safe Room.

**How is the starting stage chosen?**  
After baseline, `ExposureStageAdvisor` maps resting BPM and movement to stage 1, 2, or 3. Manual Beginner / Intermediate / Advanced is **hidden**. AI picks.

**Live Polar on a Quest APK?**  
No. Panel demo is **Quest Link + `polar_hr_bridge.py` on the laptop**.

**Appointments?**  
Out of scope for this FYP.

**Offline?**  
Yes. Local JSON for accounts and sessions. Supabase sync when online.

---

# 4. How the AI works

There are **two decision trees**, both hand-written C#. No sklearn, no weights file, no training.

## Tree A - starting stage

**File:** `Assets/Scripts/Therapy/ExposureStageAdvisor.cs`  
**Inputs:** baseline BPM, baseline head-movement score.

```text
if BPM <= 95 and movement <= 16
    Stage 3  Advanced
else if BPM <= 108 and movement <= 28
    Stage 2  Intermediate
else
    Stage 1  Beginner
```

Constants: `HeartRateBleConstants.cs`  
- `StageAdvancedMaxBpm = 95`  
- `StageIntermediateMaxBpm = 108`

Live Polar rest is typically **70 to 95 BPM**, so a calm seated patient can still get Advanced.

`AllowManualStageSelection` is **false**. If it were on, AI would still cap a requested high stage if vitals were too high.

**Viva line:**  
*"The AI is a clinical decision tree. Thresholds are physiological, not learned. We can walk through every if-statement."*

## Tree B - live recommendations

**Files:** `PanicDetectionService.cs`, `AdaptiveExposureController.cs`  
Every **1 second** during exposure, `ExposureSessionMonitor` samples heart rate and agitation.

| Score | Class | AI says | What the app does |
| --- | --- | --- | --- |
| below 10 | Calm Stress | Continue Exposure | After 20 s calm and 12 s since last change, HUD may suggest advancing next session |
| 10 to 18 | Panic Detected | Repeat Level | After 8 s cooldown, lower stage by 1 (minimum 1) |
| 18+ | Severe Panic | Stop / Safe Room | If score is at least 22 **and** delta HR is at least 20, and 20 s grace has passed, return to Safe Room |

**Forced gates**

- delta HR greater than **36** BPM raises the score to at least 22  
- delta HR greater than **32** BPM raises a low score to at least 10  

**Visible AI:** animated AI badge (`UiAiMark`) plus recommendation text.

---

# 5. Panic formula (write this on the board)

```text
deltaHR     = currentBPM - BaselineHeartRate

HM score    = 0 if movement is not above baseline
            = min(100, (currentMove - baselineMove) / baselineMove * 45)

Time score  = 0 if stress seconds = 0 or deltaHR <= 5
            = (stressSeconds / 15) * 100   if under 15 seconds
            = 100                          if 15 seconds or more

PanicScore  = 0.5 * deltaHR  +  0.3 * HM  +  0.2 * TimeSpent
```

**A stress second is counted when**

- delta HR is greater than **16**, **or**
- movement is greater than **2.4 times** baseline movement (floor 15)

**Head agitation** (`HeadMovementTracker.cs`)

```text
Agitation = 0.45 * rotationSpeed + 24 * movementFrequency + 0.35 * jerk
```

Freeze is tracked but **not** used in the panic formula.

Safety does **not** fire in the first **20 seconds** of exposure (`SafetyGraceSeconds`), so sitting down in VR cannot immediately abort.

**Emergency (not AI):** Quest **A** (right) or **X** (left), or keyboard **X** in the Editor, returns to the Safe Room (`VrEmergencyInput.cs`).

---

# 6. Important values

Change timers **only** in `SessionTiming.cs`. Exposure scenes read that class at runtime.

| What | Value | File |
| --- | --- | --- |
| Baseline duration | 60 s | `SessionTiming.BaselineSeconds` |
| Exposure duration | 120 s | `SessionTiming.ExposureSeconds` |
| Safe Room countdown | 5 s | `SessionTiming.SafeRoomCountdownSeconds` |
| Skip timers (Editor) | F1 | `EditorTherapySimulator` |
| Polar UDP port | 7878 | `HeartRateBleConstants.DefaultUdpPort` |
| Polar bridge command | `python tools\polar_hr_bridge.py` | `tools/polar_hr_bridge.py` |
| BLE Heart Rate UUID | `0000180d-0000-1000-8000-00805f9b34fb` | Polar / Bluetooth SIG |
| Resting BPM window | 55 to 95 | `HeartRateBleConstants` |
| Therapy clamp | 40 to 180 BPM | same |
| Device clamp | 30 to 220 BPM | Polar spec |
| Stage 3 max rest BPM | 95 | `StageAdvancedMaxBpm` |
| Stage 2 max rest BPM | 108 | `StageIntermediateMaxBpm` |
| Panic weights | 50% / 30% / 20% | `PanicDetectionService` |
| Safe Room score gate | 22 | `SafeRoomScoreThreshold` |
| delta HR force-evaluate | 36 | `DeltaHrImmediateEvaluateThreshold` |
| Skybox intensity | 1.0 / 1.15 / 1.35 | `TherapySessionHelper` |
| World canvas scale | about 0.001 | 1000 px is about 1 m |
| Grip drag distance | 0.45 m to 3.2 m | `VRMenuDragger` |
| XR Origin Y | 0 | double-height fix in `VRRigBuilder` |
| Comfort laser pitch | 15 degrees down | `VRRigBuilder` |
| Theme accent | #FF5722 | `ThemePreset` |
| Theme background | #0D0E12 | charcoal |
| Seed admin | admin@phobia.local / admin123 | `LocalAccountStore` |
| Seed user | user@phobia.local / user1234 | same |

---

# 7. Session flow (demo path)

```text
WelcomeScene
    -> LoginScene or RegisterScene
    -> DashboardScene
    -> BaselineScene                 (60 s average HR + head movement)
    -> PhobiaSelectionScene          (Height / Darkness / Crowd; AI picks stage)
    -> SafeRoomScene                 (review stage, 5 s countdown, Continue)
    -> HeightScene or DarknessScene or CrowdScene   (120 s + live AI)
    -> FeedbackScene                 (rating, graphs, save)
    -> DashboardScene                (View stats / Analyze performance)
```

Admin login opens `AdminScene`.

**F1** skips the long timers in the Editor so rehearsal does not take three minutes.

---

# 8. Tools and names (say these)

| Layer | Tool |
| --- | --- |
| Editor / engine | Unity 2022.3.62f3 LTS |
| VR runtime | OpenXR |
| VR input / lasers | Unity XR Interaction Toolkit (XRI) |
| Headset | Meta Quest + Quest Link (USB) |
| Text | TextMesh Pro |
| Heart rate hardware | Polar H10 (BLE Heart Rate service 0x180D) |
| PC BLE to Unity | Python, package **bleak**, script `tools/polar_hr_bridge.py`, UDP 127.0.0.1:7878 |
| Cloud database | Supabase (PostgreSQL, Auth, REST, Row Level Security) |
| Local cache | JSON in `Application.persistentDataPath` |
| Schema apply | `docs/supabase-admin-schema.sql` via **Tools > PRT > Apply Cloud Setup** |
| Logic tests | **Tools > PRT > Run SRS Logic Tests** |
| Password recovery | GitHub Pages under `docs/` |
| Version control | Git, branch `fyp2` |

**Not used in the shipped AI:** TensorFlow, PyTorch, sklearn Random Forest, Unity ML-Agents.

---

# 9. File map

Paths are under `Assets/Scripts/` unless noted.

## Therapy and AI

| File | What it contains |
| --- | --- |
| `Therapy/PanicDetectionService.cs` | SRS panic formula and classification. Memorize this file. |
| `Therapy/ExposureStageAdvisor.cs` | Baseline to starting stage. |
| `Therapy/AdaptiveExposureController.cs` | Live HUD, lower stage, slow Safe Room return. |
| `Therapy/ExposureSessionMonitor.cs` | 1 Hz sampling, stress seconds, 20 s grace. |
| `Therapy/BaselineManager.cs` | 60 s average heart rate, then advisor. |
| `Therapy/SafeRoomManager.cs` | Pre-exposure briefing and abort landing. |
| `Therapy/HeightManager.cs` | Acrophobia skybox, 120 s session. |
| `Therapy/CrowdManager.cs` | Crowd phobia skybox session. |
| `Therapy/DarknessManager.cs` | Nyctophobia. Three images by stage. |
| `Therapy/PhobiaSelectionManager.cs` | Pick track. Manual difficulty hidden. |
| `Therapy/PhobiaCatalog.cs` | Low / Medium / High mapped to stages 1 / 2 / 3. |
| `Therapy/TherapySessionHelper.cs` | Start, complete, abort, persist, emergency button. |
| `Therapy/DashboardManager.cs` | Patient home. |
| `Therapy/DashboardSessionHistory.cs` | View stats / analyze performance. |
| `Therapy/FeedbackManager.cs` | Post-session rating and vitals summary. |

## Data

| File | What it contains |
| --- | --- |
| `Data/UserData.cs` | Current user, phobia, `CurrentStage`, admin flag. |
| `Data/SessionMetrics.cs` | Peak / average HR, movement, panic list, abort flag. |
| `Data/SessionTiming.cs` | Only place for 60 / 120 / 5. |
| `Data/SessionVitalsHistory.cs` | Sparkline buffers for HUD graphs. |
| `Data/LocalSessionStore.cs` | `PhobiaReliefTherapy_sessions.json` |
| `Data/LocalAccountStore.cs` | `PhobiaReliefTherapy_accounts.json`, seed logins. |
| `Data/DatabaseManager.cs` | Supabase Auth, session POST / GET, merge local + cloud. |
| `Data/CloudSetup.cs` | Copies SQL and opens the Supabase SQL editor. |

## Sensors and managers

| File | What it contains |
| --- | --- |
| `Managers/SensorManager.cs` | Live UDP first. `forceMockSensor` is false by default. |
| `Managers/HeartRate/UdpHeartRateProvider.cs` | Parses packets like `BPM:72`. |
| `Managers/HeartRate/MockHeartRateProvider.cs` | Editor fallback. |
| `Managers/HeartRate/HeartRateBleConstants.cs` | UUIDs, BPM gates, UDP port. |
| `Managers/HeadMovementTracker.cs` | Headset rotation, jerk, freeze. |
| `Managers/PolarSensorDebugUI.cs` | Editor overlay. F8 collapses it. |
| `Managers/authManager.cs` | Login, register, recovery UI. |
| `Managers/SceneLoader.cs` | Fade and scene load. |
| `Managers/VRManager.cs` | Headset present vs Editor fallback. |

## VR and UI

| File | What it contains |
| --- | --- |
| `VR/VRRigBuilder.cs` | Runtime XR Origin, lasers, Y = 0, 15 degree pitch. |
| `VR/VrWorldMenu.cs` | Overlay to World Space so the headset can see menus. |
| `VR/VRMenuDragger.cs` | Grip drag. Stick Y = distance. Stick X = zoom. |
| `VR/VrMenuPose.cs` | Remembers panel / HUD pose across scenes. |
| `VR/VrEmergencyInput.cs` | A / X emergency. |
| `VR/VRKeyboard.cs` | In-world keyboard (no Quest OS overlay). |
| `VR/VRGazePointer.cs` | Look-at ray for exposure metrics. |
| `UI/ClinicalUIFactory.cs` | Themed buttons and text. Prefers World Space in VR. |
| `UI/SessionVitalsOverlay.cs` | Live BPM / movement / stress graphs. |
| `UI/UiSparkline.cs` | Line graph widget. |
| `UI/UiAiMark.cs` | Visible AI badge. |
| `Theme/ThemePreset.cs` | Color tokens. |
| `Theme/UIThemeAutoApply.cs` | Paints UI on scene load. |

## Testing, editor, Polar script

| File | What it contains |
| --- | --- |
| `Testing/SrsLogicTests.cs` | Formula and login tests. No headset needed. |
| `Testing/EditorTherapySimulator.cs` | F1 skip. |
| `Assets/Editor/*SceneBuilder.cs` | Rebuild scene UI from code. |
| `tools/polar_hr_bridge.py` | Polar H10 to UDP 7878. |

Runtime images live in `Assets/Resources/`: `baseline_image`, `height_image`, `crowd_image`, `darkness_image` / `darkness_image2` / `darkness_image3`, `safe_room_preview`.

---

# 10. What you can change vs leave alone

**Safe to change (one constant, then recompile)**

- Timers in `SessionTiming.cs`
- Stage BPM gates in `HeartRateBleConstants.cs`
- Panic weights and thresholds in `PanicDetectionService.cs`
- `forceMockSensor` on `SensorManager` **only** if Polar is dead (leave **false** for the real demo)

**Do not change on demo day**

- Canvas World Space conversion
- `VRMenuDragger` grab logic
- Signup success path in `DatabaseManager`
- `AllowManualStageSelection` (must stay off so AI is visible)

---

# 11. Polar + VR demo checklist

1. Unity Editor Play. Quest on **Link (USB)**.
2. Same PC: `pip install bleak` (once), then `python tools\polar_hr_bridge.py`.
3. Wear H10. Wet electrodes. Terminal prints `BPM: ...`. Unity listens on **7878**.
4. Log in: `user@phobia.local` / `user1234`, or a real Supabase account.
5. Baseline. Wait, or press **F1**. Confirm AI stage text. Safe Room. Exposure.
6. Point the laser at a **menu**. **Grip** to drag. Stick Y to push / pull. Stick X to zoom. Graphs move separately from screens.
7. Press **A** or **X** for Safe Room.

Editor mock keys (only if mock is forced): Polar debug window, `+` / `-` BPM, **P** panic, **C** calm. **F8** hides the debug panel.

---

# 12. Where data is stored

| Store | Location |
| --- | --- |
| Local accounts | `persistentDataPath/PhobiaReliefTherapy_accounts.json` |
| Local sessions | `persistentDataPath/PhobiaReliefTherapy_sessions.json` |
| Cloud users | Supabase Auth |
| Cloud sessions | Supabase tables from `docs/supabase-admin-schema.sql` |
| Login tokens | PlayerPrefs |

`DatabaseManager` merges local + cloud so View Stats and Analyze Performance stay filled if a POST was delayed. Persist happens at exposure **complete or abort**, not only after feedback Save.

Signup can return HTTP 2xx with **no session** until the email is verified. That is still a **successful cloud signup**, not “cloud unavailable”.

---

# 13. Use cases vs what we ship

Treat “functional” as: the actor can complete the job in the app.

| Area | Status |
| --- | --- |
| Login / register / recovery | Done (cloud + local fallback) |
| Dashboard, phobia select | Done |
| Baseline 1 minute | Done |
| AI stage from vitals | Done (decision tree) |
| Safe Room + emergency | Done |
| 2 minute exposure, three phobias | Done |
| Panic / adaptive intensity | Done |
| Feedback + stats | Done |
| Admin users / suspend | Done |
| Lab appointments | Not implemented. Say out of scope. |

If they require 80% of listed use cases, appointments are the known miss. Do not claim Random Forest or ML training.

---

# 14. Likely questions

**“This is not AI.”**  
Classical AI includes expert systems and decision trees. The SRS specifies a weighted panic score and stage recommendation. We implemented that as an explicit tree. A black-box Random Forest would be harder to defend with our dataset size.

**“Show me the model file.”**  
There is none. The model **is** the code in `PanicDetectionService` and `ExposureStageAdvisor`. Every recommendation is explainable.

**“Why 360 photos, not 3D cities?”**  
One skybox draw call, stable 90 Hz on Quest, photographic realism, smaller APK. Intensity uses `_Exposure` and, for darkness, three textures.

**“Why World Space UI?”**  
Screen Space Overlay is invisible inside the headset. We convert to World Space (scale about 0.001) and use XRI `TrackedDeviceGraphicRaycaster`.

**“Why a custom keyboard?”**  
The Quest OS keyboard sits outside Unity’s stereo camera and glitches. Ours uses the same UI stack as the rest of the app.

**“Double height?”**  
Floor tracking already includes standing height. An origin at Y = 1.7 m stacked to about 3.4 m. Origin is Y = 0.

**“Why coroutines, not Thread.Sleep?”**  
Unity is single-threaded. Sleep would freeze the headset and cause nausea. Timers and HTTP use `yield`.

---

# 15. Honest limitations (say these first)

1. Appointments / clinic scheduling are not in the product.  
2. Live Polar requires the **PC bridge** and Quest Link, not a standalone APK.  
3. AI is **not** a trained Random Forest.  
4. Height and crowd use **one** panorama each; darkness has three images. Stage is also encoded in skybox exposure.  
5. Older write-ups still mention 20 s / 30 s. **Code is 60 / 120.** Trust this guide and `SessionTiming.cs`.

---

# 16. Unity editor menus

- **Tools > PRT > Run SRS Logic Tests** - panic bands, stage advisor, seed login, suspend.  
- **Tools > PRT > Apply Cloud Setup** - copy SQL, open Supabase editor.  
- Scene rebuilders under `Assets/Editor/` if a canvas is missing after a pull.

If tests fail on demo morning, do not invent new thresholds. The answers in this guide match the current constants.
