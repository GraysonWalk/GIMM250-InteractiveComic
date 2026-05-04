# AGENTS.md — GIMM250 Interactive Comic

## Project Overview
Unity URP project (Unity 6, URP 17.x). An interactive 3D comic where a Cinemachine brain moves between panel-specific cameras as the player advances. Player focus choices (Science / Philosophy / Leadership) alter art and prose in specific panels. Navigation supports back/forward without replaying animations.

**Source of truth for planned architecture:** `Assets/ProjectPlanning/CodeArchitecture`
**Reference patterns document:** `Assets/ProjectPlanning/UnityProgrammingPatterns.txt` (local PDF extract — SOLID, Command, Observer, State, MVP patterns with Unity examples)

---

## Architecture at a Glance

```
ScriptableObjects (data, designer-editable)
  PanelDataSO      ← rank, firstLoop, lastLoop, blend, completion config per panel
  PlayerChoicesSO  ← ScienceFocus / PhilosophyFocus / LeadershipFocus (runtime shared state)

MonoBehaviours (runtime behaviour)
  ComicManager        ← panel list + loop pointer; calls PanelSelector + CommandHistory
  ComicPanel          ← Show()/Hide(), reads PlayerChoicesSO at Show() time
  StepBase            ← abstract base for all IPanelStep implementations
  AnimatedStep        ← blocking IPanelStep; populates IVariantContent children on activate
  PanelText           ← focus-variant text display; implements IVariantContent; child of AnimatedStep
  SpriteVariant       ← focus-variant sprite swap; implements IVariantContent; child of AnimatedStep
  MiniGame            ← abstract blocking IPanelStep base — subclass for each puzzle
  FocusPoint          ← blocking IPanelStep; holds Clickable refs, writes to PlayerChoicesSO; RecordChoice() called via Inspector UnityEvent
  NavigationController← Input events → ComicManager.AdvancePanel() / RetreatPanel()
  NavigationPresenter ← UI display only (arrows, replay button, advance hint)
  FrameMask           ← syncs SpriteMask size to parent 9-sliced SpriteRenderer
  TooltipTrigger      ← fires static OnShow/OnHide events on pointer enter/exit
  TooltipDisplay      ← subscribes to TooltipTrigger events; positions + fades tooltip panel
  CursorTrigger       ← changes OS cursor on hover; restores via CursorManager on exit
  CursorManager       ← sets default cursor on startup; exposes static ApplyDefault()
  TitleScreenController ← title screen UI + start button; calls ComicManager.StartComic() on click
  EndScreenController   ← stub — end screen UI + restart/quit

Editor-only
  PanelViewLock       ← solo-locks a CinemachineCamera in Game View for editing; stripped from builds

Plain C# (no MonoBehaviour)
  IVariantContent     ← interface implemented by PanelText, SpriteVariant; Populate(PlayerChoicesSO)
  PanelSelector       ← static NextPanel(list, current, loop) — pure function
  CommandHistory      ← two-stack undo/redo (Stack<ICommand> _history + _future)
  SwitchCameraCommand ← Execute: prev.Hide()+target.Show(); Undo: reverse
  LoopCountBounds     ← static readonly Last — computed from LoopCount enum; single change point
```

**Key decoupling rule:** No class should reference `ComicManager` just to read player choices — use `PlayerChoicesSO` directly via `[SerializeField]`.

---

## Camera Pattern
Cinemachine cameras are switched by **enabling/disabling** `CinemachineCamera` components — never by changing priorities. One camera enabled = active; all others disabled. `CinemachineBrain` on the main camera handles blending automatically. `NavigationController` and `SwitchCameraCommand` implement this pattern.

---

## Enums & Shared Types
All shared enums live in **`Assets/Code/Scripts/SharedEnums.cs`** — do not redefine them elsewhere.
- `LoopCount { Loop0–Loop3 }` — panel ordering across loops
- `ScienceChoice { None, OptionA, OptionB }` — the player's selected Science focus
- `PhilosophyChoice { None, OptionA, OptionB }` — the player's selected Philosophy focus
- `LeadershipChoice { None, OptionA, OptionB }` — the player's selected Leadership focus

