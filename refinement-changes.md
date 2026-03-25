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
