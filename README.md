# GIMM250 — Interactive Comic

An interactive comic system built in Unity, designed for the GIMM250 final project. The system supports a looping 
structure with player choices that affects the art in each panel and what text they contain. It also includes features 
for tooltips and custom cursors.

---

## Documentation References

| Resource | Version | URL                                                                                 |
|----------|---------|-------------------------------------------------------------------------------------|
| Unity 6 User Manual | 6000.3  | https://docs.unity3d.com/6000.3/Documentation/Manual/                               |
| Unity 6 Scripting API | 6000.3  | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/                      |
| URP Manual | 17.3.0  | https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.3/manual/ |
| URP Scripting API | 17.3.0  | https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.3/api/    |
| Cinemachine Manual | 3.1.6   | https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/                 |
| Cinemachine Scripting API | 3.1.6   | https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/api/                    |
| Input System Manual | 1.11.0  | https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/                |
| Input System Scripting API | 1.11.0  | https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/api/                   |

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
  ├── Step_02_Dialogue      ← AnimatedStep with PanelText child(ren) (second press)
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

**Optional text (dialogue / prose / captions):** Add one or more child GameObjects to the `AnimatedStep` and attach a `PanelText` component to each. The `AnimatedStep`'s Animator controls when each text element appears and disappears via keyframes. Fill in variant text fields on each `PanelText` to show different content based on player focus choices. Leave no `PanelText` children for purely visual steps.

**Persists In Final State:** Tick this on any `AnimatedStep` that should remain visible when the player browses history (UI arrows). Only steps that have already been triggered via the advance button will show — steps not yet reached are always hidden regardless of this setting.

**Revisit behaviour:** Use the two flags on `AnimatedStep` to control what happens when the player sees a panel again in a **later loop** (this does not affect the Replay button — that always replays all steps):

| `Replay On Revisit` | `Hide On Revisit` | What happens on loop revisit |
|---|---|---|
| ✅ | — | Full animation replays and blocks input, same as first visit |
| ☐ | ☐ | Snaps to final frame instantly; appears with no advance press needed |
| ☐ | ✅ | Stays hidden entirely; requires no advance press |

When a panel is revisited, all non-blocking steps (frozen or hidden) activate in a single burst on one advance press before the first blocking step or panel completion.

> **Save step children as inactive.** In Prefab Mode, deactivate each step child GameObject before saving. This prevents TMP text components from rendering in the Scene View during initial Editor load before fonts are ready (which produces harmless but noisy *"No Font Asset"* warnings). Adding `AnimatedStep` via the Inspector on a new GameObject does this automatically via `Reset()`.

> **Tip — automatic vs manual panel completion:**
> By default (`Require Advance To Complete` ticked on the `PanelDataSO`), the advance hint appears after the last step and the player presses spacebar to move on. Untick it for panels whose last step ends cinematically and should flow straight to the next panel with no extra press.

---

## Step Types

| Component | Blocks input? | Has text? | Use for |
|---|---|---|---|
| `AnimatedStep` | until `OnAnimationFinished` | Via `PanelText` children | Sprite reveals, panel effects, dialogue, prose |
| `PanelText` | — (not a step) | Yes — focus-variant | Dialogue lines, captions, prose; child of `AnimatedStep`, shown/hidden by its Animator |
| `MiniGame` subclass | until `Complete()` or `Fail()` | No | Interactive puzzles embedded in panels |

---

## Player Focus Choices

Choices are stored in the shared `PlayerChoices.asset` ScriptableObject. All panels read from the same asset, so a choice made in panel 4 is visible in panel 15 with no extra wiring.

To display focus-variant text on a panel, add a `PanelText` component to a child GameObject of the `AnimatedStep`. Use the `AnimatedStep`'s Animator to control when the text appears. Fill in the variant fields on `PanelText`:

| Field | When it shows |
|---|---|
| **Default Text** | No override matches, or no choice made yet |
| **Science Option A Text** | Player chose Science OptionA |
| **Science Option B Text** | Player chose Science OptionB |
| **Philosophy Option A Text** | Player chose Philosophy OptionA |
| **Philosophy Option B Text** | Player chose Philosophy OptionB |
| **Leadership Option A Text** | Player chose Leadership OptionA |
| **Leadership Option B Text** | Player chose Leadership OptionB |