`OptionA` and `OptionB` are placeholder names. When the real thematic names are decided, rename them in `SharedEnums.cs` only — all consumers update automatically. Do not rename them in individual class files.

---

## Design Principles

All code in this project should adhere to the following principles. When proposing or generating a solution, favour the simplest approach that satisfies the requirement without over-engineering.

### SOLID
- **Single Responsibility** — each class does one thing. `PanelSelector` selects panels; `ComicPanel` manages panel state; `ComicManager` orchestrates flow. Do not merge these concerns.
- **Open/Closed** — extend behaviour through data (`PanelDataSO` fields, new `IPanelStep` implementations) rather than modifying existing classes.
- **Liskov Substitution** — any `IPanelStep` implementation (`AnimatedStep`, `MiniGame`, future types) must be usable interchangeably by `ComicPanel` without special-casing.
- **Interface Segregation** — keep interfaces narrow. `IComicPanel` and `IPanelStep` expose only what their consumers need; do not bloat them with convenience methods.
- **Dependency Inversion** — `ComicManager` depends on `IComicPanel`, not `ComicPanel`. `ComicPanel` depends on `IPanelStep`, not `AnimatedStep`. Consumers program to interfaces, not concrete types.

### DRY (Don't Repeat Yourself)
- Panel eligibility logic lives solely in `PanelSelector` — do not duplicate loop checks elsewhere.
- Shared enums live solely in `SharedEnums.cs` — do not redefine them in other files.
- If the same condition or calculation appears in two places, extract it.

### KISS (Keep It Simple, Stupid)
- Prefer the simplest data structure or algorithm that correctly solves the problem.
- Avoid clever one-liners that obscure intent. Clarity beats brevity.
- A new field on an existing SO is simpler than a new class — use it when it suffices.

### YAGNI (You Aren't Gonna Need It)
- Do not add fields, methods, classes, or abstractions for hypothetical future requirements.
- If a feature isn't needed right now, don't build it. Extend only when the requirement is real and present.
- Example: adding `lastLoop` to `PanelDataSO` was justified by an immediate need; adding a full loop-range collection would have been YAGNI.

---

## Conventions
- `[SerializeField]` only on MonoBehaviours and ScriptableObjects — not plain C# classes (`PanelSelector` is a plain class; inject dependencies via constructor).
- For components that are **always co-located on the same GameObject**, use `[RequireComponent(typeof(T))]` on the class and resolve via `GetComponent<T>()` in `Awake()` — do not use `[SerializeField]`. Example: `ComicPanel` and its `Animator`. For components on **different objects**, use `[SerializeField]` and assign in the Inspector. Example: `ComicPanel` and its `CinemachineCamera` child.
- Use `#region Variables / #region Constructor / #region Methods` blocks for consistency.
- `readonly` + `private` for fields that are set once and never reassigned; `static readonly` for cached hashes.
- Input is event-driven: subscribe to `InputAction.performed` in `OnEnable`, unsubscribe in `OnDisable`. Do not poll in `Update`.

---

## ScriptableObject Asset Organisation

All SO assets live under `Assets/Data/`, separated by type:

```
Assets/Data/
  PanelData/          ← one PanelDataSO per panel
  │   Panel_01_TheSignal.asset      (zero-padded rank prefix so they sort in comic order)
  │   Panel_02_RiggedDrawA.asset
  │   ...
  PlayerChoices/      ← single shared PlayerChoicesSO runtime asset
      PlayerChoices.asset
```

- `PanelDataSO` assets must be in `Data/PanelData/`, not inside panel art folders. Keeping them together makes it easy to overview all panel configuration without opening individual scene hierarchies.
- Name `PanelDataSO` assets with a zero-padded rank prefix (`Panel_01_`, `Panel_02_`) so the Project window sort order matches comic order without opening each asset.
- There is only ever **one** `PlayerChoices.asset` — all scenes share the same asset via `[SerializeField]` references.

## Panel Asset Organisation

Each panel's art and animation lives under `Assets/Panels/Panel_XX_Name/`:

