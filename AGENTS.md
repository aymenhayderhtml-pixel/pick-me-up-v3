# AGENTS.md

## Purpose
This repository is a Unity 6 game project. This file is the primary entry point for all AI coding agents working in the repo.

The goal is to keep agents:
- fast
- token-efficient
- architecture-aware
- narrow in scope
- consistent across sessions

Agents should rely on documentation and system maps before scanning code.

---

# Architecture Philosophy

- Prefer data-driven systems over hardcoded branching.
- Prefer composition over inheritance.
- Separate runtime state from template data.
- UI reflects gameplay state but does not own gameplay logic.
- Stable systems should be extended carefully instead of rewritten.
- Systems should communicate through events, interfaces, or data contracts instead of tight direct coupling.
- Keep systems modular and independently understandable.

---

# Read Order

Unless overridden by a deeper local AGENTS.md:

1. AGENTS.md
2. CURRENT_TASK.md
3. PROJECT_MAP.md
4. Relevant system document in Docs/
5. Nearest local AGENTS.md
6. Only the code files required for the task

Never begin by scanning the entire repository.

---

# Non-Negotiable Rules

- Never scan the entire Assets/ tree for orientation.
- Never rewrite stable systems without explicit reason.
- Never widen task scope unnecessarily.
- Never add gameplay logic inside UI scripts.
- Never store scene object references in save files.
- Never mutate ScriptableObject templates at runtime.
- Never edit Library/, Logs/, Temp/, or generated build output.
- Never introduce hidden dependencies between unrelated systems.
- Never bypass documented architecture rules without explanation.

---

# Navigation Rules

- Use PROJECT_MAP.md first when locating systems.
- Read only the relevant system documentation.
- If a folder has its own AGENTS.md, read it after the root file.
- Prefer partial context loading:
  - file header
  - public API
  - nearby dependencies
  - then implementation
- If a task touches multiple systems, inspect only those systems.

---

# Preferred Edit Strategy

1. Read documentation first.
2. Inspect public APIs and headers before full files.
3. Make the smallest safe change possible.
4. Avoid unrelated edits.
5. Preserve stable architecture patterns.
6. Update docs after structural changes.
7. Leave clear summaries for future agents.

---

# Uncertainty Rules

- If system intent is unclear, inspect docs before editing.
- If architecture conflicts appear, explain before implementing.
- Avoid speculative refactors.
- Prefer minimal safe edits over broad rewrites.
- If a dependency chain is unclear, stop expanding scope and consult PROJECT_MAP.md.

---

# Runtime Data Rules

- ScriptableObjects are immutable templates.
- Runtime HP, progression, buffs, upgrades, cooldowns, and temporary state belong in runtime models.
- Save systems serialize runtime data only.
- Asset templates must never store live gameplay state.
- Runtime instances should be reconstructible from template IDs + save data.

---

# Dependency Direction

- UI may depend on gameplay systems.
- Gameplay systems must not depend on UI.
- Data definitions should not depend on scene logic.
- Save systems must not depend directly on UI systems.
- Runtime systems should avoid direct dependencies on presentation layers.

---

# Editing Rules

- Keep MonoBehaviour classes thin and lifecycle-focused.
- Keep gameplay logic outside UI controllers.
- Prefer event-driven communication between systems.
- Keep manager responsibilities narrow.
- Use ScriptableObjects for static content definitions.
- Use serializable runtime models for mutable state.
- Prefer reusable systems over one-off hardcoded solutions.
- Avoid singleton sprawl.

---

# Folder Ownership

- Scripts/Core = global runtime services
- Scripts/Combat = combat-only systems
- Scripts/UI = presentation and interaction
- Scripts/Data = ScriptableObject definitions
- Scripts/Heroes = hero runtime logic
- Scripts/Save = persistence and serialization
- Scripts/Summon = gacha and reward generation
- Scripts/Inventory = inventory and equipment logic
- Do not move responsibilities across folders casually.

---

# Performance Rules

- Avoid FindObjectOfType during gameplay loops.
- Cache expensive lookups.
- Avoid allocations in hot combat paths.
- Prefer object pooling for repeated spawned objects.
- Avoid unnecessary Update() usage.
- Prefer event-driven updates when possible.

