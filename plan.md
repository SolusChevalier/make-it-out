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

### M1 — Playable Core Loop
- [ ] Loading screen runs generation and shows progress bar.
- [ ] Player spawns at maze centre and can move, jump, and climb.
- [ ] Camera switches correctly through all six orientations including upside-down states.
- [ ] Gravity and jump always follow current camera orientation — no world Y assumptions.
- [ ] Player can reach an exit and trigger a win state.
- [ ] Session can be restarted without editor intervention.

### M2 — Spatial Readability
- [ ] Transparency pass correctly reveals the player's movement plane through occluding walls.
- [ ] Exit blocks are clearly visually distinct from maze walls.
- [ ] Chunk culling keeps frame rate stable while navigating.
- [ ] Camera transition animation is smooth and does not disorient beyond the intended mechanic.

### M3 — Feel and Feedback
- [ ] Movement feels responsive and well-tuned — speed, jump height, step-up timing.
- [ ] Camera switch has satisfying weight and snap.
- [ ] Minimal UI communicates current orientation and exit proximity without cluttering the view.
- [ ] Basic audio pass — movement, jump, camera switch, exit found.

### M4 — Polish and Submission
- [ ] Full performance profile pass — generation time, frame rate, memory.
- [ ] Bug fix pass against all system acceptance criteria.
- [ ] Build and test on target Windows platform.
- [ ] All moderation documents current and accurate.

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

### 2026-03-25

- Rewrote plan to reflect actual game design: axis-switch camera mechanic, orientation-relative 
  gravity, large procedural cube maze. Previous version described a generic maze escape.
- Added architecture overview of five systems and their dependency order.
- Expanded milestones to reflect specific technical deliverables per system.
- Added camera system complexity and spatial confusion as explicit risks.
