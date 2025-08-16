# Coding Standards (C# / Unity)

## Naming (No Abbreviations)
- Use full English words; avoid ambiguous abbreviations.
- Types/Enums/Interfaces/Attributes: PascalCase (e.g., `DamageCalculator`, `IAttackable`).
- Methods/Properties/Events: PascalCase.
- Parameters/Locals: camelCase.
- Private fields: _camelCase (leading underscore to distinguish from parameters/locals).
- Constants: prefer `static readonly` fields named in PascalCase.
- Booleans: `Is/Has/Can/Should + Noun/Verb`.
- Events: `OnXxx`.

## Comments & Docs
- Public and important internal APIs: XML doc comments with a clear intent summary and parameter/return descriptions.
- Explain “why”, not “how”.
- Use `[Tooltip]`, `[Header]`, `[Range]` for Inspector clarity.
- TODO/FIXME format: `// TODO(Erika): ...`, `// FIXME: ...`.

## Style & Formatting
- 4-space indentation; Allman braces (each `{` on its own line).
- One statement per line; avoid clever chained side effects.
- Use `var` only when the type is obvious; otherwise write explicit types.
- Avoid magic numbers; use ScriptableObject configs for gameplay tuning.
- One public type per `.cs` file and keep filename equal to type name.
- Namespaces: `Company.Product.Module` or `Studio.Game.Feature` (PascalCase segments).

## Unity-specific
- Cache components in initialization; prefer `TryGetComponent(out _)`.
- Avoid frequent `Find`/`GetComponent` calls inside `Update()`.
- Physics in `FixedUpdate()`, logic in `Update()`/`LateUpdate()`; use `Time.deltaTime` when needed.
- Serialization: `[SerializeField] private` fields; expose via read-only properties.
- Event order dependencies should be stated in doc comments.
- Prefer coroutines at runtime; use `async/Task` for tooling/editor.
- Asset organization: separate `Assets/Scripts/Runtime` and `Assets/Scripts/Editor`; use `.asmdef` boundaries.
- Logging: centralize logging helpers; avoid scattered `Debug.Log`.

## Review Checklist
- Names are descriptive; no abbreviations.
- Methods are small, single-responsibility; early returns preferred.
- Public surface is minimal and documented.
- Time/physics/frame-step correctness is respected.
- No hard-coded magic numbers; data-driven via ScriptableObjects.

Refer to `.editorconfig` for IDE-enforced rules. This document is the source of truth for code readability across the project.