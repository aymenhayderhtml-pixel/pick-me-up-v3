# Assets/Scripts/UI/AGENTS.md

## Purpose
Use this file for scene UI controllers and UI prefab logic.

## Scope
Current UI scripts include:
- `BattleUI`
- `LobbyUI`
- `RosterUI`
- `SummonUI`
- `SynthesisUI`
- `MemorialUI`
- `QuestUI`
- `FacilityUI`
- `SquadFormationUI`
- `DetailPanelUI`
- `HeroCardUI`
- `GearSlotUI`
- `GachaCardFlip`
- `InfoCell`
- `TraitRowUI`
- `StatBox`

## Rules
- UI reads state from managers and raises user actions back to managers
- Subscribe in `OnEnable` and unsubscribe in `OnDisable`
- Keep business rules out of UI scripts
- Use prefabs in `Assets/Prefabs/UI`
- Do not scan unrelated UI folders if the task is a single-screen fix