```
Assets/Panels/
  Panel_01_TheSignal/
    Art/              ← sprites, textures specific to this panel
    Animation/
      Clips/          ← .anim files
      Controllers/    ← .controller files
    Panel_01_TheSignal Variant.prefab   ← prefab variant of the base ComicPanel prefab
  Panel_02_RiggedDrawA/
    ...
  _Test/              ← test and prototype panels (underscore keeps them sorted to top)
    Panel_01A_Test1/
    ...
```

- `.asset` (PanelDataSO) files are **not** stored here — they live in `Assets/Data/PanelData/`.
- Test and prototype panels live in `Panels/_Test/` to distinguish them from production content.
- The base `ComicPanel.prefab` lives in `Assets/Prefabs/` — panel variants inherit from it.

## Shared Asset Organisation

```
Assets/Shared/
  Art/
    Sprites/          ← art shared across panels (UI excluded)
  Animation/
    UI/               ← shared UI animator controllers (buttons, hints)
  Sound/              ← shared audio
```

---

## Key Files
| File | Role |
|------|------|
| `Assets/Code/Scripts/ComicPanel/ComicPanel.cs` | Panel state machine — Show/Hide/Advance, drives step sequence |
| `Assets/Code/Scripts/ComicPanel/AnimatedStep.cs` | Blocking `IPanelStep`; plays animation clip, populates `PanelText` children |
| `Assets/Code/Scripts/ComicPanel/PanelText.cs` | Focus-variant text display (dialogue, prose, captions) — child of `AnimatedStep`; visibility driven by Animator |
| `Assets/Code/Scripts/MiniGame/MiniGame.cs` | Abstract blocking `IPanelStep` base — subclass for each puzzle |
| `Assets/Code/Scripts/Management/ComicManager.cs` | Panel registry, loop pointer, history navigation orchestration |
| `Assets/Code/Scripts/Management/PanelSelector.cs` | Pure static panel eligibility + selection logic |
| `Assets/Code/Scripts/Management/CommandHistory.cs` | Two-stack undo/redo (Execute / Undo / Redo) |
| `Assets/Code/Scripts/Commands/SwitchCameraCommand.cs` | `ICommand` — Show/Hide panels, set Cinemachine blend |
| `Assets/Code/Scripts/Navigation/NavigationController.cs` | Input events → ComicManager; blocks advance during animations |
| `Assets/Code/Scripts/Navigation/NavigationPresenter.cs` | UI arrows, replay button, advance hint display |
| `Assets/Code/Scripts/ComicPanel/FrameMask.cs` | Syncs `SpriteMask` size to parent 9-sliced `SpriteRenderer`; use on a child of the Frame object |
| `Assets/Code/Scripts/ComicPanel/StepBase.cs` | Abstract base for all `IPanelStep` implementations — owns activation state, `IsBlocking`, revisit-behaviour flags (`replayOnRevisit`, `hideOnRevisit`), `OnStepComplete`, `Deactivate()` (virtual), `PrepareForReplay()` (virtual) |
| `Assets/Code/Scripts/VariantContent/IVariantContent.cs` | Interface for focus-choice-driven child content — `Populate(PlayerChoicesSO)`; implemented by `PanelText` and `SpriteVariant`; discovered by `AnimatedStep` via `GetComponentsInChildren` |
| `Assets/Code/Scripts/VariantContent/SpriteVariant.cs` | Focus-variant sprite swap; implements `IVariantContent`; attach to any `SpriteRenderer` child of an `AnimatedStep` |
| `Assets/Code/Scripts/Steps/FocusPoint.cs` | Blocking `IPanelStep`; holds two `Clickable` refs, writes result to `PlayerChoicesSO`; `RecordChoice(bool)` is called via `Clickable.onClick` UnityEvent wired in the Inspector; optionally applies a `VolumeProfile` to a global scene `Volume` at decision time; `PrepareForReplay()` is a no-op — choice persists across replays |
| `Assets/Code/Scripts/MiniGame/Clickable.cs` | Click target component; exposes a `UnityEvent onClick` wired in the Inspector and a public `Click()` method invoked by `ClickManager`; attach to any 3D GameObject with a Collider |
| `Assets/Code/Scripts/MiniGame/ClickManager.cs` | Routes `Physics.Raycast` hits to `Clickable.Click()` — must be present in any scene that uses `FocusPoint` or mini-games; 3D objects only |
| `Assets/Code/Scripts/Tooltip/TooltipTrigger.cs` | Fires static `OnShow`/`OnHide` events on pointer enter/exit |
| `Assets/Code/Scripts/Tooltip/TooltipDisplay.cs` | Subscribes to `TooltipTrigger` events; positions + fades tooltip panel |
| `Assets/Code/Scripts/Tooltip/CursorTrigger.cs` | Changes OS cursor on hover; restores via `CursorManager` on exit |
| `Assets/Code/Scripts/Tooltip/CursorManager.cs` | Sets default cursor on startup; exposes static `ApplyDefault()` |
| `Assets/Code/Scripts/Editor Tools/PanelViewLock.cs` | Editor-only — solo-locks a `CinemachineCamera` in Game View for panel editing; stripped from builds |
| `Assets/Code/Scripts/Management/TitleScreenController.cs` | Title screen — hides title UI, disables title camera, calls `ComicManager.StartComic()` on button press |
| `Assets/Code/Scripts/Management/EndScreenController.cs` | Stub — end screen UI + restart/quit |
| `Assets/ProjectPlanning/CodeArchitecture` | Full architecture reference including ScriptableObjects, CommandHistory, interfaces |