`OptionA` / `OptionB` are placeholder names in the code — they will be renamed to the real thematic names once decided. Leave any field empty to fall through to `defaultText`.

Multiple `PanelText` children can exist under one `AnimatedStep`, each with independent text and independent show/hide timing in the Animator.

---

## Code Structure

```
ScriptableObjects          — designer-editable data
  PanelDataSO              — rank, firstLoop, lastLoop, blend, completion config per panel
  PlayerChoicesSO          — shared state for all three focuses

MonoBehaviours            
  ComicManager             — panel registry, loop pointer, history navigation
  ComicPanel               — Show/Hide/Advance, drives step sequence
  StepBase                 — abstract base for all IPanelStep implementations
  AnimatedStep             — blocking IPanelStep; populates PanelText children on activate
  PanelText                — focus-variant text (dialogue/prose/captions); child of AnimatedStep
  FrameMask                — syncs SpriteMask size to parent 9-sliced SpriteRenderer
  MiniGame (abstract)      — blocking IPanelStep base; subclass for each puzzle
  FocusPoint               — stub; not yet implemented (see AGENTS.md)
  FocusPointPresenter      — stub; mirrors NavigationPresenter for FocusPoint UI
  NavigationController     — input events → ComicManager
  NavigationPresenter      — UI arrows, replay button, advance hint
  TooltipTrigger           — fires static events on pointer enter/exit; attach to any hoverable
  TooltipDisplay           — subscribes to TooltipTrigger; positions + fades tooltip panel
  CursorTrigger            — changes OS cursor on hover; restores default on exit
  CursorManager            — sets default cursor on startup; exposes static ApplyDefault()
  TitleScreenController    — stub; title screen UI + scene transition
  EndScreenController      — stub; end screen UI + restart/quit

Editor-only
  PanelViewLock            — solo-locks a CinemachineCamera in Game View for editing

Plain C#                  
  PanelSelector            — picks next panel by rank + loop eligibility
  CommandHistory           — two-stack undo/redo (Execute/Undo/Redo)
  SwitchCameraCommand      — ICommand; Show/Hide panels, set Cinemachine blend
  LoopCountBounds          — static readonly Last; computed from LoopCount enum
```

### Key Rules
- **Camera switching** — enable/disable `CinemachineCamera` components only. Never change priority. One enabled = active; Cinemachine Brain blends automatically.
- **Player choices** — read `PlayerChoicesSO` directly via `[SerializeField]`. No class should reference `ComicManager` just to get choices.
- **Input** — subscribe to `InputAction.performed` in `OnEnable`, unsubscribe in `OnDisable`. Never poll in `Update`.
- **Loop bounds** — use `LoopCountBounds.Last` instead of a hardcoded `LoopCount.Loop3`. Add new values to `LoopCount` in `SharedEnums.cs` only.

---

## Tooltips

Add `TooltipTrigger` to any hoverable GameObject, fill in the **Message** field. No other wiring required.

`TooltipDisplay` lives on a persistent parent inside the Canvas. It subscribes to `TooltipTrigger`'s static events automatically. Required Inspector assignments on `TooltipDisplay`:

| Slot | What to assign |
|---|---|
| **Tooltip Panel** | The small child GameObject with the background + label |
| **Label** | The `TMP_Text` component inside that child |

> **Tooltip Panel setup:** anchor to centre **(0.5, 0.5)**, add a `CanvasGroup` component. Save as **inactive** in the scene.

---

## Custom Cursors

Add `CursorManager` to any persistent GameObject (e.g. the same one as `ComicManager`). Assign the **Default Cursor** texture — this is set at startup and restored whenever the pointer leaves a `CursorTrigger` element.

Add `CursorTrigger` to any hoverable GameObject alongside `TooltipTrigger`. Assign the **Cursor Texture** for that element.

> **Texture import settings:** Texture Type → **Cursor**, Read/Write enabled. On macOS the Editor requires `CursorMode.ForceSoftware` (already set in `CursorTrigger`) — hardware cursors are intercepted by the OS.

---

### Data Assets Location
```
Assets/Data/
  PanelData/           ← one PanelDataSO per panel (Panel_01_, Panel_02_…)
  PlayerChoices/       ← single shared PlayerChoices.asset used by all scenes
```