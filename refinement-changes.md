# Refinement Changes

Track meaningful project changes as the jam progresses.

## Entry Template

### YYYY-MM-DD - Short Title

- **Area:** (Gameplay / Systems / UI / Build / Docs / Other)
- **Reason:** Why this refinement was needed.
- **Change:** What was updated.
- **Impact:** Player-facing or technical impact.
- **Follow-up:** Any next action required.

## Change Log

### 2026-03-25 - Documentation baseline created

- **Area:** Docs
- **Reason:** Jam moderation requires persistent high concept and change tracking documents.
- **Change:** Added and initialized `plan.md`, `README.md`, `requirements.txt`, and `refinement-changes.md`.
- **Impact:** Establishes required project documentation and maintenance workflow.
- **Follow-up:** Append entries during each implementation/refactor pass.

### 2026-03-25 - System 3 chunk build pipeline

- **Area:** Systems
- **Reason:** System 3 requires post-generation mesh creation, chunk scene population, and feature prop rendering before gameplay starts.
- **Change:** Reworked chunk build orchestration into phased chunk generation/registration, added chunk registration API and feature instancing renderer, and connected loading progress handoff from chunk building back into `MazeGenerator`.
- **Impact:** Maze chunks are now built from world data as load-time assets and registered for activation culling, with feature props rendered through GPU instancing instead of per-prop GameObjects.
- **Follow-up:** Implement System 5 transparency mesh rebuild path in `ChunkManager.RebuildChunkMesh`.

### 2026-03-25 - System 4 player controller

- **Area:** Systems / Gameplay
- **Reason:** Core loop needs a capsule controller with gravity, jump, ladder climb, and step-up 
  defined only in camera-relative axes ahead of the real camera system.
- **Change:** Added `CameraOrientation` and `GameManager` stubs, implemented `PlayerController` 
  with the specified Update order, `SmoothStepUp` guard, and `OnCameraSwitchStart` / 
  `OnCameraSwitchComplete` hooks. Added EditMode tests for grid-below math and singleton / 
  camera-switch behaviour.
- **Impact:** Player can be wired in a test scene with `CharacterController`, manual grid data, 
  and chunk colliders; win triggers via `GameManager.TriggerWin` when standing on an exit feature 
  cell.
- **Follow-up:** Author the manual corridor test scene (separate from load scene), run Unity 
  EditMode tests with the project closed in other Unity instances, then validate System 4 
  acceptance criteria in-editor (including inverted `CameraOrientation.Up`). Implement System 5 
  to drive live orientation vectors and camera transitions.

### 2026-03-25 - System 5 camera + transparency pass

- **Area:** Systems / Gameplay
- **Reason:** The core camera mechanic required quaternion-relative rotation, smooth transitions, and runtime visibility through occluding maze geometry before game-state polish work.
- **Change:** Added `CameraController` with cardinal snapping, key-driven quaternion target computation, transition locking hooks into `PlayerController`, orientation publishing to `CameraOrientation`, zoom clamping, and camera-distance follow behavior. Added `TransparencyManager` and connected camera `LateUpdate` to apply per-chunk transparency via `MaterialPropertyBlock` only. Added `ChunkManager.GetChunkObject` support method and new EditMode tests in `CameraSystemTests`.
- **Impact:** Camera orientation can now rotate through axis-aligned states (including upside-down), player movement lock/unlock is coordinated with transitions, and occluding chunks along the view axis can be faded each frame without allocating material instances.
- **Follow-up:** Verify in live PlayMode with Unity editor open: full six-orientation traversal, transparency visuals against active chunk meshes, memory profiler material count stability, and shader compatibility for `_Alpha`.

### 2026-03-25 - Transparency scope note (jam decision)

- **Area:** Systems / Gameplay
- **Reason:** Per-block transparency would require renderer granularity not present in the chunk mesh architecture and would add complexity outside jam scope.
- **Change:** Documented and kept transparency behavior at chunk granularity, where any occluding block in a chunk fades that chunk renderer.
- **Impact:** Some non-occluding blocks inside a faded chunk may also become transparent, but runtime cost and implementation complexity stay aligned with jam constraints.
- **Follow-up:** Revisit per-block occluder highlighting only if visual readability testing shows chunk-level fading is insufficient.

### 2026-03-25 - DevEnv testing scene automation

- **Area:** Systems / Tooling / QA
- **Reason:** System 4 and System 5 acceptance checks required repeatable in-editor verification scenes, but scene setup was manual and error-prone.
- **Change:** Added runtime bootstraps for generated-world and corridor-world testing (`DevSceneBootstrap`, `DevCorridorBootstrap`), added an editor menu action (`Tools/Make It Out/Create DevEnv Scenes`) that creates `DevBoot` and `DevCorridor` scenes with core references wired, and added EditMode coverage for corridor layout feature placement and step geometry.
- **Impact:** Team can generate a stable verification environment quickly and run camera/movement/transparency checks without hand-building scene wiring each session.
- **Follow-up:** Open Unity and run the new menu command once to materialize scene/meta assets, then validate six-orientation traversal and chunk transparency visuals in PlayMode.