---

## IPanelStep Contract

`IPanelStep` exposes exactly two booleans — keep them distinct and do not conflate them:

| Property | Asked by | Meaning |
|----------|----------|---------|
| `IsBlocking` | `ComicPanel.Advance()` | Lock advance input after `Activate()` until `OnStepComplete` fires? |
| `ShowInFinalState` | `ComicPanel.ShowInstant()` | Should this step be visible when the panel is shown in its history/final state? |

**`ShowInFinalState` must combine designer intent with runtime state.** Example from `AnimatedStep`:
```csharp
public bool ShowInFinalState => persistsInFinalState && _hasBeenActivated;
```
The `&& _hasBeenActivated` guard is not optional — calling `Activate()` on a step that has never been through `Advance()` plays its animation outside the blocking system, permanently locking the advance button. Every `IPanelStep` implementation must enforce this guard in `ShowInFinalState`.

---

## Panel Loop Eligibility

A panel is eligible when `firstLoop <= currentLoop <= lastLoop`.
- `firstLoop` defaults to `Loop0` — appears from the start.
- `lastLoop` defaults to `Loop3` — never retired; existing assets need no change.
- Panel eligibility logic lives solely in `PanelSelector` — do not duplicate these checks elsewhere.

To retire a panel after a specific loop, set `lastLoop` on its `PanelDataSO` asset. Do not add any other mechanism.

---

## Established Decisions (Do Not Revert)

These patterns were deliberately chosen to fix specific bugs or enforce invariants. Do not change them without understanding the reason documented here.

**`ComicManager` discovers panels via `FindObjectsByType`, not a `[SerializeField]` list.**
A manually-wired `List<ComicPanel>` in the Inspector modifies `Main.unity` every time a teammate adds a panel. When two branches each add panels and are merged, git cannot reconcile the conflicting lists and writes `{fileID: 0}` null slots, causing null reference exceptions at startup. `FindObjectsByType<ComicPanel>` at `Start()` discovers every panel already in the scene hierarchy with no Inspector wiring — eliminating the merge conflict source entirely. Do not re-add a `[SerializeField]` panel list to `ComicManager`.

**`ComicManager.Start()` only discovers panels; `StartComic()` begins play.**
`Start()` runs `FindObjectsByType` so panels are ready, but does not show any panel. `TitleScreenController` calls `StartComic()` when the player presses the start button. This means the comic never starts automatically — it always waits for an explicit signal. If testing without a title screen, call `StartComic()` from any other `Start()` or wire it temporarily to a button.

