# Make It Out

Unity game jam project for Windows desktop.

## Project Documentation Standards

This repository maintains the following jam moderation documents at the project root:

- `high-concept.md`
- `plan.md`
- `requirements.txt`
- `refinement-changes.md`
- `README.md` (this file)

## Maintenance Rules For Agents

- Do not delete historical entries from tracking documents.
- Add new information by appending dated entries.
- Keep entries concise and factual.
- Record why a change was made, not only what changed.
- Update `plan.md` when scope, priorities, or milestones shift.
- Update `requirements.txt` when new constraints are introduced.
- Update `refinement-changes.md` for each meaningful implementation change.

## Quick Start

1. Open project in Unity Hub.
2. Use a 2022+ LTS editor that matches project packages.
3. Run Edit Mode tests before significant merges.

## Current Focus

System 4 (player controller and orientation stubs) is implemented under 
`Assets/Scripts/Runtime/Player/`. Next dependency is System 5 (camera system replacing 
`CameraOrientation` internals). See `plan.md` for milestones and acceptance checks.
