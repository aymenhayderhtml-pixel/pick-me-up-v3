# Assets/Scripts/Combat/AGENTS.md

## Purpose
Use this file for battle-related work.

## Scope
Current combat logic lives in:
- `Assets/Scripts/CombatManager.cs`
- `Assets/Scripts/CombatSkillEvaluator.cs`
- `Assets/Scripts/TraitSystem.cs`
- `Assets/Scripts/TowerFloor.cs`
- `Assets/Scripts/EnemyData.cs`
- `Assets/Scripts/SkillData.cs`
- `Assets/Scripts/SkillInstance.cs`

## Rules
- Only inspect combat scripts, their direct data models, and the battle UI when the task is combat-related
- Do not open summon or save files unless battle outcomes depend on them
- Prefer changing battle evaluators or helper methods over changing the whole loop
- Keep combat scene-local and event-driven

## Notes
This folder is a forward-looking combat home. The current code is still in the shared `Assets/Scripts/` root, so treat this file as the local combat policy even before a file move happens.