**`SwitchToPanel` always uses `next.IncomingBlend`; there is no forced Cut for the first panel.**
The original code forced a Cut when `prev == null` to prevent the camera travelling from world origin to Panel 1. With a title camera in the scene, `prev == null` just means the title camera is the blend origin — Cinemachine blends from it correctly. Panel 1's `IncomingBlend` on its `PanelDataSO` controls the feel (EaseInOut 2s for a tilt-down, Cut for instant). Set Panel 1's blend to Cut if testing without a title camera.

**`HasBeenVisited` is set in `WaitForIntroCompletion()`, not in `Show()`.**
If set at the start of `Show()`, navigating away mid-animation would leave `HasBeenVisited = true`, causing the panel to skip to its end state on the next forward navigation instead of replaying the animation. It must only be set after the animation fully completes.

**`SwitchCameraCommand.Redo()` calls `ShowInstant()`, not `Show()`.**
History navigation (UI arrows) always shows the panel in its final/end state — no animation, no blocking. `Show()` is reserved for story-forward progression only.

**`ShowInstant()` uses `step.ShowInFinalState`, not just `step.PersistsInFinalState`.**
`ShowInFinalState` gates on `_hasBeenActivated` internally. Skipping that check would activate steps that were never triggered via `Advance()`, playing their animations without registering an `UnblockPanel` listener — permanently locking the advance button.

**`AnimatedStep` child GameObjects must be saved as inactive in the prefab.**
`AnimatedStep.Deactivate()` calls `SetActive(false)`, but `Awake()` doesn't run in Edit Mode. If step children (including `PanelText` GameObjects) are saved active, `TextMeshPro` (3D) components on them will attempt to render in the Scene View during initial asset loading before the font asset is ready, producing spurious *"Can't Generate Mesh, No Font Asset has been assigned"* warnings. `AnimatedStep.Reset()` and `PanelText.Reset()` both start new components inactive automatically; when creating step children manually, deactivate them in Prefab Mode before saving.

**`AnimatedStep.Activate()` has three explicit branches; the Animator seek is extracted into `SeekAnimator()`.**
Unity resets an `Animator` to its default state (time 0) whenever the GameObject is re-enabled (`keepAnimatorStateOnDisable = false` by default). `BeginActivation()` returns `skip = true` on a revisit, giving three paths:
1. `!skip` → first visit or replay: `SetActive(true)`, `SeekAnimator(0f)`, let the clip play.
2. `skip && !hideOnRevisit` → frozen state: `SetActive(true)`, `SeekAnimator(1f)` (end frame, no replay).
3. `skip && hideOnRevisit` → stays deactivated; `IsBlocking = false`, `Advance()` auto-chains.

`SeekAnimator(normalizedTime)` sequence: `Update(0f)` (resolves Entry → default-state transition), `GetCurrentAnimatorStateInfo(0).fullPathHash` (hash read at runtime — designers name states freely so no static hash can be cached), `Play(hash, 0, normalizedTime)`, `Update(0f)` (evaluates at target time). Do not inline this sequence — it must not be duplicated between branches.

**`_isBlocked` starts `true` in `Show()`, not `false`.**
Input must be blocked from the moment a panel begins showing until the intro animation fully completes and `OnReadyForInput` fires. If `_isBlocked` were `false` at the start of `Show()`, the player could press advance during the intro animation and trigger the first step before the panel has finished entering. `WaitForIntroCompletion()` and `FireReadyForInputNextFrame()` both set `_isBlocked = false` immediately before invoking `OnReadyForInput`.

**`ComicPanel.Advance()` uses a loop to auto-chain non-blocking steps.**
When a step's `IsBlocking` is `false` and it is not the last step, `Advance()` immediately activates the next step without waiting for another button press. This means all frozen/hidden revisit steps appear in a single burst on one advance press, rather than requiring the player to press advance once per step. The loop terminates when it hits a blocking step (waits for animation), the last step (evaluates `RequireAdvanceToComplete`), or `OnPanelComplete`. `UnblockPanel()` still fires `OnReadyForInput` after a blocking step completes — the next call to `Advance()` will then chain through any subsequent non-blocking steps.

