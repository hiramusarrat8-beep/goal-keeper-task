# Goalkeeper Game

A Unity implementation of the "Goalkeeper Game" decision-making task used in fMRI/TUS
research. On each trial, the participant predicts which way a penalty shooter will
go, the shooter is revealed, and the participant can stay with their prediction or
switch before the outcome plays out. Built on [UXF](https://github.com/immersivecognition/unity-experiment-framework)
for trial/block sequencing and CSV data logging.

This repository implements the **Goal Keeper Task** described in the original publication:
*https://www.biorxiv.org/content/10.1101/2025.08.19.664920v1*

The task uses a **Markov chain** to generate trial sequences, meaning that the probability of the next outcome depends on the current state rather than being completely random. This creates learnable patterns that participants must discover and adapt to during the experiment.

## Screenshots
<img width="1918" height="1020" alt="image" src="https://github.com/user-attachments/assets/edcbda59-9af4-4d85-a753-a5e45e1739a1" />

<img width="1918" height="971" alt="image" src="https://github.com/user-attachments/assets/0d5451c8-1fd8-4de8-98fc-8475e5135757" />



## Requirements

- Unity **2022.3.62f2** (see `ProjectSettings/ProjectVersion.txt`)

## Getting started

Setup Instructions

1. Clone the repository and open the project in **Unity Hub**.

2. Drag the **UXF Rig** prefab (`UXF/Prefabs/UXF_Rig`) into the **Hierarchy**.

3. From the Unity toolbar, select **GoalKeeper → Build Scene**, then **GoalKeeper → Wire References**.

4. Switch to the **Game** view, press **Play** once, then stop the session. Return to the **Scene** view.

5. In the **Hierarchy**, select **UXF Rig**. In the **Inspector**, scroll down to **Events**. Under **On Session Begin**:
   - Click the **+** button.
   - Drag the **Game Manager** object from the Hierarchy into the **None (Object)** field.
   - From the **No Function** dropdown, select:
     - **GoalkeeperSessionGenerator → GenerateExperiment()**

6. Under **On Trial Begin**:
   - Click the **+** button.
   - Drag the **Game Manager** object into the **None (Object)** field.
   - From the **No Function** dropdown, select:
     - **GoalkeeperTrialRunner → OnTrialBegin()**

7. Save the scene (**Ctrl + S**).

8. Return to the **Game** view and press **Play** to start the experiment.
   

## Task flow

1. **Tutorial** — a short, untimed onboarding flow: task goal and controls, the
   four possible shooters (A/B/C/D, each shown in its own color), and a demo of the
   stay-or-switch mechanic. Advances on Spacebar.
2. **Practice block** — 10 untimed trials using a fixed, identical-for-everyone
   sequence, with full outcome/score feedback so participants learn the mechanic
   before the timer is introduced.
3. **Real task** — 4 blocks (160 trials)
## Trial Timeline

| Phase | Duration | Description |
|-------|:--------:|------------|
| Initial prediction window | **3.0 s** | Participant predicts the direction of the upcoming shot. |
| ISI 1 (before shooter reveal) | **1.0 s** | Inter-stimulus interval before the shooter is revealed. |
| Shooter reveal / Stay-or-Switch window | **1.5 s** | Shooter identity is displayed, allowing participants to maintain or change their prediction. |
| ISI 2 (before outcome) | **1.0 s** | Inter-stimulus interval before the outcome is presented. |
| Outcome display | **2.0 s** | The shot outcome is revealed and feedback is shown. |
| ITI (Inter-Trial Interval) | **2.0 s** | Blank interval before the next trial begins. |


Practice trials are tagged `is_practice` in the results CSV so they can be filtered
out of analysis — UXF logs every trial it runs, including practice ones.

## Project structure

- `Assets/Goalkeeper/` — game-specific scripts:
  - `GoalkeeperTrialRunner.cs` — per-trial coroutine/state machine, scoring.
  - `GoalkeeperView.cs` — all visual/HUD logic (shooter, ball, keeper, timer bar,
    tutorial/practice UI).
  - `GoalkeeperSessionGenerator.cs` — builds the UXF block/trial schedule
    (practice block + the 4 real blocks).
  - `GoalkeeperStructure.cs` — pure trial-generation logic (no Unity dependency):
    the paired-transition schedule and the fixed practice sequence.
  - `GoalkeeperTutorialRunner.cs` — the pre-task onboarding flow.
  - `Editor/GoalkeeperSceneBuilder.cs` — **rebuilds the entire scene from scratch**
    via the **`Goalkeeper → Build Scene`** menu item. Editing this file (or any
    script it wires up) has no effect on the live scene until you re-run this menu
    item.
  - `Editor/GoalkeeperWirer.cs` — `Goalkeeper → Wire References` menu item;
    re-finds existing named objects and reassigns references without rebuilding
    geometry/UI.
- `Assets/UXF/` — the UXF experiment framework (trial/block/session management,
  CSV output).
- `Assets/settings.json` — paper-accurate timing (jittered ISI/ITI via `_min`/`_max`
  keys).
- `Assets/settings_behavioural.json` — fixed/flat timing variant for fast piloting.

## Modifying the task

Any change to `Editor/GoalkeeperSceneBuilder.cs` (or a script it wires into the
scene) requires re-running **`Goalkeeper → Build Scene`** in the Unity Editor —
it destroys and recreates the relevant GameObjects from the current scripts every
time, so the live scene only reflects your latest code after that menu item runs.

## Data output

UXF writes one CSV per session to `Application.persistentDataPath` (outside this
repo, not included in version control).
