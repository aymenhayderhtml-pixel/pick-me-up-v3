# AGENTS.md

## Purpose
This repository is a Unity 6 game project. This file is the first read for every AI agent working in the repo.

The goal is to keep agents fast, consistent, and narrow in scope:
- do not scan the whole `Assets/` tree
- read only the files needed for the current task
- update the memory docs when the project changes
- prefer data-driven changes over hardcoded logic

## Read Order
Use this order unless a local `AGENTS.md` overrides it:
1. `AGENTS.md`
2. `CURRENT_TASK.md`
3. `PROJECT_MAP.md`
4. The relevant system doc in `Docs/`
5. The nearest local `AGENTS.md` for the folder being edited
6. Only the code files needed for the task

## Non-Negotiable Rules
- Never scan the entire `Assets` folder just to get oriented.
- Never edit `Library/`, `Logs/`, `ProjectSettings/`, or generated build output unless explicitly requested.
- Never rewrite stable systems just because they look imperfect.
- Never change a system without checking the matching doc in `Docs/`.
- Never add new gameplay rules inside UI scripts.
- Never store scene object references in save data.
- Never make `ScriptableObject` templates mutable at runtime.

## Navigation Rules
- Use `PROJECT_MAP.md` first when looking for a file or system.
- Read only the system doc that matches the task.
- If a folder has its own `AGENTS.md`, read it after this root file.
- If a task touches multiple systems, read each system doc, but do not widen scope beyond those systems.
- Prefer partial context loading: file header, public API, nearby dependency, then edit.

## Editing Rules
- Keep `MonoBehaviour` classes thin and lifecycle-focused.
- Keep data definitions in `ScriptableObject` assets or serializable runtime models.
- Keep battle logic, save logic, and UI logic separated.
- Use events to move state changes from managers to UI.
- If you add a new public method or data field, update the relevant doc in `Docs/`.
- If you change a system boundary, update `PROJECT_MAP.md` and `MEMORY.md`.

## Memory Update Rules
After any meaningful change, update the living docs:
- `CURRENT_TASK.md` for the current work state
- `TODO.md` for future work
- `BUGS.md` for known issues discovered
- `CHANGELOG.md` for completed work
- `MEMORY.md` for stable architecture decisions

## Token Efficiency Rules
- Read the smallest possible set of files.
- Prefer summaries and headers over full-file rereads.
- Do not reread unchanged systems.
- Capture system-level knowledge in docs so later agents do not need to rescan code.
- When finishing a task, leave a short doc note instead of reopening many files.

## Unity Conventions
- `GameManager` is the persistent runtime hub.
- `SceneLoader` and `AudioManager` are persistent scene services.
- `CombatManager` is scene-local.
- `HeroData`, `SkillData`, `EnemyData`, and `TowerFloor` are read-only templates.
- `HeroInstance`, `GameState`, and save DTOs are runtime data.
- `Resources` is supported for now, but Addressables is the future target.

## Local AGENTS Inheritance
- Root `AGENTS.md` applies everywhere.
- A deeper `AGENTS.md` adds local rules and can narrow scope further.
- The nearest matching `AGENTS.md` wins when rules conflict.
- If a folder-specific doc says "do not touch unrelated files", obey it even if the root file is broader.

## Script Header Template
Use this header in new or heavily edited scripts:

```csharp
/*
Purpose:
Responsibilities:
Dependencies:
Used By:
Warnings:
Modification Rules:
Related Systems:
*/
```

## Where To Look
- `Docs/HERO_SYSTEM.md` for hero templates and runtime hero state
- `Docs/COMBAT_SYSTEM.md` for battle flow and combat helpers
- `Docs/SUMMON_SYSTEM.md` for gacha, pity, and summon flow
- `Docs/UI_SYSTEM.md` for scene UI controllers and prefab patterns
- `Docs/SAVE_SYSTEM.md` for JSON save/load rules
- `Docs/INVENTORY_SYSTEM.md` for planned item and equipment work
- `Docs/SKILL_SYSTEM.md` for skill data and combat skill execution
- `Docs/DATA_PIPELINE.md` for content pipeline and validation

