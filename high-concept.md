# High Concept - Make It Out

## Elevator Pitch

`Make It Out` is a 3D maze escape game where the player navigates the interior of
a cube-shaped labyrinth by rotating the camera to any of six axis-aligned views.
Gravity and movement always follow the camera — a corridor that was a floor becomes
a wall, a wall becomes a ceiling, depending on how many times you have switched.
Levels get larger. The maze gets harder to model in your head. The clock does not
stop.

## Player Fantasy

You are deep inside a structure that does not care about your sense of up and down.
You switch your view, the world rotates around you, and suddenly the path you came
from is above your head. The exit exists. You have to find it faster than you found
it last time.

## Core Mechanic

The player moves in 2D at all times — left, right, jump, fall — but those directions
map to different world axes depending on the current camera orientation. Four arrow
keys rotate the camera 90 degrees in any direction relative to the current view,
including upside-down. Gravity always pulls toward the camera's current down.

This means the same physical corridor can be walked along its floor, its wall, or
its ceiling depending on accumulated orientation switches. An obstacle that required
a camera switch to pass on the way in may be jumpable on the way back. The maze
never changes — the player's relationship to it does.

## Progression Structure

The game is level-based. Each level is a freshly generated maze at a specific grid
size. Early levels are small and introduce the mechanic. Later levels are larger,
more complex, and demand a more accurate mental model of the space. Each run is
timed. Finishing faster earns more stars. Stars unlock the next level.

## Scoring

Each level has five star thresholds based on completion time. Finishing at any time
always awards at least one star — there is no hard fail on timeout. The timer turns
red when the five-star window has passed, but the run continues. Stars are the
primary progression currency and the secondary optimisation target after simply
getting out.

## Target Experience

- First completion of a level feels earned and disorienting in equal measure.
- Replaying a level to improve star rating feels like a different kind of puzzle —
  the maze is the same but the player's spatial model of it is sharper.
- Each new level size feels like a meaningful step up, not just more of the same.

## Target Session Length

- 3 to 15 minutes per level depending on size and player familiarity.
- Level select and result screens support natural session breaks.

## Scope Guardrails

- No combat, no collectibles, no enemies.
- No procedural difficulty beyond grid size — complexity emerges from scale.
- Visual polish is secondary to spatial clarity of the maze geometry.
- Audio and atmosphere are stretch goals after the core loop is stable.

## Change Log

### 2026-03-26

- Rewrote to reflect level-based progression pivot. Previous version described a
  single-run escape loop. Core mechanic description is unchanged.