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
