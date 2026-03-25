# High Concept - Make It Out

## Elevator Pitch

`Make It Out` is a first-person 3D maze escape game built around a single disorienting mechanic: 
the player can rotate the camera to any of six axis-aligned views, and gravity and movement 
always follow the camera. A corridor that was a floor becomes a wall. A wall becomes a ceiling. 
The maze never changes — but the player's relationship to it does, completely, with every switch.

## Player Fantasy

You are deep inside a vast cube. The exit exists. You can feel the shape of the maze around you, 
but only ever one plane at a time. The more you switch, the more the geometry reveals itself — 
and the more disoriented you become. Getting out is not about speed. It is about building a 
mental model of a space that refuses to stay still.

## Core Mechanic

The player moves in 2D at all times — left, right, jump, fall — but the axis those directions 
map to in world space is determined entirely by the current camera orientation. Four arrow keys 
rotate the camera 90 degrees at a time in any direction, including upside-down. Gravity always 
pulls toward the camera's current down. A surface is a surface because you are looking at it 
that way, not because it was designed as one.

This creates the central tension of the game: every orientation switch that reveals a new path 
forward is also a switch that makes the path you came from harder to retrace.

## What Makes It Distinct

Most maze games ask the player to build a 2D map in their head. `Make It Out` asks the player 
to build a 3D one — and then rotate it. The maze is procedurally generated fresh each run, so 
no memorised solution survives. The challenge is spatial reasoning under accumulated 
disorientation, not reflex or speed.

## Target Experience

- A single run feels like solving a puzzle that kept changing shape while you were solving it.
- The moment of finding the exit feels earned in a way that is hard to immediately explain.
- The player wants to try again because they now understand something they did not before.

## Target Session Length

10 to 20 minutes per run. Not a speed game.

## Scope Guardrails

- One scene. One mechanic. One win condition.
- No combat, no collectibles, no timer unless playtesting reveals it is needed.
- Visual polish is secondary to the spatial clarity of the maze geometry.
- Audio and atmosphere are stretch goals — a working, readable maze comes first.

## Change Log

### 2026-03-25

- Rewrote baseline high concept to accurately reflect the axis-switch orientation mechanic,
  which is the actual core of the game. Previous version described a generic maze escape 
  without capturing the spatial disorientation mechanic or the camera-relative gravity system.