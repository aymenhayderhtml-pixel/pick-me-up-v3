# SAVE_SYSTEM.md

## Purpose
Defines the save schema, JSON persistence, and load boot flow.

## Main Scripts
- `Assets/Scripts/GameState_SaveSystem.cs`
- `Assets/Scripts/GameManager.cs`
- `Assets/Scripts/QuestSystem.cs`

## Dependencies
- `GameState`
- `HeroInstance`
- `QuestSaveData`
- `DroppedGearZone`
- `JsonUtility`
- `Application.persistentDataPath`

## Data Flow
```mermaid
flowchart LR
    A[Runtime managers] --> B[GameState]
    B --> C[SaveSystem.Save]
    C --> D[save.json]
    D --> E[SaveSystem.Load]
    E --> F[GameManager state]
```

## Runtime Lifecycle
1. `SaveSystem.Initialize` ensures the save folder exists
2. `GameManager` loads save data on startup
3. Gameplay mutates `GameState`
4. Any major change triggers `SaveSystem.Save`
5. On next boot, `Load` restores state or creates a fresh save

## Related Managers
- `GameManager`
- `QuestSystem`
- `FacilityManager`
- `MoraleSystem`

## Common Bugs
- Schema changes can break old saves
- `JsonUtility` is limited compared to custom serializers
- Scene objects and direct references are not safe in save data
- Missing versioning makes migrations harder

## Important Warnings
- Keep save data flat and serializable
- Never store a `MonoBehaviour` or `ScriptableObject` instance inside `GameState`
- Use stable IDs instead of direct object references
- Validate new fields before shipping

## AI Editing Precautions
- Any change to `GameState` must be reviewed for load compatibility
- Update `MEMORY.md`, `BUGS.md`, and `PROJECT_MAP.md` when save structure changes
- Do not add transient UI state to the save model

## Future Expansion Plans
- Save versioning
- Migration support
- Cloud sync
- Optional encryption
- Separate profile slots

