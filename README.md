# GIMM250 — Interactive Comic

Player focus choices (Science / Philosophy / Leadership) alter art and prose in specific panels. Navigation supports back/forward through history without replaying animations.

---

## Adding a Panel

### 1. Create the Data Asset
In the Project window: **right-click → Create → Comic → Panel Data**

Name it with a rank prefix so assets sort in comic order:
`Panel_01_TheSignal`, `Panel_04_DrawingLots`, etc.

| Field | What it does |
|---|---|
| **Rank** | Order within the loop. Lower = earlier. Use gaps (10, 20, 30…) so new panels can be inserted later. |
| **First Loop** | Which loop this panel first appears in. Loop0 = always shown from the start. |
| **Last Loop** | Which loop this panel last appears in. Defaults to Loop3 (always shown). Set lower to retire a panel after a specific loop. |
| **Require Advance To Complete** | If ticked (default), the player must press the advance button after the last step before moving to the next panel. Untick for cinematic panels that should advance automatically. |
| **Replay Animation On Revisit** | If ticked, the intro animation replays when this panel is seen again in a later loop. |
| **Incoming Blend** | How the camera moves to this panel. Default: EaseInOut 1s. Use Cut for instant jumps. |

### 2. Build the Scene Hierarchy

```
ComicPanel_XX_Name          ← ComicPanel component + Animator live HERE (root)
  ├── CinemachineCamera     ← one per panel; starts disabled automatically
  ├── Step_01_Reveal        ← AnimatedStep (first spacebar press)
  ├── Step_02_Dialogue      ← AnimatedStep with Label assigned (second press)
  ├── Step_03_SpriteIn      ← AnimatedStep, purely visual (third press)
  └── Step_04_Puzzle        ← MiniGame subclass (fourth press — blocking until complete)
```

Steps are activated in **hierarchy top-to-bottom order**. To reorder steps, drag GameObjects in the Hierarchy.

### 3. Wire the Inspector

On the **ComicPanel** root:
- **Data** → your new `PanelDataSO` asset
- **Cam** → the `CinemachineCamera` child
- **Choices** → the shared `PlayerChoices.asset` (in `Assets/Data/PlayerChoices/`)

> **Animator** is resolved automatically via `GetComponent` — no manual assignment needed. Unity will also prevent it from being accidentally removed (`[RequireComponent]`).

On the **ComicManager** GameObject: drag your new panel into the **Comic Panels** list.

### 4. Set Up the Intro Animation

1. Select the ComicPanel root and open the **Animation** window.
2. Create a clip named **`Intro`**; disable **Loop Time** in the import settings.
3. Animate whatever you need (sprite alpha, position, scale, etc.).
4. The code detects when `normalizedTime >= 1` automatically — no Animation Event required on the Intro clip.

### 5. Set Up AnimatedStep clips

Each `AnimatedStep` child needs its own Animator and clip:

1. Add an **Animator** component to the child GameObject.
2. Create an animation clip; set it as the Animator's default state; disable **Loop Time**.
3. On the **last keyframe**, add an **Animation Event** → Function: `OnAnimationFinished`
4. The step blocks input until that event fires, then the next spacebar press is accepted.

**Optional dialogue text:** Assign a `TMP_Text` component to the **Label** field. Fill in variant text fields to override the default text based on player focus choices.

**Persists In Final State:** Tick this on any `AnimatedStep` that should remain visible when the player browses history (UI arrows). Only steps that have already been triggered via the advance button will show — steps not yet reached are always hidden regardless of this setting.

> **Save step children as inactive.** In Prefab Mode, deactivate each step child GameObject before saving. This prevents TMP text components from rendering in the Scene View during initial Editor load before fonts are ready (which produces harmless but noisy *"No Font Asset"* warnings). Adding `AnimatedStep` via the Inspector on a new GameObject does this automatically via `Reset()`.

> **Tip — automatic vs manual panel completion:**
> By default (`Require Advance To Complete` ticked on the `PanelDataSO`), the advance hint appears after the last step and the player presses spacebar to move on. Untick it for panels whose last step ends cinematically and should flow straight to the next panel with no extra press.

---

## Step Types

| Component | Blocks input? | Has text? | Use for |
|---|---|---|---|
| `AnimatedStep` | until `OnAnimationFinished` | Optional | Dialogue bubbles, sprite reveals, panel effects |
| `MiniGame` subclass | until `Complete()` or `Fail()` | No | Interactive puzzles embedded in panels |

---

## Player Focus Choices

Choices are stored in the shared `PlayerChoices.asset` ScriptableObject. All panels read from the same asset, so a choice made in panel 4 is visible in panel 15 with no extra wiring.

On any `AnimatedStep` with a **Label** assigned, fill in the variant text fields to show different dialogue based on what the player chose:

THIS NEEDS TO CHANGE STILL TO HAVE TWO OPTIONS FOR EACH

| Field | When it shows |
|---|---|
| Science Text | Player chose a Science option |
| Philosophy Text | Player chose a Philosophy option |
| Leadership Text | Player chose a Leadership option |
| *(TMP_Text default)* | No override matches, or no choice made yet |

---

## Code Structure

```
ScriptableObjects          — designer-editable data
  PanelDataSO              — rank, firstLoop, lastLoop, blend, variant flags per panel
  PlayerChoicesSO          — shared state for all three focuses

MonoBehaviours            
  ComicManager             — panel registry, loop pointer, history navigation
  ComicPanel               — Show/Hide/Advance, drives step sequence
  AnimatedStep             — blocking IPanelStep; optional dialogue text
  FrameMask                — syncs SpriteMask size to parent 9-sliced SpriteRenderer
  MiniGame (abstract)      — blocking IPanelStep base; subclass for each puzzle
  FocusPoint               — stub; not yet implemented (see AGENTS.md)
  NavigationController     — input events → ComicManager
  NavigationPresenter      — UI arrows, replay button, advance hint

Plain C#                  
  PanelSelector            — picks next panel by rank + loop eligibility
  CommandHistory           — two-stack undo/redo (Execute/Undo/Redo)
  SwitchCameraCommand      — ICommand; Show/Hide panels, set Cinemachine blend
```

### Key Rules
- **Camera switching** — enable/disable `CinemachineCamera` components only. Never change priority. One enabled = active; Cinemachine Brain blends automatically.
- **Player choices** — read `PlayerChoicesSO` directly via `[SerializeField]`. No class should reference `ComicManager` just to get choices.
- **Input** — subscribe to `InputAction.performed` in `OnEnable`, unsubscribe in `OnDisable`. Never poll in `Update`.

### Data Assets Location
```
Assets/Data/
  PanelData/           ← one PanelDataSO per panel (Panel_01_, Panel_02_…)
  PlayerChoices/       ← single shared PlayerChoices.asset used by all scenes
```