---

# Memory Update Rules

After meaningful changes, update living documentation:

- CURRENT_TASK.md = active work state
- TODO.md = planned future work
- BUGS.md = discovered issues
- CHANGELOG.md = completed work
- MEMORY.md = stable architecture decisions

Documentation is part of the project architecture.

---

# Token Efficiency Rules

- Read the smallest possible set of files.
- Prefer summaries over full-file rescans.
- Avoid rereading unchanged systems.
- Use docs to preserve architecture understanding.
- Leave concise summaries after completing tasks.
- Do not consume tokens rediscovering documented systems.

---

# Unity Conventions

- GameManager is the persistent runtime hub.
- CombatManager is scene-local.
- SceneLoader and AudioManager are persistent services.
- HeroData, SkillData, EnemyData, and TowerFloor are immutable templates.
- HeroInstance and GameState are runtime models.
- Resources is temporary infrastructure.
- Addressables are the future content-loading target.

---

# Data Pipeline Philosophy

Preferred future pipeline:

Google Sheets
↓
JSON Export
↓
Unity Importer
↓
ScriptableObject Generation
↓
Runtime Database
↓
Gameplay Systems

The spreadsheet is the source of truth for balance and content.

---

# Local AGENTS Inheritance

- Root AGENTS.md applies globally.
- Nested AGENTS.md files add local constraints.
- The nearest AGENTS.md takes priority during conflicts.
- Folder-level rules may narrow scope further.
- Local AGENTS.md files should stay concise and system-specific.

---

# Script Header Template

Use this header in new or heavily modified scripts:

/*
Purpose:
Responsibilities:
Dependencies:
Used By:
Warnings:
Modification Rules:
Related Systems:
*/

---

# Where To Look

- Docs/HERO_SYSTEM.md
- Docs/COMBAT_SYSTEM.md
- Docs/SUMMON_SYSTEM.md
- Docs/UI_SYSTEM.md
- Docs/SAVE_SYSTEM.md
- Docs/INVENTORY_SYSTEM.md
- Docs/SKILL_SYSTEM.md
- Docs/DATA_PIPELINE.md

Always prefer docs before large code scans.

---

# Qwen Audit Prompt

Use this prompt when asking Qwen to diagnose the repo with maximum signal and minimum token waste.

## Copy/Paste Prompt

You are auditing a Unity 6 project for real bugs, not style.

Goal:
- Find all current gameplay, UI, save/load, scene, data, and wiring problems that are directly verifiable from the files you open.
- Use as few tokens as possible.
- Focus on root causes, not symptoms.
- If something is intentional or uncertain, mark it as `needs confirmation`.

Current known pain points to inspect first:
- roster portraits are still blank even though hero names and stats display
- roster generation uses template heroes
- skill data was missing before and now exists as starter assets
- scene loading may still reference missing or fallback scenes

Read order:
1. `AGENTS.md`
2. `CURRENT_TASK.md`
3. `PROJECT_MAP.md`
4. `Docs/HERO_SYSTEM.md`
5. `Docs/UI_SYSTEM.md`
6. `Docs/SKILL_SYSTEM.md`
7. Only the exact code files needed to verify a suspected issue

Rules:
- Do not scan the whole repo.
- Do not inspect unrelated systems.
- Do not give generic architecture advice unless it explains the bug.
- Use exact file paths and line numbers.
- If multiple files share the same root cause, report the root cause once and list all affected files.
- If a file is not the source of the bug, ignore it.
- If a bug is only visible in the editor or scene setup, say so explicitly.

Output format:

```md
# Findings
| Severity | File | Evidence | Problem | Fix |
| --- | --- | --- | --- | --- |

# Needs Confirmation
| File | Why uncertain | What to check |
| --- | --- | --- | --- |

# Conclusion
- State whether the issue is a real bug, missing content, or wiring problem.
- State the minimum fix needed.
```

What to verify for the roster issue:
- whether `HeroData.portrait` is actually assigned in the template assets
- whether the roster card and detail panel are using the correct `RawImage`
- whether the prefab or scene has the wrong child name or missing component
- whether the selected hero is a runtime preview copy with a stripped portrait reference
- whether portraits fail because the `Sprite` asset import type or texture assignment is wrong
