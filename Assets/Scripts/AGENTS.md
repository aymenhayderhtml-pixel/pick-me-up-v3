# Assets/Scripts/AGENTS.md

## Purpose
Use this file for all C# work in `Assets/Scripts/`.

## Rules For Script Changes
- Read the matching system doc before changing code
- Only open the scripts that belong to the system you are editing
- Keep `MonoBehaviour` classes focused on Unity lifecycle and wiring
- Keep pure logic in static helpers or runtime data classes
- Keep data templates and runtime data separate

## Current Code Boundaries
- `GameManager` owns persistent runtime state
- `CombatManager` owns battle simulation
- `GachaSystem` owns summon logic
- `SaveSystem` owns JSON persistence
- `HeroInstance` owns mutable hero state
- `HeroData`, `SkillData`, `EnemyData`, and `TowerFloor` are template data only

## Edit Discipline
- Do not rewrite stable systems without a direct request
- Do not touch unrelated systems while solving one issue
- If you add a public API, update `Docs/` and `PROJECT_MAP.md`
- If you change data shape, update save and documentation together