**`IsBlocking` must be read before calling `Activate()`, not after.**
`IsBlocking` in `StepBase` reads `_hasBeenActivated` live: `!_hasBeenActivated || replayOnRevisit`. `Activate()` calls `BeginActivation()` which sets `_hasBeenActivated = true`. If `IsBlocking` is read after `Activate()`, any step with `replayOnRevisit = false` will incorrectly appear non-blocking on its first visit, causing the auto-chain loop to fire the next step immediately and play two animations simultaneously. `Advance()` captures `bool blocking = step.IsBlocking` before calling `step.Activate(choices)` to preserve the pre-activation intent.

**Step revisit behaviour is controlled by two flags in `StepBase`, not one.**
`replayOnRevisit` and `hideOnRevisit` control **loop-revisit** behaviour only (i.e. `Show()` called on a panel the player has already completed). They have no effect when the player presses the Replay button.
- `replayOnRevisit = true` → full replay on loop revisit; blocks input (identical to first visit)
- `replayOnRevisit = false`, `hideOnRevisit = false` → show in frozen/final state on loop revisit; auto-chained, no advance press
- `replayOnRevisit = false`, `hideOnRevisit = true` → invisible on loop revisit; auto-chained, no advance press

**`PrepareForReplay()` always resets `_hasBeenActivated`; override to preserve state.**
`ComicPanel.Replay()` (the Replay button) calls `PrepareForReplay()` on every step before restarting, unconditionally resetting `_hasBeenActivated = false`. This means every step animates and blocks again regardless of `replayOnRevisit` / `hideOnRevisit`. Steps that should preserve their state across explicit replays (e.g. `FocusPoint` showing the previously chosen option rather than re-presenting the choice) override `PrepareForReplay()` in their concrete class and leave `_hasBeenActivated` untouched.

Do not collapse `replayOnRevisit` / `hideOnRevisit` into a single enum — the two bools serialise cleanly, map clearly to Inspector checkboxes, and avoid the serialisation migration that renaming enum values would require.

---

## Stub / Incomplete Classes

These classes exist but are not yet fully implemented. Do not implement them ad-hoc — agree on the design first.

| Class | File | Status |
|-------|------|--------|
| `SignalGame` | `Assets/Code/Scripts/MiniGame/SignalGame.cs` | Stub — extends `MiniGame`; `StartGame()` not yet implemented |
| `EndScreenController` | `Assets/Code/Scripts/Management/EndScreenController.cs` | Stub — end screen UI; needs `ComicManager.OnComicComplete` event to subscribe to |
| `TooltipEvents` | Does not exist yet | Planned — when a second tooltip trigger source appears (e.g. world-space hotspot that isn't a `MonoBehaviour`), extract the two static events out of `TooltipTrigger` into a dedicated `TooltipEvents` static class. `TooltipTrigger` fires into it; `TooltipDisplay` subscribes to it. Neither class will need to know about the other. Do not implement until the second source is real. |

---

## Testing Checklist

When making changes to panel navigation, `ComicPanel`, `IPanelStep` implementations, or `SwitchCameraCommand`, manually verify these scenarios:

1. **First visit** — advance through all steps; panel completes and moves to next.
2. **Navigate back mid-animation** — go backward before the intro animation finishes; go forward again — the animation should replay from the beginning, not skip to end state.
3. **Navigate back mid-steps** — advance through some steps, go backward, go forward — persistent completed steps should be visible; the next advance should continue from where steps left off.
4. **History arrows** — use back/forward arrows to browse visited panels; no animations should play; advance button should be inactive.
5. **Replay button** — replay a completed panel; all replayable steps should animate and block again; non-replayable steps should skip.
6. **Loop transition** — complete the last panel in a loop; the first eligible panel of the next loop should appear.
7. **Revisit — frozen steps** — revisit a panel with steps marked `replayOnRevisit = false`, `hideOnRevisit = false`; one advance press should reveal all frozen steps in a single burst, then one final press (if `RequireAdvanceToComplete = true`) advances to the next panel.
8. **Revisit — hidden steps** — revisit a panel with steps marked `replayOnRevisit = false`, `hideOnRevisit = true`; the steps should not appear; the burst and final advance should still work correctly.