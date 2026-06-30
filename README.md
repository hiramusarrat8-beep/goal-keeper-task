# Goalkeeper Game

A Unity implementation of the "Goalkeeper Game" decision-making task used in fMRI
research. On each trial, the participant predicts which way a penalty shooter will
go, the shooter is revealed, and the participant can stay with their prediction or
switch before the outcome plays out. Built on [UXF](https://github.com/immersivecognition/unity-experiment-framework)
for trial/block sequencing and CSV data logging.

## Requirements

- Unity **2022.3.62f2** (see `ProjectSettings/ProjectVersion.txt`)

## Getting started

1. Open the project folder in Unity Hub.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press Play to run the task.

## Task flow

1. **Tutorial** — a short, untimed onboarding flow: task goal and controls, the
   four possible shooters (A/B/C/D, each shown in its own color), and a demo of the
   stay-or-switch mechanic. Advances on Spacebar.
2. **Practice block** — 10 untimed trials using a fixed, identical-for-everyone
   sequence, with full outcome/score feedback so participants learn the mechanic
   before the timer is introduced.
3. **Real task** — 4 blocks (160 trials by default) with the paper's timing:
   initial prediction → ISI → shooter reveal (stay/switch window) → ISI → outcome →
   ITI, with jittered ISI/ITI by default. A short reminder screen appears once
   before the first real trial, flagging that decisions are now timed.

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
