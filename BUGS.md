# BUGS.md

## Known Issues
1. `SceneLoader` includes `SCENE_RESULTS`, but no `Results.unity` file is currently present in `Assets/Scenes`.
2. `SkillInstance` looks up data from `Resources/Skills`, but that resource folder is not currently populated.
3. `QuestSystem` subscribes with anonymous lambdas in `OnEnable`, then attempts to unsubscribe with new anonymous lambdas in `OnDisable`, which will not remove the original handlers.
4. Several combat and skill behaviors are still hardcoded by skill ID, so new skills require manual evaluator updates.
5. `Resources.LoadAll` is still used for hero registration in `GameManager`, which is acceptable for now but should be replaced by a stronger registry when the roster grows.
6. `ANTARIS` currently uses a placeholder portrait copied from the `ISLAT HAN` archive image because no dedicated source image was found yet.

## Watchlist
- Save schema changes need versioning before release
- Scene name changes must be mirrored in `SceneLoader`
- Any move away from `Resources` must preserve current load paths until migration is complete
- Battle UI must continue to consume events rather than polling state

## How To Record A Bug
Use this format:
```text
Date:
System:
Problem:
Impact:
Repro:
Suggested Fix:
Owner:
```
