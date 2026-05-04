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

> **No ComicManager wiring needed.** `ComicManager` discovers all `ComicPanel` components in the scene automatically at startup — just having your panel in the scene hierarchy is enough.

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

## FocusPoint Setup

A `FocusPoint` step blocks the advance button until the player clicks one of two option objects.

### Scene hierarchy

```
ComicPanel_XX_Name
  └── Step_FocusPoint          ← FocusPoint component (StepBase); starts inactive
        ├── OptionA             ← any 3D GameObject; Collider + Clickable components
        ├── OptionB             ← any 3D GameObject; Collider + Clickable components
        └── PostProcessVariant  ← optional; swaps a global Volume profile at decision time
```

### Inspector wiring

On the **FocusPoint** component:

| Field | What to assign |
|---|---|
| **Category** | Science / Philosophy / Leadership — which choice axis this step records |
| **Choices** | The shared `PlayerChoices.asset` |
| **Option A** | The `Clickable` component on the Option A object |
| **Option B** | The `Clickable` component on the Option B object |

On each **Clickable** component's `onClick` UnityEvent:

| Option | Listener | Argument |
|---|---|---|
| Option A's `Clickable` | `FocusPoint → RecordChoice` | ☑ **(true)** |
| Option B's `Clickable` | `FocusPoint → RecordChoice` | ☐ **(false)** |

### Scene requirements

A **`ClickManager`** component must be present somewhere in the scene (e.g. on the same GameObject as `ComicManager`). It routes `Physics.Raycast` hits to `Clickable.Click()`.

> **3D Colliders required.** `ClickManager` uses `Physics.Raycast` — option objects must have a 3D `Collider` component. UI and 2D sprite objects are not supported.

### Global Volume change (optional)

To change the comic's post-process look at the moment the player decides — and keep that look for all subsequent panels — fill in the **Global Volume Change** fields on the `FocusPoint` component:

| Field | What to assign |
|---|---|
| **Global Volume** | The scene `Volume` component to update (typically a global URP volume) |
| **Option A Profile** | `VolumeProfile` applied when the player picks Option A |
| **Option B Profile** | `VolumeProfile` applied when the player picks Option B |

The profile is swapped immediately when `RecordChoice()` fires and re-applied on revisit so history browsing reflects the player's choice. Leave **Global Volume** empty for panels with no post-process change.

### Revisit behaviour

On loop revisit or after the Replay button, `FocusPoint` shows the already-chosen state rather than re-presenting the choice. The chosen option's `Clickable` stays enabled; the unchosen option is disabled. Any `IVariantContent` children (e.g. `PostProcessVariant`) are re-populated on revisit so persistent effects are restored correctly when browsing history. `PrepareForReplay()` is a no-op — choices persist.

### Persistent global Volume change

To change the comic's post-process look at the moment the player makes a focus decision — and keep that look for all subsequent panels — add a `PostProcessVariant` child to the `FocusPoint` step and assign the global scene `Volume`. The choice is written to `PlayerChoicesSO` first, then `PostProcessVariant.Populate()` is called immediately, swapping the profile. Because the profile change is applied directly to the `Volume` component it persists until something else changes it.

---

## Step Types

| Component | Blocks input? | Has text? | Use for |
|---|---|---|---|
| `AnimatedStep` | until `OnAnimationFinished` | Via `PanelText` / `SpriteVariant` children | Sprite reveals, panel effects, dialogue, prose |
| `PanelText` | — (not a step) | Yes — focus-variant | Dialogue lines, captions, prose; child of `AnimatedStep`, shown/hidden by its Animator |
| `SpriteVariant` | — (not a step) | No — swaps sprites | Attach to any `SpriteRenderer` child of an `AnimatedStep`; swaps sprite based on player focus choice |
| `FocusPoint` | until option clicked | No | Presents two `Clickable` options; records choice to `PlayerChoicesSO`; optionally swaps a global `VolumeProfile` at decision time |
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
  ComicManager             — discovers panels automatically; loop pointer, history navigation
  ComicPanel               — Show/Hide/Advance, drives step sequence
  StepBase                 — abstract base for all IPanelStep implementations
  AnimatedStep             — blocking IPanelStep; populates IVariantContent children on activate
  PanelText                — focus-variant text (dialogue/prose/captions); implements IVariantContent; child of AnimatedStep
  SpriteVariant            — focus-variant sprite swap; implements IVariantContent; child of AnimatedStep
  FocusPoint               — blocking IPanelStep; holds two Clickable refs; RecordChoice() wired via Inspector UnityEvent; writes choice to PlayerChoicesSO; optionally swaps a global VolumeProfile at decision time
  Clickable                — click target; exposes UnityEvent onClick (wired in Inspector); invoked by ClickManager via Physics.Raycast; requires 3D Collider
  ClickManager             — routes Physics.Raycast hits to Clickable.Click(); must be present in any scene using FocusPoint or mini-games
  FrameMask                — syncs SpriteMask size to parent 9-sliced SpriteRenderer
  MiniGame (abstract)      — blocking IPanelStep base; subclass for each puzzle
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
  IVariantContent          — interface for focus-driven child content; Populate(PlayerChoicesSO); implemented by PanelText and SpriteVariant
  PanelSelector            — picks next panel by rank + loop eligibility
  CommandHistory           — two-stack undo/redo (Execute/Undo/Redo)
  SwitchCameraCommand      — ICommand; Show/Hide panels, set Cinemachine blend
  LoopCountBounds          — static readonly Last; computed from LoopCount enum
```

### Key Rules
- **Camera switching** — enable/disable `CinemachineCamera` components only. Never change priority. One enabled = active; Cinemachine Brain blends automatically.
- **Panel discovery** — `ComicManager` finds panels automatically via `FindObjectsByType`. Never add a `[SerializeField]` panel list to `ComicManager` — multiple teammates modifying that list causes null slots after merges.
- **Player choices** — read `PlayerChoicesSO` directly via `[SerializeField]`. No class should reference `ComicManager` just to get choices.
- **Input** — subscribe to `InputAction.performed` in `OnEnable`, unsubscribe in `OnDisable`. Never poll in `Update`. Exception: `ClickManager` uses `Physics.Raycast` in `Update()` for 3D click targets.
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