### 2026-03-25 - Dev HUD and editor compile hardening

- **Area:** Tooling / QA
- **Reason:** In-scene verification needed faster state visibility, and editor tooling required stronger cross-assembly resilience while runtime scripts recompile.
- **Change:** Added `DevHudOverlay` and `DevHudFormatter` with `F3` toggle and runtime status display (scene, seed, generation progress, grid, velocity, switching, orientation vectors), exposed grounded/ladder state accessors on `PlayerController`, added formatter EditMode tests, and updated scene generation to attach HUD while avoiding hard editor compile dependency on the runtime HUD type.
- **Impact:** Dev scenes now provide immediate gameplay/system telemetry during playtests, and the scene generator remains usable across script recompilation boundaries.
- **Follow-up:** Regenerate Dev scenes in Unity and verify HUD readability and toggle behavior in both `DevBoot` and `DevCorridor`.

### 2026-03-25 - Bug fix pass: input backend, instancing, MPB init

- **Area:** Systems / Runtime stability
- **Reason:** Runtime errors were blocking playtests: legacy `UnityEngine.Input` calls under new-input-only mode, `DrawMeshInstanced` with non-instanced materials, and null `MaterialPropertyBlock` usage in transparency updates.
- **Change:** Confirmed project `activeInputHandler` is set to `2` (Both) so existing legacy input calls in `PlayerController`, `CameraController`, and `DevHudOverlay` remain valid without API migration; enabled GPU instancing on DevEnv feature materials (`DevLadder`, `DevStair`, `DevExit`); added `FeaturePropRenderer` startup validation for instancing flags; updated `TransparencyManager` to initialize `_mpb` and `_currentlyTransparent` in `Awake`, added an `_mpb` null guard at the top of `UpdateTransparency`, and reused `_mpb` via `Clear()` instead of per-use allocation.
- **Impact:** Input exceptions from backend mismatch are avoided, feature prop instancing failures are caught on load (and materials are preconfigured), and transparency processing now fails safely with a clear diagnostic if lifecycle initialization is skipped.
- **Follow-up:** Restart Unity after input backend changes if prompted, then run Dev scenes and verify no `InvalidOperationException`/`ArgumentNullException` in Play Mode.

### 2026-03-26 - System 6 game loop state + minimal HUD

- **Area:** Systems / Gameplay / UI
- **Reason:** Milestone System 6 required replacing the `GameManager` stub with a full state machine and adding a minimal, jam-scope HUD + restart flow without introducing extra fail mechanics.
- **Change:** Replaced `GameManager` with `GameState`-driven flow (`Loading`, `Playing`, `Win`, `Fail`, `Restarting`), run stopwatch timing, hold-delay end-screen trigger, and guarded transition methods while preserving `TriggerWin`, `TriggerFail`, and `RestartRun` signatures. Added runtime `HudManager` to own loading/in-run/win/fail UI panel state, loading progress text/bar updates, orientation and timer display, win/fail restart buttons, and `R` key voluntary restart. Wired generation completion through `GameManager.Instance.NotifyGenerationComplete()` in `MazeGenerator`, and added out-of-bounds fail trigger in `PlayerController` so falling out of the cube exits into fail.
- **Impact:** Core loop now communicates state cleanly to the player, supports reliable run restarts in-scene, and formalizes jam fail behavior as fall-out-of-bounds (plus voluntary restart) with no enemy/timer/hazard requirement in M1.
- **Follow-up:** In Unity Editor, wire the canvas object hierarchy to `HudManager`, validate hold durations and panel transitions against acceptance criteria, and run EditMode tests with no other Unity instance open.

### 2026-03-26 - Claude Code docs + ignore alignment

- **Area:** Docs / Tooling
- **Reason:** Project setup started in Claude Code and needed explicit repository guidance plus local settings hygiene.
- **Change:** Added root `CLAUDE.md`, updated `AGENTS.md` and `README.md` to include Claude workflow alignment, and added `.claude/settings.local.json` to `.gitignore`.
- **Impact:** Agent guidance is now explicit for both Cursor and Claude workflows while machine-local Claude config stays out of version control.
- **Follow-up:** If team-level Claude settings are introduced later, commit only shared files and keep local-only settings ignored.

### 2026-03-26 - HUD canvas auto-bootstrap + EditMode coverage

- **Area:** UI / Systems / QA
- **Reason:** System 6 needed the minimal Canvas hierarchy in place immediately, without requiring manual scene setup before validation.
- **Change:** Extended `HudManager` to auto-build a Screen Space Overlay canvas hierarchy when references are unassigned (loading/hud/win/fail panels, loading slider/label, timer/orientation labels, win/fail labels, restart buttons, event system), while preserving existing assigned references if already wired. Added `HudManagerCanvasTests` to validate that missing references are populated and the panel parents attach to the overlay canvas.
- **Impact:** New runs can render core System 6 UI out of the box in development scenes, reducing setup friction and ensuring restart controls exist in minimally configured scenes.
- **Follow-up:** Re-run Unity EditMode tests once project lock contention is cleared, then manually validate visual placement/padding in the target gameplay scene and tweak anchors/font sizes if desired.
