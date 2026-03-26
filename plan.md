# Development Plan - Make It Out

## Game Summary

`Make It Out` is a 3D maze escape game built around a single core mechanic: the player rotates 
the camera to any of six axis-aligned orthographic views, and gravity and movement always follow 
the camera. The maze is a large procedurally generated cube. The player starts at the centre and 
must find one of several exits on the outer boundary. The maze never changes during a run — but 
the player's orientation to it does, completely, with every camera switch.

## Core Pillars

1. **Spatial disorientation as the core challenge.** The player must build and maintain a 3D 
   mental model of the maze while their relationship to it keeps shifting.
2. **Movement clarity at all times.** Despite the disorientation mechanic, controls must always 
   feel responsive and readable — the player should never be confused about what their inputs 
   will do, only about where they are.
3. **Procedural replayability.** No two runs share a maze. Prior knowledge of a layout does not 
   survive a new run, but spatial reasoning skills do.

## Architecture Overview

The game is built in five systems in dependency order:

- **System 1 — Grid and Chunk System:** Flat byte array world grid, coordinate utilities, 
  chunk-based mesh management, distance culling.
- **System 2 — Maze Generator:** Burst-compiled 3D DFS carver, loop injection, feature 
  placement, exit carving. Runs on loading screen.
- **System 3 — Chunk Mesh Builder:** Burst parallel face-culled mesh generation, 
  MeshDataArray creation, MeshCollider baking, GPU instanced feature props.
- **System 4 — Player Controller:** Capsule controller with all physics relative to current 
  camera orientation. No world-axis assumptions anywhere.
- **System 5 — Camera System:** Six axis-aligned orthographic views, quaternion-based switching 
  with no hardcoded orientation enum, per-frame transparency pass on occluding chunks.

## Milestones

### M1 — Core Loop (Complete)
- [x] Grid, chunk, and mesh systems operational.
- [x] Procedural maze generation with Burst jobs.
- [x] Player controller with camera-relative movement and gravity.
- [x] Camera system with full six-axis orientation switching.
- [x] Basic win state on exit reach.

### M2 — Level Progression Architecture (Complete)
- [x] LevelDefinition ScriptableObject and LevelRegistry asset.
- [x] ProgressionService, ScoringService, PersistenceService implemented and tested.
- [x] ServiceLocator wiring all services.
- [x] GameManager expanded to full flow state machine.
- [x] Grid pipeline parameterized for variable level sizes.

### M3 — UI Flow
- [x] Main menu, level select, level intro, result, pause, and high scores screens.
- [x] All screens bound to GameManager state transitions and service data.
- [x] Star rating animation on result screen.

### M4 — Balancing and Feel
- [x] Star thresholds authored for all campaign levels.
- [x] Timeout visual feedback (timer turns red past 1-star window).
- [x] Orientation switch counter tracked and shown on result screen.
- [x] Camera transition feel tuning pass.

### M5 — Tests and Submission
- [x] EditMode tests for all three services.
- [x] Flow transition tests.
- [x] Regression pass on Systems 1–5 at GridSize 31, 63, and 95.
- [ ] Windows build verified.
- [x] All moderation documents current.

## Risks and Mitigations

- **Spatial confusion becomes frustrating rather than engaging:** Ensure the transparency pass 
  is always clear and the movement plane is always readable. Playtest this early.
- **Generation performance:** Maze is large (63^3). All heavy work is Burst and Job System. 
  A loading screen is already planned — use it.
- **Camera system complexity:** Orientation is a pure quaternion with no named enum. This is 
  correct but easy to introduce drift or gimbal issues. Snap to cardinal quaternions after every 
  transition and test exhaustively.
- **Scope creep:** The mechanic is the game. No combat, no collectibles, no timer unless 
  playtesting reveals a specific need. Freeze the feature list after M1.

## Change Log

### 2026-03-25 — System 4 player controller

- Implemented `CameraOrientation` stub, `GameManager` win/fail/restart stub, and 
  `PlayerController` (CharacterController-based) with camera-relative gravity, jump, ladder 
  climb, step-up coroutine, and camera-switch lock hooks for System 5.
- Added EditMode coverage for ground-cell offset math, singleton stubs, and camera-switch 
  velocity reset. Full acceptance criteria (multi-orientation play, mesh collision, corridor 
  scene) require manual or PlayMode verification in Unity.

### 2026-03-25

- Rewrote plan to reflect actual game design: axis-switch camera mechanic, orientation-relative 
  gravity, large procedural cube maze. Previous version described a generic maze escape.
- Added architecture overview of five systems and their dependency order.
- Expanded milestones to reflect specific technical deliverables per system.
- Added camera system complexity and spatial confusion as explicit risks.

### 2026-03-26

- Finalized Stages A-F for submission readiness: progression architecture, dynamic grid
  session flow, full state-driven UI loop, and polish feedback hooks (camera/movement/UI/audio).
- Added dedicated Stage F test coverage for persistence/scoring/progression/grid session and
  play-mode regression/flow scaffolds; retained manual verification gates where editor licensing
  blocked fully automated Unity CLI execution.
- Explicitly cut non-critical polish stretch items from final scope (orientation RenderTexture
  cube replacement, scene wipe transitions, and Stage E stretch goals) to preserve submission
  stability.
