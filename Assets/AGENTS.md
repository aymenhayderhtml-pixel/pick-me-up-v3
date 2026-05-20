# Assets/AGENTS.md

## Purpose
Use this file when editing anything under `Assets/`.

## Asset Rules
- Keep `ScriptableObject` assets as the source of truth for static content
- Keep runtime state out of asset files
- Do not rename or move assets unless you are prepared to update all references and docs
- Prefer consistent names like `SO_Hero_*`, `SO_Enemy_*`, `SO_Floor_*`, and `SO_Skill_*`
- Keep UI prefabs in `Assets/Prefabs/UI`

## Loading Rules
- `Resources` is acceptable for the current build
- Future systems should be designed so they can move to Addressables
- Do not introduce ad hoc folder scans in gameplay code

## Local Docs
- Read the nearest folder-specific `AGENTS.md` before editing any asset-related scripts or prefabs
- If a folder does not have a local doc, use the root `AGENTS.md` plus `PROJECT_MAP.md`

