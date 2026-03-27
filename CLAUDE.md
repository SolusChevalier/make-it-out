# Claude Code Project Notes

This project was started with Claude Code.

## Agent Guidance

- Use `AGENTS.md` as the source of truth for documentation workflow rules.
- Keep `CLAUDE.md` and `AGENTS.md` aligned when process guidance changes.

## Local Claude Settings

- Repository-specific local Claude settings live in `.claude/settings.local.json`.
- That file is intentionally ignored in git because it is machine-local configuration.

## Project Documentation Standards

This repository maintains the following jam moderation documents at the project root:

- `high-concept.md`
- `plan.md`
- `requirements.txt`
- `refinement-changes.md`
- `AGENTS.md`
- `CLAUDE.md`
- `README.md` (this file)

## Maintenance Rules For Agents

- Do not delete historical entries from tracking documents.
- Add new information by appending dated entries.
- Keep entries concise and factual.
- Record why a change was made, not only what changed.
- Update `plan.md` when scope, priorities, or milestones shift.
- Update `requirements.txt` when new constraints are introduced.
- Update `refinement-changes.md` for each meaningful implementation change.
- Keep `AGENTS.md` and `CLAUDE.md` aligned when process guidance changes.