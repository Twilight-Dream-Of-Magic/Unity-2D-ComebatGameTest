# Unity 2D Fighting Game MVP

This repository contains a 1-week deliverable MVP for a 2D side-view fighting game (KOF-like) built in Unity.

## Features (MVP)
- Player vs Simple AI
- Movement: walk, jump, crouch, auto-turn
- Combat: light/heavy attacks, block, dodge (brief i-frames), basic combos
- Core systems: Hitbox/Hurtbox with animation events, hit-stun, knockback, hit stop
- UI: health bars, round timer, win/lose flow (code-driven HUD via HUDFactory)
- Audio hooks and simple VFX placeholders

## Recent Fixes
- Auto-bind `Hitbox.owner` and `Hurtbox.owner` at runtime when not wired in Inspector (prevents hits from being ignored and ensures AI/player damage routes correctly).
- Player input now sets `J/K` directly into the per-frame command snapshot in addition to enqueuing tokens (so light/heavy attacks work even without the queue feeder).
- `AttackExecutor` ensures hitboxes are discovered and owner-bound on `Awake`.

## Layered State Machine (HFSM)
- New hierarchical FSM organizes gameplay states with KOF rules while supporting layered blending:
  - Root -> Locomotion(super) -> `Grounded` / `Air`
  - `Grounded` children: `Idle`, `Walk`, `Crouch`, `Block` (stand/crouch), `Attack-Light`, `Attack-Heavy`, `Hitstun`, `Downed`, `Throw`, `Dodge`
  - `Air` children: `Jump`, `AirAttack-Light`, `AirAttack-Heavy`, `Hitstun`
- The controller now ticks the HFSM; legacy FSM was removed from runtime.

## UI Mode & Debug Guide
- A `RuntimeConfig` singleton controls UI visibility.
  - Mode: `Debug` or `Release` (`RuntimeConfig.uiMode`)
  - Toggles: `showStateTexts`, `showNumericBars` (HP/Meter numbers), `showDebugHUD`
- All HUD elements created by `HUDFactory` subscribe to config changes (observer pattern) and apply safe-area clamping.
- Typical usage:
  ```csharp
  Systems.RuntimeConfig.Instance.SetUIMode(Systems.UIMode.Debug);
  Systems.RuntimeConfig.Instance.SetShowStateTexts(true);
  Systems.RuntimeConfig.Instance.SetShowNumericBars(true);
  Systems.RuntimeConfig.Instance.SetShowDebugHUD(true);
  ```
  In Release mode the debug texts are hidden automatically.

## Input Tuning (Designers)
- `Data/InputTuningConfig` ScriptableObject exposes:
  - `commandBufferWindow`: token lifetime in `CommandQueue`
  - `specialHistoryLifetime`, `defaultSpecialWindowSeconds`: special input detection timing (defaults: 1.2s, 1.0s)
- Assign this asset to `BattleAutoSetup.inputTuning` (or directly to `CommandQueue`/`SpecialInputResolver`).

## Repo Structure
- `Assets/` (created after opening Unity)
  - `Scripts/`
    - `Combat/`: Hitbox, Hurtbox, DamageInfo, CommandQueue, SpecialInputResolver
    - `Fighter/`: FighterController, HFSM, Input (`PlayerBrain` / `AIBrain`)
    - `Systems/`: RoundManager, GameManager, FrameClock, CameraShaker, RuntimeConfig
    - `UI/`: HUDFactory, SafeAreaClamp, Health/Meter binders, Debug HUD, MainMenuBuilder
    - `Data/`: ScriptableObjects for fighter stats, moves, input tuning, command sequences
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
   - Attach `FighterController.cs` and one brain:
       - Player: add `Input/PlayerBrain`
       - AI: add `Input/AIBrain`
   - Create `FighterStats` ScriptableObject and assign
   - Specials via `CommandSequenceSet`: define sequences using direction/keys (e.g. Down, Forward, Heavy -> Super). Sequences with length < 3 are ignored (ensures single-key still triggers basic attacks).
5) Animator: set Idle/Walk/Jump/Crouch/Block/Light/Heavy/Hit/KO. Add Animation Events to attack clips to toggle hitboxes.
6) In the battle scene, add `RoundManager` and call `UI/HUDFactory.Create(...)` (or use the `BattleAutoSetup` to auto-build the scene). Link returned references to `RoundManager` (`p1`, `p2`, `p1Hp`, `p2Hp`, `timerText`).
7) Play. Use controls below.

## Main Menu
- Empty scene -> add `UI/MainMenuBuilder`. Optional: assign a `defaultBgm` AudioClip; if not assigned, no BGM will play.
- Start Game loads `Battle` (configurable via `battleSceneName`).
- Difficulty dropdown sets `GameManager.difficulty`.
- Master/BGM/SFX sliders set volumes via `GameManager.Set*Volume`.

## One-click Demo (for recording)
- Empty scene -> add `Dev/DemoAutoRunner` component and press Play. It auto-generates a full battle and runs a scripted showcase (walk/jump/crouch/L-L-H/block/dodge/Super). Alternatively, set `Dev/BattleAutoSetup.demoScripted = true` (uses `InputBrain` Scripted mode under the hood).

## Controls (default)
- Move: A/D (relative forward/back used for sequences)
- Jump: Space/W
- Crouch: S
- Light: J
- Heavy: K
- Block (hold): L (crouch+block for crouch-guard)
- Dodge: Left Shift
- Super: via defined sequences (length >= 3 to be recognized)

## White-box Hitbox/Hurtbox
- `Hurtbox` = trigger collider on the defender, carries reference to its owner.
- `Hitbox` = trigger collider on the attacker, toggled on/off by animation events during active frames.
- On trigger enter: if active and the other is a `Hurtbox` with a different owner, apply `DamageInfo` to the defender.

## Build & Record
- Build PC/Mac/Linux Standalone for submission
- Record a 3–5 min demo (OBS): show movement, attacks, block/dodge, sequence special, AI, round end.

## Git & Branching
- `main`: stable

## License
MIT

---

## Coding Standards（Unity/C#）

- 命名
  - 类型/类/结构/接口/枚举：PascalCase
  - 方法/属性/事件：PascalCase；布尔以 Is/Has/Can/Should 前缀
  - 局部变量/参数：camelCase；私有字段：_camelCase
  - 常量优先 static readonly；枚举成员 PascalCase
- 注释
  - 重要 public/internal API 使用 XML 文档注释（中英皆可），说明用途、参数/返回、前置/后置条件
  - Inspector 字段配合 [Header]/[Tooltip]/[Range] 等
- 代码风格
  - 缩进 4 空格；Allman 花括号；一行一条语句；避免魔法数字（用 ScriptableObject 配置）
  - var 仅在类型显而易见时使用；每个 .cs 文件一个公开类型，文件名与类型同名
- Unity 约定
  - 组件获取在 Awake/Start 缓存；避免在 Update 里频繁 GetComponent/Find
  - 物理逻辑在 FixedUpdate；渲染/相机在 LateUpdate；按需乘 Time.deltaTime
  - 序列化：私有字段 + [SerializeField]，公开只读用属性暴露
  - 日志：集中封装并可开关；避免散落 Debug.Log