# MEMORY.md

## Stable Project Facts
- Game type: infinite gacha RPG inspired by *Pick Me Up*
- Engine: Unity 6
- Primary architecture: data-driven, ScriptableObject-based, modular gameplay systems
- Save format: JSON via `SaveSystem` into `Application.persistentDataPath`
- Persistent runtime hub: `GameManager`
- Persistent utility services: `SceneLoader`, `AudioManager`
- Scene-local battle system: `CombatManager`
- Main template assets: `HeroData`, `SkillData`, `EnemyData`, `TowerFloor`
- Main runtime data: `HeroInstance`, `GameState`, `SkillInstance`, `QuestSaveData`

## Current Folder Truth
- Code lives mainly in `Assets/Scripts/`
- UI controllers live in `Assets/Scripts/UI/`
- Runtime data assets live in `Assets/Resources/Heroes`, `Assets/Resources/Enemies`, and `Assets/Resources/Floors`
- UI prefabs live in `Assets/Prefabs/UI`
- Editor utilities live in `Assets/Editor`
- Scene files live in `Assets/Scenes`
- Non-Unity support folders exist at `src/` and `ui-prototype/`

## Architecture Decisions To Preserve
- `HeroData` is read-only template data
- `HeroInstance` stores the mutable history of one summoned hero
- `GameState` is the only save-root object
- UI should react to events from managers rather than mutating state directly
- Battle should use runtime snapshots, not raw `ScriptableObject` references
- Content should be referenced by stable IDs, not by scene object references
- Hardcoded logic should be isolated to small evaluators only when the system is still experimental

## Known Current Risks
- `SceneLoader` points at a `Results` scene name, but no `Results.unity` is present in `Assets/Scenes`
- `SkillInstance` expects `Resources/Skills`, but that folder is not currently populated
- `QuestSystem` uses lambda subscriptions in `OnEnable` and `OnDisable`, so its unsubscribe logic is not symmetric
- Several systems still contain hardcoded skill IDs and balancing values
- `Resources.LoadAll` is still used for hero registration, which is acceptable short-term but not ideal for scaling

## Token Efficiency Strategy
Use these rules to keep AI context small:
- read `PROJECT_MAP.md` before opening code
- read only the system doc that matches the current task
- prefer file headers and public APIs over full-file scans
- load one system at a time
- summarize what changed before moving to the next file
- do not reread unchanged systems
- keep new knowledge in docs so later agents can reuse it

## Maintenance Rules
- When a public API changes, update the matching system doc immediately
- When a folder moves, update `PROJECT_MAP.md`
- When a bug is found, add it to `BUGS.md` before leaving the task
- When work is completed, log it in `CHANGELOG.md`
- When the plan changes, update `CURRENT_TASK.md`

## Current Assumptions
- Combat will remain event-driven and scene-local
- Inventory and equipment will be data-driven before they are feature-complete
- Addressables will likely replace most `Resources` lookups later
- Future content import will come from a spreadsheet-to-JSON pipeline

