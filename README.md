# Unity 2D Fighting Game MVP

This repository contains a 1-week deliverable MVP for a 2D side-view fighting game (KOF-like) built in Unity.

## Features (MVP)
- Player vs Simple AI
- Movement: walk, jump, crouch, auto-turn
- Combat: light/heavy attacks, block, dodge (brief i-frames), basic combos
- Core systems: Hitbox/Hurtbox with animation events, hit-stun, knockback, hit stop
- UI: health bars, round timer, win/lose flow (code-driven HUD via HUDFactory)
- Audio hooks and simple VFX placeholders

## Repo Structure
- `Assets/` (created after opening Unity)
  - `Scripts/`
    - `Combat/`: Hitbox, Hurtbox, DamageInfo, Health
    - `Fighter/`: FighterController, States, Input (IInputSource, PlayerInputSource, AIInputSource, InputDriver)
    - `Systems/`: RoundManager, GameManager, FrameClock, CameraShaker
    - `UI/`: HUDFactory, SafeAreaClamp, Health/Meter binders, Debug HUD
    - `Data/`: ScriptableObjects for fighter stats and moves
  - `Art/`, `Audio/`, `Prefabs/`, `Scenes/`
- `ProjectSettings/`, `Packages/` (Unity-generated)

## Quick Start
1) Open Unity Hub -> New Project (2D URP or 2D). Close it.
2) Clone this repo and open the folder in Unity. Unity will generate `Assets/`, `Packages/`, etc.
3) Create two scenes: `Scenes/MainMenu` and `Scenes/Battle`.
4) Create a `Fighter` prefab:
   - Add components: `Rigidbody2D`, `CapsuleCollider2D` (body), `Animator`
   - Add child `Hurtbox` (BoxCollider2D set as Trigger) with `Hurtbox.cs`
   - Add child `Hitboxes` empty with several BoxCollider2D children (set Trigger) + `Hitbox.cs`
   - Attach `FighterController.cs`, `InputDriver` and one input source:
     - Player: `Fighter/Input/PlayerInputSource`
     - AI: `Fighter/Input/AIInputSource`
   - Create `FighterStats` ScriptableObject and assign
   - Specials: `SpecialMoveSet` defines input sequences using direction/keys (e.g. Down, Forward, Heavy -> Super; Down, Down, Light -> Heal). `CommandQueue` default cleanup 0.25s; sequence matching uses per-entry `maxWindowSeconds` (default 0.6s).
5) Animator: set Idle/Walk/Jump/Crouch/Block/Light/Heavy/Hit/KO. Add Animation Events to attack clips to toggle hitboxes.
6) In the battle scene, add `RoundManager` and call `UI/HUDFactory.Create(...)` (or use the `BattleAutoSetup` to auto-build the scene). Link returned references to `RoundManager` (`p1`, `p2`, `p1Hp`, `p2Hp`, `timerText`).
7) Play. Use controls below.

## Controls (default)
- Move: A/D or Left/Right
- Jump: Space
- Crouch: S or DownArrow
- Light: J
- Heavy: K
- Block (hold): Left Shift
- Dodge: L

## White-box Hitbox/Hurtbox
- `Hurtbox` = trigger collider on the defender, carries reference to its owner.
- `Hitbox` = trigger collider on the attacker, toggled on/off by animation events during active frames.
- On trigger enter: if active and the other is a `Hurtbox` with a different owner, apply `DamageInfo` to the defender.

## Build & Record
- Build PC/Mac/Linux Standalone for submission
- Record a 3–5 min demo (OBS): show movement, attacks, block/dodge, combo, AI, round end.

## Git & Branching
- `main`: stable
- `dev`: active development
- Use feature branches and PRs for daily progress

## License
MIT