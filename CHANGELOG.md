# CHANGELOG.md

## 2026-05-20
### Changed
- Implemented stable hero IDs, a runtime hero database, and database-backed hero resolution through `GameManager`
- Added a 10-hero novice roster to `Assets/Resources/Heroes/` with imported portraits and stable template IDs
- Switched the hero template model to primary stats: `Strength`, `Intelligence`, `HP`, `Agility`, and `Star`
- Replaced the temporary mock roster bootstrap with a real starter roster built from the 10 hero templates
- Rebuilt the roster screen to generate hero cards at runtime and open the detail panel on selection

### Added
- Created the AI documentation and memory architecture for the Unity 6 project
- Added root AI workflow files
- Added system templates for hero, combat, summon, UI, save, inventory, skill, and data pipeline docs
- Added local AI guidance files for asset and script folders
- Added a token-efficient Qwen audit prompt in `Docs/QWEN_CODEBASE_AUDIT_PROMPT.md`
- Added starter `SkillData` assets in `Assets/Resources/Skills/` for the current hardcoded combat skill IDs
